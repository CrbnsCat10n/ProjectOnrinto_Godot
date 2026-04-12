using Godot;

public partial class MusicClock : AudioStreamPlayer
{
	public static MusicClock Instance {get; private set; }

	private double currentTime;
	public double CurrentTime { get; private set; }
	
	public override void _Ready()
	{
		Instance = this;
	}

	public void LoadMusic(string path)
	{
		if (string.IsNullOrEmpty(path)) return;
		
		var stream = GD.Load<AudioStream>(path);
		if (stream != null)
		{
			Stream = stream;
		}
		else
		{
			GD.PrintErr($"Failed to load music at: {path}");
		}
	}

	public override void _Process(double delta)
	{
		if(!Playing) return;

		double rawTime = GetPlaybackPosition();
		double delay = AudioServer.GetTimeSinceLastMix();
		double latency = AudioServer.GetOutputLatency();

		CurrentTime = rawTime + delay - latency;
	}
}
