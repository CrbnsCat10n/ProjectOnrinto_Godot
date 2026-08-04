// 处理玩家的横向移动，并向外广播移动和打击输入
using Godot;
using Onrinto.Core;

public partial class PlayerController : Node3D
{
	[Signal]
	public delegate void HitRequestedEventHandler(int keyType);

	[Signal]
	public delegate void HorizontalPositionChangedEventHandler(float playerX);

	// 鼠标灵敏度
	[Export(PropertyHint.Range, "0.001,0.05,0.001")]
	public float MouseSensitivity { get; set; } = 0.01f;

	[Export]
	public float MinX { get; set; } = -3.0f;
	[Export]
	public float MaxX { get; set; } = 3.0f;

	private float _targetX;
	public float CurrentX => _targetX;

	public override void _Ready()
	{
		_targetX = Position.X;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		// 位移
		if (inputEvent is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_targetX -= mouseMotion.Relative.X * MouseSensitivity;
			_targetX = Mathf.Clamp(_targetX, MinX, MaxX);
			EmitSignal(SignalName.HorizontalPositionChanged, _targetX);
		}

		// 广播打击
		if (inputEvent is InputEventKey hitKey && hitKey.Pressed && !hitKey.Echo && IsLetterKey(hitKey))
		{
			EmitSignal(SignalName.HitRequested, (int)KeyType.Other);
		}

		// 退出捕获模式
		if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Escape)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}

		// 进入捕获模式
		if (inputEvent is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// 更新位置
		Position = new Vector3(
			_targetX,
			Position.Y,
			Position.Z
		);
	}

	// 识别字母键
	private static bool IsLetterKey(InputEventKey keyEvent)
	{
		Key key = keyEvent.PhysicalKeycode != Key.None
			? keyEvent.PhysicalKeycode
			: keyEvent.Keycode;

		return (long)key >= (long)Key.A && (long)key <= (long)Key.Z;
	}
}
