// 接收玩家输入，管理活动音符，并完成时间与横向位置判定。
using Godot;
using System;
using System.Collections.Generic;

namespace Onrinto.Core;

public enum JudgmentGrade { Perfect, Great, Good, Miss }

public partial class JudgmentSystem : Node
{
	[Signal]
	public delegate void JudgmentMadeEventHandler(NoteObject note, int grade, double timeErrorMs);

	[Export] public NodePath PlayerPath;
	private PlayerController _player;

	[ExportGroup("Time Windows")]
	[Export] public double PerfectWindowMs = 60.0;
	[Export] public double GreatWindowMs = 80.0;
	[Export] public double GoodWindowMs = 120.0;

	[ExportGroup("Horizontal Tolerance")]
	[Export] public float HorizontalTolerance = 0.7f;
	[Export] public double GraceBeforeMs = 50.0;
	[Export] public double GraceAfterMs = 30.0;
	[Export] public double HistoryDurationMs = 150.0;

	// 活动音符列表、Pending 输入列表、玩家横向位置历史
	private readonly List<NoteObject> _activeNotes = new();
	private readonly List<PendingHit> _pendingHits = new();
	private readonly List<PositionSample> _positionHistory = new();

	private readonly struct PositionSample
	{
		public readonly double Time;
		public readonly float X;

		public PositionSample(double time, float x)
		{
			Time = time;
			X = x;
		}
	}

	private sealed class PendingHit
	{
		public NoteObject Note { get; }
		public double PressTime { get; }
		public double Deadline { get; }

		public PendingHit(NoteObject note, double pressTime, double deadline)
		{
			Note = note;
			PressTime = pressTime;
			Deadline = deadline;
		}
	}

	public override void _Ready()
	{
		// 从场景路径解析玩家节点
		_player = GetNodeOrNull<PlayerController>(PlayerPath);
		if (_player == null)
		{
			GD.PrintErr("JudgmentSystem: Player not found ?!");
			return;
		}

		// 订阅玩家广播的移动和打击输入
		_player.HitRequested += OnHitRequested;
		_player.HorizontalPositionChanged += OnHorizontalPositionChanged;
		AddPositionSample(GetMusicTime(), _player.CurrentX);
	}

	public override void _ExitTree()
	{
		if (_player == null) return;

		_player.HitRequested -= OnHitRequested;
		_player.HorizontalPositionChanged -= OnHorizontalPositionChanged;
	}

	// 每帧处理等待横向容错的输入，并清理已经错过的音符
	public override void _Process(double delta)
	{
		double currentTime = GetMusicTime();
		UpdatePendingHits(currentTime);
		UpdateMissedNotes(currentTime);
		CleanupLists();
		PrunePositionHistory(currentTime);
	}

	// 由 TrackGenerator 调用，将新生成的音符加入候选列表
	public void RegisterNote(NoteObject note)
	{
		if (note == null || _activeNotes.Contains(note)) return;
		_activeNotes.Add(note);
	}

	// 收到按键后，先找时间最近的音符，再检查横向轨迹
	private void OnHitRequested(float playerX)
	{
		double pressTime = GetMusicTime();
		AddPositionSample(pressTime, playerX);

		NoteObject candidate = FindBestCandidate(pressTime);
		if (candidate == null) return;

		// 满足横向位置，判定
		double graceBefore = GraceBeforeMs / 1000.0;
		if (HasPassedHorizontalRange(candidate.HitX, pressTime - graceBefore, pressTime))
		{
			ResolveHit(candidate, pressTime);
			return;
		}

		// 不满足横向位置，短暂等待玩家随后经过正确位置
		candidate.BeginPending();
		double deadline = pressTime + GraceAfterMs / 1000.0;
		_pendingHits.Add(new PendingHit(candidate, pressTime, deadline));
	}

	// 保存鼠标移动形成的短时间横向轨迹
	private void OnHorizontalPositionChanged(float playerX)
	{
		AddPositionSample(GetMusicTime(), playerX);
	}

	// 在最大时间窗口内选择时间误差最小的活动音符
	private NoteObject FindBestCandidate(double pressTime)
	{
		NoteObject bestNote = null;
		double bestError = double.MaxValue;
		double maxWindow = GoodWindowMs / 1000.0;

		foreach (NoteObject note in _activeNotes)
		{
			if (!IsNoteUsable(note) || note.State != NoteState.Active) continue;

			double error = Math.Abs(note.HitTime - pressTime);
			if (error <= maxWindow && error < bestError)
			{
				bestNote = note;
				bestError = error;
			}
		}

		return bestNote;
	}

	// Pending 输入在截止时间前经过横向区域即可命中
	private void UpdatePendingHits(double currentTime)
	{
		for (int i = _pendingHits.Count - 1; i >= 0; i--)
		{
			PendingHit pending = _pendingHits[i];
			if (!IsNoteUsable(pending.Note))
			{
				_pendingHits.RemoveAt(i);
				continue;
			}

			if (HasPassedHorizontalRange(pending.Note.HitX, pending.PressTime, currentTime))
			{
				ResolveHit(pending.Note, pending.PressTime);
				_pendingHits.RemoveAt(i);
			}
			else if (currentTime >= pending.Deadline)
			{
				pending.Note.CancelPending();
				_pendingHits.RemoveAt(i);
			}
		}
	}

	// 超过最大时间窗口且没有 Pending 输入的音符判定为 Miss
	private void UpdateMissedNotes(double currentTime)
	{
		double missWindow = GoodWindowMs / 1000.0;

		foreach (NoteObject note in _activeNotes)
		{
			if (!IsNoteUsable(note) || note.State != NoteState.Active) continue;
			if (currentTime <= note.HitTime + missWindow) continue;

			note.ResolveMiss();
			EmitSignal(SignalName.JudgmentMade, note, (int)JudgmentGrade.Miss, GoodWindowMs);
			GD.Print($"MISS | note={note.HitTime:F3}s");
		}
	}

	// 用相邻采样点形成的线段检查玩家是否经过音符横向区域
	private bool HasPassedHorizontalRange(float noteX, double fromTime, double toTime)
	{
		for (int i = 0; i < _positionHistory.Count; i++)
		{
			PositionSample current = _positionHistory[i];
			if (current.Time >= fromTime && current.Time <= toTime &&
				Math.Abs(current.X - noteX) <= HorizontalTolerance)
			{
				return true;
			}

			if (i == 0) continue;

			PositionSample previous = _positionHistory[i - 1];
			if (current.Time < fromTime || previous.Time > toTime) continue;

			float minX = Math.Min(previous.X, current.X) - HorizontalTolerance;
			float maxX = Math.Max(previous.X, current.X) + HorizontalTolerance;
			if (noteX >= minX && noteX <= maxX) return true;
		}

		return false;
	}

	// 完成命中并广播结果，UI 和计分系统之后可以订阅该 Signal
	private void ResolveHit(NoteObject note, double pressTime)
	{
		double errorMs = Math.Abs(note.HitTime - pressTime) * 1000.0;
		JudgmentGrade grade = GetGrade(errorMs);

		note.ResolveHit();
		EmitSignal(SignalName.JudgmentMade, note, (int)grade, errorMs);
		GD.Print($"{grade.ToString().ToUpper()} | error={errorMs:F1}ms | x={note.HitX:F2}");
	}

	private JudgmentGrade GetGrade(double errorMs)
	{
		if (errorMs <= PerfectWindowMs) return JudgmentGrade.Perfect;
		if (errorMs <= GreatWindowMs) return JudgmentGrade.Great;
		return JudgmentGrade.Good;
	}

	private void AddPositionSample(double time, float x)
	{
		_positionHistory.Add(new PositionSample(time, x));
	}

	private void PrunePositionHistory(double currentTime)
	{
		double cutoff = currentTime - HistoryDurationMs / 1000.0;
		while (_positionHistory.Count > 1 && _positionHistory[1].Time < cutoff)
		{
			_positionHistory.RemoveAt(0);
		}
	}

	private void CleanupLists()
	{
		_activeNotes.RemoveAll(note => !IsNoteUsable(note));
		_pendingHits.RemoveAll(pending => !IsNoteUsable(pending.Note));
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
