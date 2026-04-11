using System;
using System.Windows;
using System.Windows.Media.Animation;

public class CornerRadiusAnimation : AnimationTimeline
{
    public override Type TargetPropertyType => typeof(CornerRadius);

    public CornerRadius From { get; set; }
    public CornerRadius To { get; set; }

    // Optional easing function
    public IEasingFunction EasingFunction { get; set; } = null;

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        double progress = animationClock.CurrentProgress.Value;

        // Apply easing if provided
        if (EasingFunction != null)
        {
            progress = EasingFunction.Ease(progress);
        }

        return new CornerRadius(
            From.TopLeft + (To.TopLeft - From.TopLeft) * progress,
            From.TopRight + (To.TopRight - From.TopRight) * progress,
            From.BottomRight + (To.BottomRight - From.BottomRight) * progress,
            From.BottomLeft + (To.BottomLeft - From.BottomLeft) * progress
        );
    }

    protected override Freezable CreateInstanceCore() => new CornerRadiusAnimation();
}