using Godot;
using System;
using System.Text.Json.Serialization;

namespace Onrinto.Chart;

public enum EventType { Hole, Obstacle, Beat, Catch, Through, Jump, Dash, Float };
public enum AnimationType { Linear, Sin, Back };

public class TempoPoint
{
	public double Tick { get; set; }
	public float BPM { get; set; }

	[JsonConstructor]
	public TempoPoint(double tick, float bpm)
	{
		Tick = tick;
		BPM = bpm;
	}
}

public class SpeedPoint
{
	public double Tick { get; set; }
	public float Speed { get; set; }
	public bool IsLinear { get; set; }

	[JsonConstructor]
	public SpeedPoint(double tick, float speed, bool isLinear)
	{
		Tick = tick;
		Speed = speed;
		IsLinear = isLinear;
	}
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ChartEvent), typeDiscriminator: "Onrinto.Chart.ChartEvent")]
[JsonDerivedType(typeof(AnimatedEvent), typeDiscriminator: "Onrinto.Chart.AnimatedEvent")]
[Serializable]
public class ChartEvent
{

	public EventType Type { get; set; }
	public double Tick { get; set; }
	public Vector2 Position { get; set; }
	public double HitTime { get; set; }
	public float HitAbsZ { get; set; }

	public ChartEvent(EventType type, double tick, Vector2 position)
	{
		Type = type;
		Tick = tick;
		Position = position;
	}
}

[Serializable]
public class AnimatedEvent : ChartEvent
{
	public AnimationType AniType { get; set; }
	public int In_Out { get; set; }

	public double AniStartTime { get; set; }
	public double AniEndTime { get; set; }

	public Vector2 StartPosition { get; set; }
	public Vector2 EndPosition { get; set; }

	public float RelativeSpeed { get; set; }

	public AnimatedEvent(
		EventType type,
		double tick,
		Vector2 position,
		AnimationType aniType,
		int in_Out,
		double aniStartTime,
		double aniEndTime,
		float relativeSpeed,
		Vector2 startPosition,
		Vector2 endPosition
	) : base(type, tick, position)
	{
		AniType = aniType;
		In_Out = in_Out;
		AniStartTime = aniStartTime;
		AniEndTime = aniEndTime;
		RelativeSpeed = relativeSpeed;
		StartPosition = startPosition;
		EndPosition = endPosition;
	}
}
