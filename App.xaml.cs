using OLLM.Memory;
using OLLM.State;
using System.Drawing;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System;
using System.Drawing;
using System.Windows.Forms;
using static OLLM.Constants;
using static OLLM.Initialization.EnsureModelsArePresent;
using Point = System.Drawing.Point;
using FontStyle = System.Drawing.FontStyle;

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

			mainWindow.Show();
			FinishedInitializing();
		} catch (Exception exception) {
			System.Windows.MessageBox.Show($"{_userFriendlyErrorOccurredDuringInitialization}\r\n{exception.Message}");
			Shutdown();
		}
	}

	internal static Thread? blurAreaFormsThread = new Thread(() => {
		System.Windows.Forms.Application.EnableVisualStyles();
		System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
		System.Windows.Forms.Application.Run(new FloatingBlurButton());
	});

	internal static Thread? overlayFormsThread = new Thread(() => {
		System.Windows.Forms.Application.EnableVisualStyles();
		System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
		System.Windows.Forms.Application.Run(new OverlayForm());
	});

	internal static void FinishedInitializing() {
		LoadingWindow.Hide();

		overlayFormsThread.SetApartmentState(ApartmentState.STA);
		overlayFormsThread.IsBackground = true;
		overlayFormsThread.Start();

		blurAreaFormsThread.SetApartmentState(ApartmentState.STA);
		blurAreaFormsThread.IsBackground = true;
		blurAreaFormsThread.Start();
	}
}



public class FloatingBlurButton : Form {
	private const int HOTKEY_ID = 9000;
	private const uint MOD_CONTROL = 0x0002;
	private const uint MOD_SHIFT = 0x0004;

	private readonly System.Windows.Forms.Button blurButton;

	private Rectangle watchedRegion;
	private Color lastAverage = Color.Black;
	private BlurRectangleOverlay? activeOverlay;

	public FloatingBlurButton() {
		FormBorderStyle = FormBorderStyle.None;
		TopMost = true;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.Manual;
		BackColor = Color.FromArgb(32, 32, 32);
		Bounds = new Rectangle(40, 140, 260, 70);

		var layout = new TableLayoutPanel {
			Dock = DockStyle.Fill,
			RowCount = 2,
			ColumnCount = 1,
			BackColor = Color.FromArgb(32, 32, 32),
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};

		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

		blurButton = new System.Windows.Forms.Button {
			Text = "Blur Area (Ctrl+Shift+B)",
			Dock = DockStyle.Fill,
			FlatStyle = FlatStyle.Flat,
			UseVisualStyleBackColor = false,
			BackColor = Color.FromArgb(45, 45, 48),
			ForeColor = Color.White,
			Font = new Font("Consolas", 10f, System.Drawing.FontStyle.Bold),
			Cursor = Cursors.Hand,
			TabStop = false,
			Margin = Padding.Empty
		};

		blurButton.FlatAppearance.BorderSize = 1;
		blurButton.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
		blurButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 64);
		blurButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 30, 30);

		blurButton.Click += (_, _) => StartRectangleSelection();

		blurButton.Resize += (_, _) => {
			blurButton.Region = new Region(
				new Rectangle(0, 0, blurButton.Width, blurButton.Height)
			);
		};

		layout.Controls.Add(blurButton, 0, 0);
		Controls.Add(layout);
	}

	private void StartRectangleSelection() {
		using var selector = new RectangleSelector();

		if (selector.ShowDialog() == DialogResult.OK &&
			selector.SelectedRectangle.Width > 0 &&
			selector.SelectedRectangle.Height > 0) {
			watchedRegion = selector.SelectedRectangle;

			activeOverlay?.Close();
			activeOverlay = new BlurRectangleOverlay(watchedRegion);
			activeOverlay.Show();
		}
	}

	protected override void OnShown(EventArgs e) {
		base.OnShown(e);

		Region = new Region(new Rectangle(0, 0, Width, Height));
		blurButton.Region = new Region(new Rectangle(0, 0, blurButton.Width, blurButton.Height));
	}

	protected override void OnHandleCreated(EventArgs e) {
		base.OnHandleCreated(e);

		RegisterHotKey(
			Handle,
			HOTKEY_ID,
			MOD_CONTROL | MOD_SHIFT,
			(uint)Keys.B
		);
	}

	protected override void WndProc(ref Message message) {
		const int WM_HOTKEY = 0x0312;

		if (message.Msg == WM_HOTKEY && message.WParam.ToInt32() == HOTKEY_ID) {
			blurButton.PerformClick();
		}

		base.WndProc(ref message);
	}

	protected override void OnFormClosing(FormClosingEventArgs e) {
		UnregisterHotKey(Handle, HOTKEY_ID);
		activeOverlay?.Close();

		base.OnFormClosing(e);
	}

	[DllImport("user32.dll")]
	private static extern bool RegisterHotKey(
		IntPtr hWnd,
		int id,
		uint fsModifiers,
		uint vk
	);

	[DllImport("user32.dll")]
	private static extern bool UnregisterHotKey(
		IntPtr hWnd,
		int id
	);
}

public class RectangleSelector : Form {
	public Rectangle SelectedRectangle { get; private set; }

	private System.Drawing.Point startPoint;
	private Rectangle currentRectangle;
	private bool isDragging;

	public RectangleSelector() {
		FormBorderStyle = FormBorderStyle.None;
		WindowState = FormWindowState.Maximized;
		TopMost = true;
		ShowInTaskbar = false;
		BackColor = Color.Black;
		Opacity = 0.15;
		Cursor = Cursors.Cross;
		DoubleBuffered = true;
	}

	protected override void OnMouseDown(MouseEventArgs e) {
		isDragging = true;
		startPoint = e.Location;
		currentRectangle = new Rectangle(e.Location,
								   size: System.Drawing.Size.Empty);
		Invalidate();
	}

	protected override void OnMouseMove(MouseEventArgs e) {
		if (!isDragging)
			return;

		currentRectangle = GetRectangle(startPoint, e.Location);
		Invalidate();
	}

	protected override void OnMouseUp(MouseEventArgs e) {
		isDragging = false;
		SelectedRectangle = GetRectangle(startPoint, e.Location);

		DialogResult = DialogResult.OK;
		Close();
	}

	protected override void OnPaint(PaintEventArgs e) {
		using var pen = new Pen(Color.Red, 2);
		e.Graphics.DrawRectangle(pen, currentRectangle);
	}

	private static Rectangle GetRectangle(System.Drawing.Point firstPoint, System.Drawing.Point secondPoint) {
		return new Rectangle(
			Math.Min(firstPoint.X, secondPoint.X),
			Math.Min(firstPoint.Y, secondPoint.Y),
			Math.Abs(firstPoint.X - secondPoint.X),
			Math.Abs(firstPoint.Y - secondPoint.Y)
		);
	}
}
public class OverlayForm : Form {
	private readonly System.Windows.Forms.Timer watchTimer = new();
	private readonly System.Windows.Forms.Timer messageTimer = new();

	private readonly Rectangle watchedRegion = new Rectangle(100, 100, 100, 100);

	private Bitmap? previousFrame;
	private bool showMessage;

	public OverlayForm() {
		FormBorderStyle = FormBorderStyle.None;
		TopMost = true;
		ShowInTaskbar = false;
		BackColor = Color.Magenta;
		TransparencyKey = Color.Magenta;
		Bounds = Screen.PrimaryScreen!.Bounds;
		DoubleBuffered = true;

		watchTimer.Interval = 250;
		watchTimer.Tick += (_, _) => CheckPixelChange();

		messageTimer.Interval = 1200;
		messageTimer.Tick += (_, _) => {
			messageTimer.Stop();
			showMessage = false;
			Invalidate();
		};

		Shown += (_, _) => {
			previousFrame = CaptureRegion(watchedRegion);
			watchTimer.Start();
		};
	}

	protected override CreateParams CreateParams {
		get {
			CreateParams parameters = base.CreateParams;
			parameters.ExStyle |= 0x80000;    // WS_EX_LAYERED
			parameters.ExStyle |= 0x20;       // WS_EX_TRANSPARENT
			parameters.ExStyle |= 0x80;       // WS_EX_TOOLWINDOW
			parameters.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
			return parameters;
		}
	}

	private void CheckPixelChange() {
		using Bitmap currentFrame = CaptureRegion(watchedRegion);

		if (previousFrame != null &&
			HasMeaningfulChange(previousFrame, currentFrame)) {
			showMessage = true;
			Invalidate();

			messageTimer.Stop();
			messageTimer.Start();
		}

		previousFrame?.Dispose();
		previousFrame = (Bitmap)currentFrame.Clone();
	}

	private static Bitmap CaptureRegion(Rectangle region) {
		Bitmap bitmap = new Bitmap(region.Width, region.Height);

		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.CopyFromScreen(
			region.Location,
			System.Drawing.Point.Empty,
			region.Size,
			CopyPixelOperation.SourceCopy
		);

		return bitmap;
	}

	private static bool HasMeaningfulChange(Bitmap previous, Bitmap current) {
		int changedPixels = 0;

		const int colorDiffThreshold = 30;
		const int requiredChangedPixels = 25;
		const int sampleStep = 2;

		int width = Math.Min(previous.Width, current.Width);
		int height = Math.Min(previous.Height, current.Height);

		for (int x = 0; x < width; x += sampleStep) {
			for (int y = 0; y < height; y += sampleStep) {
				Color a = previous.GetPixel(x, y);
				Color b = current.GetPixel(x, y);

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

	protected override void OnPaint(PaintEventArgs e) {
		using Pen pen = new Pen(Color.Red, 2);
		e.Graphics.DrawRectangle(pen, watchedRegion);

		if (!showMessage)
			return;

		using Font font = new Font("Segoe UI", 18, FontStyle.Bold);

		//// Important: this is outside the watched 100,100,100,100 area
		//e.Graphics.DrawString(
		//	"Pixel change detected",
		//	font,
		//	Brushes.Lime,
		//	100,
		//	230
		//);
	}

	protected override void OnFormClosed(FormClosedEventArgs e) {
		watchTimer.Stop();
		messageTimer.Stop();

		previousFrame?.Dispose();

		watchTimer.Dispose();
		messageTimer.Dispose();

		base.OnFormClosed(e);
	}
}

public class BlurRectangleOverlay : Form {
	public BlurRectangleOverlay(Rectangle bounds) {
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.Manual;
		Bounds = bounds;
		TopMost = true;
		ShowInTaskbar = false;
		BackColor = Color.Black;
		Opacity = 0.55;

		Region = new Region(new Rectangle(0, 0, bounds.Width, bounds.Height));
	}

	protected override CreateParams CreateParams {
		get {
			CreateParams parameters = base.CreateParams;

			parameters.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
			parameters.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
			parameters.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE

			return parameters;
		}
	}
}