// 保存谱面数据，并将 tick、节奏和速度点预计算为运行时可用的数据
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Onrinto.Chart;

public class TrackData
{
    public string MusicPath { get; set; }
    public int BPM { get; set; }
    public double Offset { get; set; }
    public double TicksPerBeat { get; set; }
    public List<ChartEvent> Events { get; set; } = new List<ChartEvent>();
    public List<TempoPoint> TempoPoints { get; set; } = new List<TempoPoint>();
    public List<SpeedPoint> RelativeSpeedPoints { get; set; } = new List<SpeedPoint>();
    public List<SpeedPoint> AbsoluteSpeedPoints { get; set; } = new List<SpeedPoint>();

    public List<double> TempoSecondsTable { get; private set; } = new List<double>();
    public List<float> RelativeZTable { get; private set; } = new List<float>();

	// 初始化时间和路程缓存，并计算事件的命中数据。
    public void Initialize()
    {
        InitializeTempoTable();
        InitializeRelativeZTable(RelativeSpeedPoints);

        foreach(var e in Events)
        {
            e.HitTime = TickToSeconds(e.Tick);
            e.HitAbsZ = GetZ(e.HitTime, RelativeSpeedPoints, RelativeZTable);
        }
    }

    public void InitializeTempoTable()
    {
		// 根据 BPM 变化点建立累计时间表。
        TempoSecondsTable.Clear();
        if(TempoPoints == null || TempoPoints.Count == 0) return;

        TempoPoints = TempoPoints.OrderBy(tp => tp.Tick).ToList();
        double cachedSeconds = 0.0;
        TempoSecondsTable.Add(cachedSeconds);

        for(int i = 0; i < TempoPoints.Count - 1; i++)
        {
            double ticksDelta = TempoPoints[i + 1].Tick - TempoPoints[i].Tick;
            double secondsDelta = ticksDelta / TicksPerBeat * 60.0 / TempoPoints[i].BPM;
            cachedSeconds += secondsDelta;
            TempoSecondsTable.Add(cachedSeconds);
        }
    }

    public void InitializeRelativeZTable(List<SpeedPoint> speedPoints)
    {
		// 根据速度点建立累计 Z 路程表。
        RelativeZTable.Clear();
        if(speedPoints == null || speedPoints.Count == 0) return;

        speedPoints = speedPoints.OrderBy(sp => sp.Tick).ToList();
        float cachedZ = 0.0f;
        RelativeZTable.Add(cachedZ);

        for(int i = 0; i < speedPoints.Count - 1; i++)
        {
            double timeDelta = TickToSeconds(speedPoints[i + 1].Tick) - TickToSeconds(speedPoints[i].Tick);
            
			if(speedPoints[i].IsLinear)
            {
                cachedZ += (float)timeDelta * (speedPoints[i].Speed + speedPoints[i + 1].Speed) * 0.5f;
            }
			else
			{
				cachedZ += (float)timeDelta * speedPoints[i].Speed;
            }
            RelativeZTable.Add(cachedZ);
        }
    }

    public double TickToSeconds(double tick)
    {
		// 将 tick 按当前 BPM 区间换算为秒数。
        if(TempoPoints == null || TempoPoints.Count == 0) 
            return tick / TicksPerBeat * 60.0 / BPM;

		int idx = 0;
        for(int i = 0; i < TempoPoints.Count; i++)
        {
            if(tick >= TempoPoints[i].Tick) idx = i;
            else break;
        }

        return TempoSecondsTable[idx] + (tick - TempoPoints[idx].Tick) / TicksPerBeat * 60.0 / TempoPoints[idx].BPM;
    }

    public float GetZ(double time, List<SpeedPoint> speedPoints, List<float> zTable)
    {
		// 根据时间和速度表计算当前位置的 Z 路程。
        if(speedPoints == null || speedPoints.Count == 0) return 0.0f;

        int idx = 0;
        for(int i = 0; i < speedPoints.Count; i++)
        {
            if(time >= TickToSeconds(speedPoints[i].Tick)) idx = i;
            else break;
        }

        var point = speedPoints[idx];
        float cachedZ = zTable[idx];
        double pointTime = TickToSeconds(point.Tick);
        double deltaTime = time - pointTime;

		if(point.IsLinear && idx < speedPoints.Count - 1)
        {
            double nextTime = TickToSeconds(speedPoints[idx + 1].Tick);
            if (nextTime <= pointTime) 
            {
                return cachedZ + (float)deltaTime * point.Speed;
            }
            float t = (float)((time - pointTime) / (nextTime - pointTime));
            float currentSpeed = Mathf.Lerp(point.Speed, speedPoints[idx + 1].Speed, t);
            return cachedZ + (float)deltaTime * (point.Speed + currentSpeed) * 0.5f;
        }
        else
        {
            return cachedZ + (float)deltaTime * point.Speed;
        }
    }
}
