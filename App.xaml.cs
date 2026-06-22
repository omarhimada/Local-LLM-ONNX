using OLLM.Memory;
using OLLM.State;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static OLLM.Constants;
using static OLLM.Initialization.EnsureModelsArePresent;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using FontFamily = System.Windows.Media.FontFamily;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace OLLM;

internal partial class App : System.Windows.Application {
	internal ModelState? ModelState;
	internal EmbedderState? EmbedderState;
	internal MiniEmbedder? MiniEmbedder;
	internal static readonly LoadingWindow LoadingWindow = new();

	private static Task _animateLabelIn(System.Windows.Controls.Label label) {
		TaskCompletionSource tcs = new();
		Current.Dispatcher.Invoke(() => {
			DoubleAnimation fade = new(0, 1, TimeSpan.FromMilliseconds(1000)) {
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
			};
			fade.Completed += (s, e) => tcs.SetResult(); // Signal when animation is done
			label.BeginAnimation(UIElement.OpacityProperty, fade);
		});
		return tcs.Task;
	}

	protected override async void OnStartup(StartupEventArgs e) {
		base.OnStartup(e);

		AppContext.SetSwitch(_appContextSwitchForSelectionBrush, false);
		LoadingWindow.Show();
		LoadingWindow.Activate();

		await _animateLabelIn(LoadingWindow.LoadingLabel);

		(string? modelPath, string? embedModelPath) = (null, null);
		try {
			// Ensure background work doesn't choke the UI thread during the fade
			await Task.Run(() => {
				(modelPath, embedModelPath) = EnsureRequiredModelsArePresent();
			});

			if (modelPath == null) {
				Current.Shutdown();
				return;
			}

			await _animateLabelIn(LoadingWindow.FoundRequiredModelsLabel);

			// Wrap State initialization in Task.Run to keep UI responsive for animations
			await Task.Run(() => {
				ModelState = new(modelPath!);
				EmbedderState = new(embedModelPath);
				MiniEmbedder = new(ModelState, EmbedderState);
			});

			await _animateLabelIn(LoadingWindow.InitializingLabel);

			MainWindow mainWindow = new();
			mainWindow.Initialize(ModelState!, EmbedderState!, MiniEmbedder!);
			MainWindow = mainWindow;

			// Start floating blur window on its own STA UI thread
			var blurAreaFormsThread = new Thread(() =>
			{
				var blurWindow = new FloatingBlurButton();

				blurWindow.Show();

				// Creates a Dispatcher message loop for this thread
				System.Windows.Threading.Dispatcher.Run();
			});

			blurAreaFormsThread.SetApartmentState(ApartmentState.STA);
			blurAreaFormsThread.IsBackground = true;
			blurAreaFormsThread.Start();// Start floating blur window on its own STA UI thread

			var overlayThread = new Thread(() => {
				var overlay = new OverlayWindow();

				overlay.Show();

				// Creates a Dispatcher message loop for this thread
				System.Windows.Threading.Dispatcher.Run();
			});

			overlayThread.SetApartmentState(ApartmentState.STA);
			overlayThread.IsBackground = true;
			overlayThread.Start();

			mainWindow.Show();
			FinishedInitializing();
		} catch (Exception exception) {
			System.Windows.MessageBox.Show($"{_userFriendlyErrorOccurredDuringInitialization}\r\n{exception.Message}");
			Shutdown();
		}
	}

	internal static void FinishedInitializing() => LoadingWindow.Hide();
}

public sealed class FloatingBlurButton : Window {
	private const int HOTKEY_ID = 9000;
	private const uint MOD_CONTROL = 0x0002;
	private const uint MOD_SHIFT = 0x0004;
	private const int WM_HOTKEY = 0x0312;

	private readonly Button blurButton;

	private Rect watchedRegion;
	private BlurRectangleOverlay? activeOverlay;

	public FloatingBlurButton() {
		Width = 145;
		Height = 28;

		Left = 15;
		Top = 15;

		WindowStyle = WindowStyle.None;
		ResizeMode = ResizeMode.NoResize;
		ShowInTaskbar = false;
		Topmost = true;

		Background = new SolidColorBrush(
			Color.FromRgb(225, 225, 225));

		Grid grid = new();

		blurButton = new Button {
			Content = "Blur Area (Ctrl+Shift+B)",
			FontFamily = new FontFamily("Consolas"),
			FontSize = 10,
			FontWeight = FontWeights.Bold
		};

		blurButton.Click += (_, _) => StartRectangleSelection();

		grid.Children.Add(blurButton);

		Content = grid;
	}

	protected override void OnSourceInitialized(EventArgs e) {
		base.OnSourceInitialized(e);

		IntPtr handle = new WindowInteropHelper(this).Handle;

		RegisterHotKey(
			handle,
			HOTKEY_ID,
			MOD_CONTROL | MOD_SHIFT,
			(uint)KeyInterop.VirtualKeyFromKey(
				System.Windows.Input.Key.B));

		HwndSource source =
			HwndSource.FromHwnd(handle)!;

		source.AddHook(WndProc);
	}

	private IntPtr WndProc(
		IntPtr hwnd,
		int msg,
		IntPtr wParam,
		IntPtr lParam,
		ref bool handled) {
		if (msg == WM_HOTKEY &&
			wParam.ToInt32() == HOTKEY_ID) {
			blurButton.RaiseEvent(
				new RoutedEventArgs(Button.ClickEvent));

			handled = true;
		}

		return IntPtr.Zero;
	}

	private void StartRectangleSelection() {
		RectangleSelector selector = new();

		if (selector.ShowDialog() == true) {
			watchedRegion = selector.SelectedRectangle;

			activeOverlay?.Close();

			activeOverlay =
				new BlurRectangleOverlay(watchedRegion);

			activeOverlay.Show();
		}
	}

	protected override void OnClosed(EventArgs e) {
		IntPtr handle =
			new WindowInteropHelper(this).Handle;

		UnregisterHotKey(handle, HOTKEY_ID);

		activeOverlay?.Close();

		base.OnClosed(e);
	}

	[DllImport("user32.dll")]
	private static extern bool RegisterHotKey(
		IntPtr hWnd,
		int id,
		uint fsModifiers,
		uint vk);

	[DllImport("user32.dll")]
	private static extern bool UnregisterHotKey(
		IntPtr hWnd,
		int id);
}

public sealed class RectangleSelector : Window {
	public Rect SelectedRectangle { get; private set; }

	private Point startPoint;
	private Rect currentRect;
	private bool dragging;

	public RectangleSelector() {
		WindowStyle = WindowStyle.None;
		WindowState = WindowState.Maximized;

		Topmost = true;
		ShowInTaskbar = false;

		AllowsTransparency = true;

		Background = new SolidColorBrush(
			Color.FromArgb(
				90, // alpha
				0,
				0,
				0));
		Cursor = Cursors.Cross;
	}

	protected override void OnMouseDown(MouseButtonEventArgs e) {
		dragging = true;

		startPoint = e.GetPosition(this);

		InvalidateVisual();
	}

	protected override void OnMouseMove(MouseEventArgs e) {
		if (!dragging)
			return;

		currentRect = CreateRect(
			startPoint,
			e.GetPosition(this));

		InvalidateVisual();
	}

	protected override void OnMouseUp(MouseButtonEventArgs e) {
		dragging = false;

		SelectedRectangle =
			CreateRect(startPoint, e.GetPosition(this));

		DialogResult = true;
		Close();
	}

	protected override void OnRender(DrawingContext dc) {
		base.OnRender(dc);

		dc.DrawRectangle(
			null,
			new Pen(Brushes.Red, 2),
			currentRect);
	}

	private static Rect CreateRect(
		Point p1,
		Point p2) {
		return new Rect(
			new Point(
				Math.Min(p1.X, p2.X),
				Math.Min(p1.Y, p2.Y)),
			new Point(
				Math.Max(p1.X, p2.X),
				Math.Max(p1.Y, p2.Y)));
	}
}

public sealed partial class OverlayWindow : Window {

	private Bitmap? previousFrame;
	private bool showMessage;

	public OverlayWindow() {
		WindowStyle = WindowStyle.None;
		ResizeMode = ResizeMode.NoResize;

		Topmost = true;
		ShowInTaskbar = false;

		AllowsTransparency = true;
		Background = Brushes.Transparent;

		Left = 0;
		Top = 0;

		Width = SystemParameters.PrimaryScreenWidth;
		Height = SystemParameters.PrimaryScreenHeight;
	}

	protected override void OnSourceInitialized(EventArgs e) {
		base.OnSourceInitialized(e);

		nint hwnd = new WindowInteropHelper(this).Handle;

		nint style = GetWindowLongPtr(hwnd, GWL_EXSTYLE);

		SetWindowLongPtr(
			hwnd,
			GWL_EXSTYLE,
			style
			| WS_EX_LAYERED
			| WS_EX_TRANSPARENT
			| WS_EX_TOOLWINDOW
			| WS_EX_NOACTIVATE);
	}

	protected override void OnClosed(EventArgs e) {
		previousFrame?.Dispose();

		base.OnClosed(e);
	}

	private static Bitmap CaptureRegion(Rectangle region) {
		Bitmap bitmap = new(
			region.Width,
			region.Height);

		using Graphics graphics =
			Graphics.FromImage(bitmap);

		graphics.CopyFromScreen(
			region.Location,
			System.Drawing.Point.Empty,
			region.Size,
			CopyPixelOperation.SourceCopy);

		return bitmap;
	}

	private static bool HasMeaningfulChange(
		Bitmap previous,
		Bitmap current) {
		int changedPixels = 0;

		const int colorDiffThreshold = 30;
		const int requiredChangedPixels = 25;
		const int sampleStep = 2;

		int width =
			Math.Min(previous.Width, current.Width);

		int height =
			Math.Min(previous.Height, current.Height);

		for (int x = 0; x < width; x += sampleStep) {
			for (int y = 0; y < height; y += sampleStep) {
				System.Drawing.Color a =
					previous.GetPixel(x, y);

				System.Drawing.Color b =
					current.GetPixel(x, y);

				int diff =
					Math.Abs(a.R - b.R) +
					Math.Abs(a.G - b.G) +
					Math.Abs(a.B - b.B);

				if (diff > colorDiffThreshold) {
					changedPixels++;

					if (changedPixels >= requiredChangedPixels)
						return true;
				}
			}
		}

		return false;
	}

	private static Rectangle ToRectangle(Rect rect) {
		return new Rectangle(
			(int)rect.X,
			(int)rect.Y,
			(int)rect.Width,
			(int)rect.Height);
	}

	private const int GWL_EXSTYLE = -20;

	private const nint WS_EX_LAYERED = 0x80000;
	private const nint WS_EX_TRANSPARENT = 0x20;
	private const nint WS_EX_TOOLWINDOW = 0x80;
	private const nint WS_EX_NOACTIVATE = 0x08000000;

	[LibraryImport(
		"user32.dll",
		EntryPoint = "GetWindowLongPtrW",
		SetLastError = true)]
	private static partial nint GetWindowLongPtr(
		nint hWnd,
		int nIndex);

	[LibraryImport(
		"user32.dll",
		EntryPoint = "SetWindowLongPtrW",
		SetLastError = true)]
	private static partial nint SetWindowLongPtr(
		nint hWnd,
		int nIndex,
		nint dwNewLong);
}

public sealed partial class BlurRectangleOverlay : Window {
	private const int GWL_EXSTYLE = -20;

	private const nint WS_EX_TRANSPARENT = 0x20;
	private const nint WS_EX_TOOLWINDOW = 0x80;
	private const nint WS_EX_NOACTIVATE = 0x08000000;

	public BlurRectangleOverlay(Rect bounds) {
		Left = bounds.X;
		Top = bounds.Y;

		Width = bounds.Width;
		Height = bounds.Height;

		WindowStyle = WindowStyle.None;
		ResizeMode = ResizeMode.NoResize;

		ShowInTaskbar = false;
		Topmost = true;

		AllowsTransparency = true;

		// Equivalent to:
		// BackColor = Black
		// Opacity = 0.55
		Background = new SolidColorBrush(
			Color.FromArgb(
				(byte)(0.88 * 255),
				0,
				0,
				0));

		// Optional:
		Focusable = false;
	}

	protected override void OnSourceInitialized(EventArgs e) {
		base.OnSourceInitialized(e);

		nint hwnd = new WindowInteropHelper(this).Handle;

		nint style = GetWindowLongPtr(
			hwnd,
			GWL_EXSTYLE);

		SetWindowLongPtr(
			hwnd,
			GWL_EXSTYLE,
			style
			| WS_EX_TRANSPARENT
			| WS_EX_TOOLWINDOW
			| WS_EX_NOACTIVATE);
	}

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern nint GetWindowLongPtr(
	nint hWnd,
	int nIndex);


	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	private static extern nint SetWindowLongPtr(
		nint hWnd,
		int nIndex,
		nint dwNewLong);
}