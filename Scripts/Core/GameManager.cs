using Godot;
using Onrinto.Chart;

public partial class GameManager : Node3D
{
    public TrackData CurrentTrack { get; private set; }
    public static GameManager Instance { get; private set; }

    public float CurrentAbsoluteSpeed { get; private set; }
    public float CurrentAbsZ { get; private set; }

    private int absoluteSpeedPointIndexArrow = 0;

    private void UpdateAbsoluteState(double time)
    {
        var points = CurrentTrack.AbsoluteSpeedPoints;
        if(points == null || points.Count == 0) return;

        double firstPointTime = CurrentTrack.TickToSeconds(points[0].Tick);
        if(time < firstPointTime)
        {
            CurrentAbsoluteSpeed = points[0].Speed;
            return;
        }

        for(int i = absoluteSpeedPointIndexArrow; i < points.Count - 1; i++)
        {
            double pointTime_from = CurrentTrack.TickToSeconds(points[i].Tick);
            double pointTime_to = CurrentTrack.TickToSeconds(points[i + 1].Tick);

            if(time >= pointTime_from && time < pointTime_to)
            {
                absoluteSpeedPointIndexArrow = i;

                if(points[i].IsLinear)
                {
                    double t = (time - pointTime_from) / (pointTime_to - pointTime_from);
                    CurrentAbsoluteSpeed = Mathf.Lerp(points[i].Speed, points[i + 1].Speed, (float)t);
                }
                else
                {
                    CurrentAbsoluteSpeed = points[i].Speed;
                }
                return;
            }
        }

        absoluteSpeedPointIndexArrow = points.Count - 1;
        CurrentAbsoluteSpeed = points[^1].Speed;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (Instance != null){ QueueFree(); return; }
        Instance = this;
        
        ProcessPriority = -100;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        double tNow = MusicClock.Instance.CurrentTime;

        if(CurrentTrack == null) return;

        UpdateAbsoluteState(tNow);
        CurrentAbsZ = CurrentTrack.GetZ(tNow, CurrentTrack.RelativeSpeedPoints, CurrentTrack.RelativeZTable);
    }
}