using Godot;
using Onrinto.Core;

public partial class CatchNote : NoteObject
{
    public override JudgmentResult? Judgment(double pressTime)
    {
        return null;
    }

    public override JudgmentResult? ObserveMotion(PlayerMotionFrame frame, double currentTime)
    {
        float tolerance = JudgmentSystem.JudgmentInstance.HorizontalTolerance;

        UpdateCrossTime(frame, tolerance);

        double window = JudgmentSystem.JudgmentInstance.PerfectWindowMs / 1000.0;

        bool crossed = _lastCrossTime.HasValue &&
            _lastCrossTime.Value >= HitTime - window && _lastCrossTime.Value <= HitTime + window;

        if (crossed)
        {
            return HitResult(JudgmentGrade.Perfect, 0.0);
        }

        if (currentTime > HitTime + window)
            return MissResult();

        return null;
    }
}