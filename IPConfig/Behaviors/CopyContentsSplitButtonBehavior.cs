using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

using HandyControl.Controls;

using IPConfig.Languages;

namespace IPConfig.Behaviors;

public class CopyContentsSplitButtonBehavior : SplitButtonToggleDropDownBehavior
{
    private bool _canPlayTextAnimation;

    protected override void OnAssociatedObjectClick(object sender, RoutedEventArgs e)
    {
        if (_canPlayTextAnimation)
        {
            AnimateText();

            _canPlayTextAnimation = false;
        }
    }

    protected override void OnAssociatedObjectPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e, HitTestResult hitTestResult)
    {
        if (hitTestResult?.VisualHit is Border { Child: null } or TextBlock)
        {
            _canPlayTextAnimation = true;
        }
    }

    private void AnimateText()
    {
        NameScope.SetNameScope(AssociatedObject, new NameScope());
        string name = $"SplitButton_{Guid.NewGuid().ToString().Replace('-', '_')}";
        AssociatedObject.RegisterName(name, AssociatedObject);

        var keyFrames = new StringAnimationUsingKeyFrames {
            Duration = new(TimeSpan.FromSeconds(1))
        };

        var frame = new DiscreteStringKeyFrame(Lang.Copied, KeyTime.FromTimeSpan(TimeSpan.Zero));
        keyFrames.KeyFrames.Add(frame);

        var story = new Storyboard { FillBehavior = FillBehavior.Stop };
        Storyboard.SetTargetName(keyFrames, name);
        Storyboard.SetTargetProperty(keyFrames, new PropertyPath(SplitButton.ContentProperty));
        story.Children.Add(keyFrames);
        story.Begin(AssociatedObject);
    }
}
