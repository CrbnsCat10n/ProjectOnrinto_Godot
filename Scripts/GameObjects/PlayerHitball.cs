using Godot;
using System;

public partial class PlayerHitball : Node3D
{
	private float _leftLimit = -4.0f;
	private float _rightLimit = 4.0f;
	private float _moveSharpness = 20.0f;

	private float _groundY = 0.0f;
	private float _jumpDuration => GameManager.Instance.JumpDuration;
	private float _maxJumpHeight = 3.0f;
	private float _jumpHeight => _maxJumpHeight * (GameManager.Instance.JumpHeight);
	enum State{Normal, Floating, Jumping};

	public float _currentX = 0.0f;
	public float _currentY = 0.0f;
	private float _currentJumpTime = 0.0f;
	// Called when the node enters the scene tree for the first time.

	State state;

	public Vector3 CurrentJudgePosition => new Vector3(_currentX, _currentY, GameManager.Instance.CurrentAbsZ);
	public override void _Ready()
	{
		_currentX = Position.X;
		_currentY = Position.Y;	
		state = State.Normal;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		float dt = (float)delta;

		UpdatePositionX(dt);
		UpdatePositionY(dt);

		Position = new Vector3(_currentX, _currentY, 0.0f);
	}

	private void UpdatePositionX(float dt)
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

		_currentX = Mathf.Lerp(_leftLimit, _rightLimit, mousePos.X / viewportSize.X);
		
		float newX = Mathf.Lerp(Position.X, _currentX, 1.0f - Mathf.Exp(-_moveSharpness * dt));
		Position = new Vector3(newX, Position.Y, Position.Z);
	}

	private void UpdatePositionY(float dt)
	{
		switch(state)
		{
			case State.Normal:
				_currentY = _groundY;
				break;
			case State.Floating:
				_currentY = _groundY + _jumpHeight;
				break;
			case State.Jumping:
				_currentJumpTime += dt;
				if(_currentJumpTime >= _jumpDuration)
				{
					state = State.Normal;
					_currentJumpTime = 0.0f;
				}
				else
				{
					float jumpProgress = _currentJumpTime / _jumpDuration;
					_currentY = _groundY + Mathf.Sin(jumpProgress * Mathf.Pi) * _jumpHeight; // Simple jump arc
				}
				break;
		}
	}
	
	public void Jump()
	{
		if(state == State.Normal)
		{
			state = State.Jumping;
			_currentJumpTime = 0.0f;
		}
	}

	public void Dash()
	{
		if(state != State.Normal)
		{
			state = State.Normal;
		}
	}
	
	public void Float()
	{
		if(state == State.Jumping)
		{
			state = State.Floating;
		}
	}
}
