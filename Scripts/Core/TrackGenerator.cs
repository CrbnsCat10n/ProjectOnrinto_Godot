// 读取谱面数据，预计算事件位置，并按音乐进度生成音符实例。
using Godot;
using System.Text.Json;
using Onrinto.Chart;
using System.Text.Json.Serialization;
using System.Linq;
using System.Collections.Generic;
using Onrinto.Core;

public partial class TrackGenerator : Node3D
{
	TrackData track = new TrackData();
	List<ChartEvent> _notes = new List<ChartEvent>();
	[Export] public PackedScene NotePrefab;
	[Export] public NodePath JudgmentSystemPath;
	private JudgmentSystem _judgmentSystem;

	private int _spawnIndex = 0;

	// 加载谱面、初始化计算数据并开始播放音乐。
	public override void _Ready()
	{
		// 从场景路径解析判定系统节点。
		_judgmentSystem = GetNodeOrNull<JudgmentSystem>(JudgmentSystemPath);
		if (_judgmentSystem == null)
		{
			GD.PrintErr("TrackGenerator: JudgmentSystem path is not set or invalid.");
		}

		// 加载并解析谱面数据。
		string chart_path = "res://Charts/track.json";
		if (!FileAccess.FileExists(chart_path))
		{
			GD.PrintErr($"Can't find chart file: {chart_path}");
			return;
		}

		using var file = FileAccess.Open(chart_path, FileAccess.ModeFlags.Read);
		string json = file.GetAsText();

		var options = new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true,
			Converters = { new JsonStringEnumConverter(), new Vector2Converter() }
		};
		try {
			track = JsonSerializer.Deserialize<TrackData>(json, options);

			// foreach (var e in track.Events) {
			// 	GD.Print($"事件时间: {e.Tick}, 类型: {e.GetType().Name}");
			// 	if (e is AnimatedEvent ani) {
			// 		GD.Print($"  -> 这是一个动画音符开始位置: {ani.StartPosition}, 结束位置: {ani.EndPosition}");
			// 	}
			// }
		} catch (JsonException ex) {
			GD.PrintErr("Failed to parse chart JSON: " + ex.Message);
		}

		_notes = track.Events.OrderBy(e => e.Tick).ToList(); // Ensure events are sorted by time.
		track.Initialize(); // Pre-calculate hit times and positions.
		GameManager.Instance.CurrentTrack = track; // Set the current track in the game manager.

		// 加载并播放谱面对应的音乐
		if (!string.IsNullOrEmpty(track.MusicPath))
		{
			MusicClock.Instance.LoadMusic(track.MusicPath);
			MusicClock.Instance.Play();
		}
	}

	private void spawnNote(ChartEvent e) {
		// 创建并初始化一个音符实例
		var noteInstance = NotePrefab.Instantiate<NoteObject>();

		noteInstance.Initialize(e);
		AddChild(noteInstance);

		// 生成完成后交给判定系统维护活动音符列表
		_judgmentSystem?.RegisterNote(noteInstance);
	}

	// 每帧检查是否有音符进入可见范围
	public override void _Process(double delta)
	{
		if (track == null) return;

		float currentZ = GameManager.Instance.CurrentAbsZ;
		float spawnThreshold = GameManager.Instance.VisibleDistance;

		// 连续生成所有已经进入可见范围的事件
		while (_spawnIndex < _notes.Count)
		{
			var e = _notes[_spawnIndex];
			float visualDist = (e.HitAbsZ - currentZ) * GameManager.Instance.FinalSpeed;

			if (visualDist <= spawnThreshold)
			{
				// 跳过已经错过太久的事件
				if (e.HitTime - MusicClock.Instance.CurrentTime >= -0.5)
				{
					spawnNote(e);
				}
				_spawnIndex++;
			}
			else break;
		}
	}
}
