using Godot;
using Onrinto.Core;

public partial class BeatNote : NoteObject
{
    public override JudgmentResult? Judgment(double pressTime)
    {
        double errorMs = Mathf.Abs(pressTime - HitTime) * 1000.0;

        if (errorMs > JudgmentSystem.JudgmentInstance.GoodWindowMs) return MissResult();

        double graceBefore = JudgmentSystem.JudgmentInstance.GraceBeforeMs / 1000.0;

        bool crossedBeforePress =
            _lastCrossTime.HasValue &&
            _lastCrossTime.Value >= pressTime - graceBefore && _lastCrossTime.Value <= pressTime;

        if (crossedBeforePress) return HitResult(GetGrade(errorMs), errorMs);

        pendingPressTime = pressTime;
        pendingDeadTime = pressTime + JudgmentSystem.JudgmentInstance.GraceAfterMs / 1000.0;

        return new JudgmentResult
        {
            judgmentGrade = GetGrade(errorMs),
            judgmentState = JudgmentState.Pending,
            timeErrorMs = errorMs
        };
    }

    public override JudgmentResult? ObserveMotion(PlayerMotionFrame frame, double currentTime)
    {
        float tolerance = JudgmentSystem.JudgmentInstance.HorizontalTolerance;

        UpdateCrossTime(frame, tolerance);

        if (State == NoteState.Pending)
        {
            bool crossedAfterPress =
                _lastCrossTime.HasValue &&
                _lastCrossTime.Value >= pendingPressTime && _lastCrossTime.Value <= pendingDeadTime;

            if (crossedAfterPress)
            {
                double errorMs = Mathf.Abs(pendingPressTime - HitTime) * 1000.0;
                return HitResult(GetGrade(errorMs), errorMs);
            }

            if (currentTime > pendingDeadTime)
            {
                return MissResult();
            }
        }
        else
        {
            if (currentTime > JudgmentSystem.JudgmentInstance.GoodWindowMs / 1000.0 + HitTime)
            {
                return MissResult();
            }
        }

        return null;
    }

}