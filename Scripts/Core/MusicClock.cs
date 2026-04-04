using Godot;

public partial class MusicClock : AudioStreamPlayer
{
	public static MusicClock Instance {get; private set; }

	private double currentTime;
	public double CurrentTime { get; private set; }
	
	public override void _Ready()
	{
		Instance = this;
		if (Stream == null) GD.PrintErr("MusicClock: No Audio File?");
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
