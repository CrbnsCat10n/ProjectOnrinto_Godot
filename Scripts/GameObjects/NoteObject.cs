using Godot;
using Onrinto.Chart;

public partial class NoteObject : Node3D
{
	private double _hitSeconds;
	private float _hitAbsZ;
	private bool _isHit = false;
	private bool _initialized = false;

	public void Initialize(ChartEvent chartEvent)
	{
		_hitSeconds = chartEvent.HitTime;
		_hitAbsZ = chartEvent.HitAbsZ;
		float initialZ = _hitAbsZ - GameManager.Instance.CurrentAbsZ;
		Position = new Vector3(chartEvent.Position.X, chartEvent.Position.Y, initialZ);

		_initialized = true;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!_initialized) return;

		double _currentTime = MusicClock.Instance.CurrentTime;		

		float newZ = (float)((_hitAbsZ - GameManager.Instance.CurrentAbsZ) * GameManager.Instance.FinalSpeed);
		Position = new Vector3(Position.X, Position.Y, newZ); // Update Z position

		if(_hitSeconds - _currentTime < -0.5) QueueFree(); // Remove note
		
		// Toggle visibility instead of removing
		Visible = (newZ >= 0 && newZ <= GameManager.Instance.VisibleDistance);
	}
}
