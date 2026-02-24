using OLLM.Memory;
using OLLM.State;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static OLLM.Constants;
using static OLLM.Initialization.EnsureModelsArePresent;

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
			MessageBox.Show($"{_userFriendlyErrorOccurredDuringInitialization}\r\n{exception.Message}");
			Shutdown();
		}
	}

	internal static void FinishedInitializing() => LoadingWindow.Hide();
}