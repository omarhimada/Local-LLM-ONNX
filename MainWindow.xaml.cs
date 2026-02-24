using OLLM.Interact;
using OLLM.Memory;
using OLLM.SD;
using OLLM.State;
using OLLM.Utility;
using System.Windows;
using System.Windows.Controls;

namespace OLLM;

using static Constants;

internal partial class MainWindow : Window {
	#region Fields & Properties
	internal Remember? Memories;
	internal ModelState? ModelState;
	internal EmbedderState? EmbedderState;
	internal MiniEmbedder? MiniEmbedder;
	internal LinearCommunication? LinearCommunication;
	#endregion
	#region Initialization
	internal void Initialize(ModelState modelState, EmbedderState embedderState, MiniEmbedder miniEmbedder) {
		ModelState = modelState;
		EmbedderState = embedderState;
		MiniEmbedder = miniEmbedder;
		LinearCommunication = new(ModelState);
		try {
			Memories = new Remember(MiniEmbedder!);
		} catch (Exception) {
			// TODO ""Vector property 'Vector' has type 'GeneratedEmbeddings<Embedding<float>>' which isn't supported by your provider, and no embedding generator is configured. Configure a generator that supports converting 'GeneratedEmbeddings<Embedding<float>>' to vector type supported by your provider.""
			// MessageBox.Show(exception.Message);
			// Fail silently, continue
		}
	}
	internal MainWindow() {
		InitializeComponent();
		Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
	}
	#endregion
	internal async void ChatButtonClick(object sender, RoutedEventArgs e) {
		_thinking();
		await Task.Yield();
		await LinearCommunication!._interact(UserInputText, TheirResponse, ChatButton);
		_doneThinking();
	}
	internal async void InterruptButtonClick(object sender, RoutedEventArgs e) {
		await LinearCommunication!._interrupt(TheirResponse, ChatButton);
		_doneThinking();
	}

	internal async void SDButtonClick(object sender, RoutedEventArgs e) {
		try {
			string positive = Base64e.DecodeFromBase64(_sdPrompt);
			string negative = Base64e.DecodeFromBase64(_sdNegative);

			Diffusion.Diffuse(new DiffusionOptions {
				Prompt = positive,
				Negative = negative
			});
		} catch (Exception sdException) {
			MessageBox.Show(sdException.Message);
			if (sdException.InnerException is not null) {
				MessageBox.Show(sdException.InnerException!.Message);
			}
		}
		_doneThinking();
	}
	internal void CloseButtonClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
	private void CodeModeToggled(object sender, RoutedEventArgs e) {
		if (sender is not CheckBox checkBox) {
			return;
		}
		ModelState?.ExpectingCodeResponse = checkBox.IsChecked ?? false;
	}
	#region 'thinking' animation
	private void _thinking() {
		Thinking.IsEnabled = true;
		Thinking.Visibility = Visibility.Visible;
	}
	private void _doneThinking() {
		Thinking.IsEnabled = false;
		Thinking.Visibility = Visibility.Hidden;
	}
	#endregion
}