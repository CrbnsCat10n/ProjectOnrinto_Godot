// 表示场景中的单个音符，并根据音乐时间更新其位置和可见性
using Godot;
using Onrinto.Chart;

public enum NoteState { Active, Pending, Hit, Missed }

public partial class NoteObject : Node3D
{
	private double _hitSeconds;
	private float _hitAbsZ;
	private float _hitX;
	private bool _initialized = false;
	private EventType _noteType;

	public double HitTime => _hitSeconds;
	public float HitX => _hitX;
	public NoteState State { get; private set; } = NoteState.Active;
	public bool IsResolved => State == NoteState.Hit || State == NoteState.Missed;

	// 使用谱面事件初始化音符的命中数据和起始位置。
	public void Initialize(ChartEvent chartEvent)
	{
		_noteType = chartEvent.Type;
		_hitSeconds = chartEvent.HitTime;
		_hitAbsZ = chartEvent.HitAbsZ;
		_hitX = chartEvent.Position.X;

		float initialZ = _hitAbsZ - GameManager.Instance.CurrentAbsZ;
		Position = new Vector3(chartEvent.Position.X, chartEvent.Position.Y, initialZ);
		_initialized = true;
	}

	// Pending 表示已有一次时间正确的输入正在等待横向容错。
	public void BeginPending()
	{
		if (State == NoteState.Active) State = NoteState.Pending;
	}

	public void CancelPending()
	{
		if (State == NoteState.Pending) State = NoteState.Active;
	}

	// 判定结果暂时只改变状态并移除节点，之后可在这里加入表现效果
	public void ResolveHit()
	{
		if (IsResolved) return;

		State = NoteState.Hit;
		QueueFree();
	}

	public void ResolveMiss()
	{
		if (IsResolved) return;

		State = NoteState.Missed;
		QueueFree();
	}

	// 音符进入场景树时调用
	public override void _Ready()
	{

	}

	// 每帧更新音符位置、可见性并清理过期音符
	public override void _Process(double delta)
	{
		if (!_initialized) return;

		double _currentTime = MusicClock.Instance.CurrentTime;		

		// 根据当前路程更新音符的 Z 坐标
		float newZ = (float)((_hitAbsZ - GameManager.Instance.CurrentAbsZ) * GameManager.Instance.FinalSpeed);
		Position = new Vector3(Position.X, Position.Y, newZ); // Update Z position

		// 判定系统未接管时保留一个兜底清理时间
		if(!IsResolved && _hitSeconds - _currentTime < -0.5) ResolveMiss();
		
		// 根据可见距离控制音符显示
		Visible = (newZ >= 0 && newZ <= GameManager.Instance.VisibleDistance);
	}
}
