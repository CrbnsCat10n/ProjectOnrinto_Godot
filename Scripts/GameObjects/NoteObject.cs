// 音符对象的基类，定义了音符的基本属性和行为
using Godot;
using Onrinto.Chart;
using Onrinto.Core;

public abstract partial class NoteObject : Node3D
{
	private double _hitSeconds;
	private float _hitAbsZ;
	private float _hitX;
	private bool _initialized = false;

	public EventType _noteType;
	public bool _requirePress => _noteType == EventType.Beat || _noteType == EventType.Jump || _noteType == EventType.Dash; 
	public Label3D _judgmentLabel;

	protected double? _lastCrossTime;
	protected double pendingPressTime;
	protected double pendingDeadTime;

	public double HitTime => _hitSeconds;
	public float HitX => _hitX;
	public NoteState State { get; set; } = NoteState.Active;
	public bool IsResolved => State == NoteState.Done;

	// 使用谱面事件初始化音符的命中数据和起始位置
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

	public abstract JudgmentResult? Judgment(double pressTime);
	public abstract JudgmentResult? ObserveMotion(PlayerMotionFrame frame, double currentTime);

	public JudgmentResult? HitResult(JudgmentGrade grade, double errorMs)
	{
		return new JudgmentResult
		{
			judgmentGrade = grade,
			judgmentState = JudgmentState.Done,
			timeErrorMs = errorMs
		};
	}

	public JudgmentResult? MissResult()
	{
		return new JudgmentResult
		{
			judgmentGrade = JudgmentGrade.Miss,
			judgmentState = JudgmentState.Done,
			timeErrorMs = double.NaN
		};
	}

	public JudgmentGrade GetGrade(double errorMs)
	{
		if (errorMs <= JudgmentSystem.JudgmentInstance.PerfectWindowMs) return JudgmentGrade.Perfect;
		if (errorMs <= JudgmentSystem.JudgmentInstance.GreatWindowMs) return JudgmentGrade.Great;
		if (errorMs <= JudgmentSystem.JudgmentInstance.GoodWindowMs	) return JudgmentGrade.Good;
		return JudgmentGrade.Miss;
	}

	protected bool UpdateCrossTime(PlayerMotionFrame motionFrame, float horizontalTolerance)
	{
		float minX = Mathf.Min(motionFrame.PreviousX, motionFrame.CurrentX) - horizontalTolerance;
		float maxX = Mathf.Max(motionFrame.PreviousX, motionFrame.CurrentX) + horizontalTolerance;

		float speed = (motionFrame.CurrentX - motionFrame.PreviousX) / (float)(motionFrame.CurrentTime - motionFrame.PreviousTime);

		if (_hitX >= minX && _hitX <= maxX)
		{
			_lastCrossTime = (motionFrame.CurrentTime + motionFrame.PreviousTime) / 2.0;

			if (speed > 5.0f)
				return true;
		}

		return false;
	}

	public override void _Ready()
	{
		_judgmentLabel = GetNodeOrNull<Label3D>("Label3D");
		if (_judgmentLabel == null)
		{
			GD.PrintErr("Judgment label not found in NoteObject.");
		}

		_judgmentLabel.Text = "";
	}

	public override void _Process(double delta)
	{
		if (!_initialized) return;	

		// 根据当前路程更新音符的 Z 坐标
		float newZ = (float)((_hitAbsZ - GameManager.Instance.CurrentAbsZ) * GameManager.Instance.FinalSpeed);
		Position = new Vector3(Position.X, Position.Y, newZ); // Update Z position

		// 根据可见距离控制音符显示
		Visible = (newZ >= 0 && newZ <= GameManager.Instance.VisibleDistance);
	}

	public async void Resolve()
	{
		State = NoteState.Done;
		
		await ToSignal(
			GetTree().CreateTimer(0.5),
			SceneTreeTimer.SignalName.Timeout
		);

		QueueFree();
	}
}
