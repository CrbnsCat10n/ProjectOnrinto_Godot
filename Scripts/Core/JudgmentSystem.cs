// 接收玩家输入，管理活动音符，并完成时间与横向位置判定。
using Godot;
using System;
using System.Collections.Generic;

namespace Onrinto.Core;

public enum NoteState { Active, Pending, Done }
public enum KeyType { Space, Shift, Other }
public enum PressType{ Press, Release }

public struct InputContext
{
	public double PressTime;
	public float PlayerX;
	public KeyType Key;
	public PressType PressType;
}

public enum JudgmentGrade { Perfect, Great, Good, Miss }
public enum JudgmentState { Pending, Done }
public struct JudgmentResult
{
	public JudgmentGrade judgmentGrade;
	public JudgmentState judgmentState;
	public double timeErrorMs;
} 

public readonly record struct PlayerMotionFrame(
    double PreviousTime,
    float PreviousX,
    double CurrentTime,
    float CurrentX
);

public partial class JudgmentSystem : Node
{
	public static JudgmentSystem JudgmentInstance {get; private set;}

	[Signal]
	public delegate void JudgmentMadeEventHandler(NoteObject note, int grade, double timeErrorMs);

	[Export] public NodePath PlayerPath;

	public PlayerMotionFrame motion {get; private set;}
	private PlayerController _player;

	[ExportGroup("Time Windows")]
	[Export] public double PerfectWindowMs = 60.0;
	[Export] public double GreatWindowMs = 80.0;
	[Export] public double GoodWindowMs = 120.0;

	[ExportGroup("Horizontal Tolerance")]
	[Export] public float HorizontalTolerance = 0.9f;
	[Export] public double GraceBeforeMs = 50.0;
	[Export] public double GraceAfterMs = 50.0;
	[Export] public double HistoryDurationMs = 150.0;

	private readonly List<NoteObject> _activeNotes = new();

	public override void _Ready()
	{
		JudgmentInstance = this;

		// 从场景路径解析玩家节点
		_player = GetNodeOrNull<PlayerController>(PlayerPath);
		if (_player == null)
		{
			GD.PrintErr("JudgmentSystem: Player not found ?!");
			return;
		}

		// 订阅玩家广播的移动和打击输入
		_player.HitRequested += OnHitRequested;
	}

	public override void _ExitTree()
	{
		if (_player == null) return;
		_player.HitRequested -= OnHitRequested;
	}

	public override void _Process(double delta)
	{
		double currentTime = GetMusicTime();
		recordMotionFrame(currentTime, _player.CurrentX);
		UpdateActiveNotes(currentTime);
		CleanupLists();
	}

	// 由 TrackGenerator 调用，将新生成的音符加入候选列表
	public void RegisterNote(NoteObject note)
	{
		if (note == null || _activeNotes.Contains(note)) return;
		_activeNotes.Add(note);
	}

	// 记录相邻帧轨迹
	private void recordMotionFrame(double currentTime, float currentX)
	{
		motion = new PlayerMotionFrame(
			motion.CurrentTime,
			motion.CurrentX,
			currentTime,
			currentX
		);
	}

	// 处理非打击与挂起的音符
	private void UpdateActiveNotes(double currentTime)
	{
		foreach (NoteObject note in _activeNotes)
		{
			if (!IsNoteUsable(note))
				continue;

			JudgmentResult? result =
				note.ObserveMotion(motion, currentTime);

			SendResult(note, result);
		}
	}

	// 处理打击音符
	private void OnHitRequested(int key)
	{
		KeyType keyType = (KeyType)key;

		double pressTime = GetMusicTime();
		
		var note = FindBestCandidate(pressTime);
		if (note == null) return;

		JudgmentResult? result = note.Judgment(pressTime);
		SendResult(note, result);
		
	}

	// 在最大时间窗口内选择时间误差最小的活动音符
	private NoteObject FindBestCandidate(double pressTime, bool requirePress = true)
	{
		NoteObject bestNote = null;
		double bestError = double.MaxValue;
		double maxWindow = GoodWindowMs / 1000.0;

		foreach (NoteObject note in _activeNotes)
		{
			if(requirePress && note.State == NoteState.Pending) continue;
			if (!IsNoteUsable(note) || note.State == NoteState.Done) continue;

			double error = Math.Abs(note.HitTime - pressTime);
			if (error <= maxWindow && error < bestError)
			{
				bestNote = note;
				bestError = error;
			}
		}

		return bestNote;
	}

	// 发送判定结果至结算系统
	private void SendResult(NoteObject note, JudgmentResult? result)
	{
		if (result.HasValue)
		{
			JudgmentResult res = result.Value;

			if(res.judgmentState == JudgmentState.Pending)
			{
				note.State = NoteState.Pending;
				return;
			}

			if(res.judgmentState == JudgmentState.Done)
			{
				EmitSignal(SignalName.JudgmentMade, note, (int)res.judgmentGrade, res.timeErrorMs);
				GD.Print($"Judgment: {res.judgmentGrade} | Error: {res.timeErrorMs:F2} ms");
				note._judgmentLabel.Text = res.judgmentGrade.ToString();

				note.Resolve();
			}
		}
	}

	// 清理已完成或无效的音符
	private void CleanupLists()
	{
		_activeNotes.RemoveAll(note => !IsNoteUsable(note));
	}

	private static bool IsNoteUsable(NoteObject note)
	{
		return GodotObject.IsInstanceValid(note) && !note.IsQueuedForDeletion() && !note.IsResolved;
	}

	private static double GetMusicTime()
	{
		return MusicClock.Instance?.GetAccurateTime() ?? 0.0;
	}
}
