using OLLM.Utility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace OLLM.State.Thinking;

using static Constants;
using static MdFd;

/// <summary>
/// Represents a visual adorner that displays a floating bubble over an adorned element, providing contextual
/// information during the model's thinking process.
/// </summary>
public sealed class FloatingAdorner : Adorner {
	// For creating logs of their thinking so we can correct with instructions.
	internal string GetBodyText() => _body.Text;

	private readonly VisualCollection _visuals;

	// Obstruction layer (blurred snapshot + dim tint)
	private readonly Grid _veilRoot;
	private readonly Border _veilBlur;
	private readonly Border _veilTint;

	// Bubble (card)
	private readonly Border _bubble;
	private readonly TextBlock _title;
	private readonly ScrollViewer _scroll;
	private readonly TextBlock _body;

	// Auto-scroll / batching
	private readonly DispatcherTimer _flushTimer;
	private string _pending = string.Empty;
	private bool _dirty;

	public FloatingAdorner(UIElement adornedElement) : base(adornedElement) {
		_visuals = new VisualCollection(this);
		IsHitTestVisible = false;

		_veilBlur = new Border {
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Opacity = 0.82,
			Background = new VisualBrush(adornedElement) {
				Stretch = Stretch.None,
				AlignmentX = AlignmentX.Left,
				AlignmentY = AlignmentY.Top
			},
			Effect = new BlurEffect {
				Radius = 18,
				KernelType = KernelType.Gaussian
			}
		};

		_veilTint = new Border {
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Background = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)),
			Opacity = 0
		};

		_veilRoot = new Grid {
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Opacity = 0,
			IsHitTestVisible = false
		};
		_veilRoot.Children.Add(_veilBlur);
		_veilRoot.Children.Add(_veilTint);

		_title = new TextBlock {
			Foreground = _white,
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Opacity = 0.92,
			TextWrapping = TextWrapping.NoWrap,
			Text = _thinking
		};

		_body = new TextBlock {
			Foreground = _white,
			FontSize = 11,
			Opacity = 0.92,
			TextWrapping = TextWrapping.Wrap
		};

		_scroll = new ScrollViewer {
			Content = _body,
			VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			CanContentScroll = false,
			MaxWidth = 680,
			MaxHeight = 227
		};

		_bubble = new Border {
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,

			CornerRadius = new CornerRadius(0),
			Padding = new Thickness(8, 8, 8, 8),
			Background = _black,
			BorderBrush = _readyDosPurple,

			BorderThickness = new Thickness(1),
			Child = new StackPanel {
				Orientation = Orientation.Vertical,
				Children =
				{
					_title,
					new Border { Height = 1, Margin = new Thickness(0, 3, 0, 3), Background = _readyDosPurple, Opacity = 1 },
					_scroll
				}
			},
			RenderTransformOrigin = new Point(0.5, 0.5),
			RenderTransform = new TransformGroup {
				Children =
				[
					new ScaleTransform(1,1),
					new TranslateTransform(0,0)
				]

			},
			Opacity = 0
		};

		_flushTimer = new DispatcherTimer(DispatcherPriority.Background) {
			Interval = TimeSpan.FromMilliseconds(48)
		};
		_flushTimer.Tick += (_, _) => {
			if (!_dirty)
				return;
			_dirty = false;
			FlushPendingToUi();
		};

		_visuals.Add(_veilRoot);
		_visuals.Add(_bubble);
	}

	public void SetTitle(string title)
		=> _title.Text = title;

	public void SetText(string text) {
		_body.Text = text;
		AutoScrollToBottom();
	}

	public void Append(string chunk) {
		if (string.IsNullOrEmpty(chunk))
			return;

		_pending += chunk;
		_dirty = true;

		if (!_flushTimer.IsEnabled)
			_flushTimer.Start();
	}

	private void FlushPendingToUi() {
		string text = _pending;
		if (string.IsNullOrEmpty(text))
			return;

		_pending = string.Empty;

		Application.Current.Dispatcher.InvokeAsync(() => {
			_body.Text += text;
			AutoScrollToBottom();
		}, DispatcherPriority.Background);
	}

	private void AutoScrollToBottom() {
		_scroll.UpdateLayout();
		_scroll.ScrollToEnd();
	}

	public async Task ShowAtTopRight(double margin = 14) {
		await Application.Current.Dispatcher.InvokeAsync(() => {
			FrameworkElement fe = (FrameworkElement)AdornedElement;
			double x = Math.Max(margin, fe.ActualWidth - margin - _bubble.ActualWidth);
			double y = margin;

			TransformGroup tg = (TransformGroup)_bubble.RenderTransform;
			TranslateTransform tt = (TranslateTransform)tg.Children[1];
			tt.X = x;
			tt.Y = y;

			InvalidateArrange();
		}, DispatcherPriority.Render);
	}

	public async Task AnimateIn() {
		await Application.Current.Dispatcher.InvokeAsync(() => {
			Storyboard sb = new();

			DoubleAnimation veilFade = new(0, 1, TimeSpan.FromMilliseconds(333)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
			Storyboard.SetTarget(veilFade, _veilRoot);
			Storyboard.SetTargetProperty(veilFade, new PropertyPath(OpacityProperty));
			sb.Children.Add(veilFade);

			DoubleAnimation tintFade = new(0, 1, TimeSpan.FromMilliseconds(333)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
			Storyboard.SetTarget(tintFade, _veilTint);
			Storyboard.SetTargetProperty(tintFade, new PropertyPath(OpacityProperty));
			sb.Children.Add(tintFade);

			DoubleAnimation fade = new(0d, 1d, TimeSpan.FromMilliseconds(252)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
			Storyboard.SetTarget(fade, _bubble);
			Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));
			sb.Children.Add(fade);

			TransformGroup tg = (TransformGroup)_bubble.RenderTransform;
			ScaleTransform scale = (ScaleTransform)tg.Children[0];

			DoubleAnimation sx = new(0.68, 1, TimeSpan.FromMilliseconds(333)) { EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut } };
			Storyboard.SetTarget(sx, scale);
			Storyboard.SetTargetProperty(sx, new PropertyPath(ScaleTransform.ScaleXProperty));
			sb.Children.Add(sx);

			DoubleAnimation sy = sx.Clone();
			Storyboard.SetTarget(sy, scale);
			Storyboard.SetTargetProperty(sy, new PropertyPath(ScaleTransform.ScaleYProperty));
			sb.Children.Add(sy);

			sb.Begin();
		}, DispatcherPriority.Render);
	}

	public async Task AnimateOut() {
		await Application.Current.Dispatcher.InvokeAsync(new Action(() => {
			_flushTimer.Stop();
			_pending = string.Empty;
			_dirty = false;

			DoubleAnimation veilFade = new(0, TimeSpan.FromMilliseconds(140));
			_veilRoot.BeginAnimation(OpacityProperty, veilFade);

			DoubleAnimation tintFade = new(0, TimeSpan.FromMilliseconds(140));
			_veilTint.BeginAnimation(OpacityProperty, tintFade);

			DoubleAnimation fade = new(0, TimeSpan.FromMilliseconds(140));
			_bubble.BeginAnimation(OpacityProperty, fade);
		}));
	}

	protected override Size ArrangeOverride(Size finalSize) {
		_veilRoot.Measure(finalSize);
		_veilRoot.Arrange(new Rect(new Point(0, 0), finalSize));

		_bubble.Measure(finalSize);
		_bubble.Arrange(new Rect(new Point(0, 0), _bubble.DesiredSize));
		return finalSize;
	}

	protected override int VisualChildrenCount => _visuals.Count;

	protected override Visual GetVisualChild(int index) => _visuals[index];
}