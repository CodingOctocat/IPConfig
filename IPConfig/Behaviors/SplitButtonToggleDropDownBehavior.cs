using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using HandyControl.Controls;

using IPConfig.Languages;

using Microsoft.Xaml.Behaviors;

namespace IPConfig.Behaviors;

/// <summary>
/// 使 SplitButton 的下拉显示表现为切换打开行为。
/// </summary>
public class SplitButtonToggleDropDownBehavior : Behavior<SplitButton>
{
    private bool _canPlayTextAnimation;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObject_PreviewMouseLeftButtonDown;
        AssociatedObject.Click += AssociatedObject_Click;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObject_PreviewMouseLeftButtonDown;
        AssociatedObject.Click -= AssociatedObject_Click;

        base.OnDetaching();
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

    private void AssociatedObject_Click(object sender, RoutedEventArgs e)
    {
        if (_canPlayTextAnimation)
        {
            AnimateText();

            _canPlayTextAnimation = false;
        }
    }

    private void AssociatedObject_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var result = VisualTreeHelper.HitTest(AssociatedObject, e.GetPosition(AssociatedObject));

        if (result?.VisualHit is Border { Child: null } or TextBlock)
        {
            _canPlayTextAnimation = true;
        }

        // 如果 VisualHit 为 Path，则鼠标在点击 SplitButton 的 DropDown 位置，
        // 如果为 TextBlock，则鼠标在点击 SplitButton 的非 DropDown 位置。
        // 如果为 Border，并且其 Child 不为 null，则鼠标在点击 DropDown 位置，否则，鼠标在点击 非 DropDown 位置。
        // 如果为 null，则鼠标在点击其他位置。
        // 如果不判断 VisualHit，则在点击下拉项前，此事件将被调用，下拉项被关闭，导致下拉项无法被点击到。
        if (AssociatedObject.IsDropDownOpen && result?.VisualHit is Border or Path or TextBlock)
        {
            AssociatedObject.IsDropDownOpen = false;
            e.Handled = true;
        }
    }
}
