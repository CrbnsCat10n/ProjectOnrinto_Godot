using Godot;
using System.Text.Json;
using Onrinto.Chart;
using System.Text.Json.Serialization;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

public partial class TrackGenerator : Node3D
{
	TrackData track = new TrackData();
	List<ChartEvent> _notes = new List<ChartEvent>();
	[Export] public PackedScene NotePrefab;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Load chart data.
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
	}

	private void spawnNote(ChartEvent e) {
		var noteInstance = NotePrefab.Instantiate<NoteObject>();

		float speed = 20.0f;
		double hitTime = (double)(e.Tick / track.TicksPerBeat * 60.0 / track.BPM);
		float initialZ = (float)(hitTime * speed);

		noteInstance.Initialize(e, hitTime, initialZ); // Initial Z position
		AddChild(noteInstance);
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
