// 负责加载音乐，并提供经过音频延迟修正的全局播放时间。
using Godot;

public partial class MusicClock : AudioStreamPlayer
{
	public static MusicClock Instance {get; private set; }

	private double currentTime;
	public double CurrentTime { get; private set; }
	
	// 初始化全局音乐时钟。
	public override void _Ready()
	{
		Instance = this;
	}

	// 从资源路径加载音频流并设置播放器。
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

	// 每帧更新经过音频延迟修正的播放时间。
	public override void _Process(double delta)
	{
		if(!Playing) return;

		CurrentTime = GetAccurateTime();
	}

	// 在输入发生时即时计算音乐时间，避免读取上一帧的缓存值。
	public double GetAccurateTime()
	{
		if (!Playing) return CurrentTime;

		double rawTime = GetPlaybackPosition();
		double delay = AudioServer.GetTimeSinceLastMix();
		double latency = AudioServer.GetOutputLatency();

		return rawTime + delay - latency;
	}
}
