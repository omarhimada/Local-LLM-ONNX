using OLLM.Memory;
using OLLM.State;
using OLLM.Utility;
using OLLM.Interact;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static OLLM.Constants;
using static OLLM.Initialization.EnsureModelsArePresent;
using Application = System.Windows.Application;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;

namespace OLLM;

internal partial class App : Application {
	internal ModelState? ModelState;
	internal EmbedderState? EmbedderState;
	internal MiniEmbedder? MiniEmbedder;
	internal static readonly LoadingWindow LoadingWindow = new();

	private static Task _animateLabelIn(Label label) {
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

		await _animateLabelIn(LoadingWindow.CheckingForCUDALabel);
		string returnMessage = CheckForCUDA.GetAvailableProviders();
		if (returnMessage != null) {
			LoadingWindow.CheckingForCUDALabel.Content = returnMessage;
		}

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
			var blurAreaFormsThread = new Thread(() => {
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
			MessageBox.Show($"{_userFriendlyErrorOccurredDuringInitialization}\r\n{exception.Message}");
			Shutdown();
		}
	}

	internal static void FinishedInitializing() => LoadingWindow.Hide();
}