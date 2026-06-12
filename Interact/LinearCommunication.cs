using Microsoft.ML.OnnxRuntimeGenAI;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace OLLM.Interact;

using Microsoft.Extensions.AI;
using OLLM.Memory;
using OLLM.Utility.ModelSpecific;
using State;
using State.Thinking;
using Utility;
using static Constants;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using RichTextBox = System.Windows.Controls.RichTextBox;
using TextBox = System.Windows.Controls.TextBox;

internal partial class LinearCommunication(ModelState modelState, Remember? _memories) {
#pragma warning disable IDE0051
	private readonly OgaHandle _ogaHandle = new();
#pragma warning restore IDE0051
	private readonly CancellationTokenSource _cts = new();
	private bool InterruptButtonEnabled { get; set; } = true;
	private FloatingAdorner? _thought;
	private AdornerLayer? _layer;

	public async Task BeginThinkingOverlayAsync(RichTextBox theirResponse, string text) {
		_layer ??= AdornerLayer.GetAdornerLayer(theirResponse);
		if (_layer is null) {
			return;
		}

		_thought ??= new FloatingAdorner(theirResponse);
		_thought.SetText(text);

		if (!_layer.GetAdorners(theirResponse)?.Contains(_thought) ?? true)
			_layer.Add(_thought);

		await _thought.ShowAtTopRight();
		await _thought.AnimateIn();
	}

	public async Task UpdateThinkingOverlay(string text, CancellationToken ct) {
		if (_thought is null || ct.IsCancellationRequested) {
			return;
		}

		_thought.Append(text);
		await _thought.ShowAtTopRight();
	}

	public async Task EndThinkingOverlay(RichTextBox theirResponse) {
		if (_layer is null || _thought is null) {
			return;
		}

		await _thought.AnimateOut();
		_layer.Remove(_thought);
		_thought = null;
		_layer = null;
	}

	internal async Task _interact(TextBox userInputText, RichTextBox theirResponse, Button chatButton) {
		try {
			ToggleInterruptButton();
			await SendMessage(userInputText.Text, theirResponse);
		} catch (Exception exception) {
			SomethingWentWrong(theirResponse, false, exception.Message);
		} finally {
			AllowUserInputEntry(chatButton);
		}
	}

	internal async Task _interrupt(RichTextBox theirResponse, Button chatButton) {
		if (!InterruptButtonEnabled) {
			return;
		}
		try {
			await _cts.CancelAsync();
			theirResponse.Document = new FlowDocument();
			chatButton.IsEnabled = true;
			ToggleInterruptButton();
			_cts.TryReset();
		} catch (Exception) {
			SomethingWentWrong(theirResponse, false);
		} finally {
			chatButton.IsEnabled = true;
		}
	}

	private async Task SendMessage(string userInputText, RichTextBox theirResponse) {
		string systemAndUserMessage = string.Empty;
		try {
			List<ChatMessage> chatMessages = [
				new (ChatRole.System, _defaultInstruction),
				new (ChatRole.User, userInputText.Trim())
			];

			systemAndUserMessage = MedGemma27B.RenderTemplate(chatMessages.ToArray());
		} catch (Exception) {
			SomethingWentWrong(theirResponse, true);
		}
		await ChatWithModelAsync(systemAndUserMessage, userInputText, theirResponse);
	}

	private async Task ChatWithModelAsync(string systemAndUserMessage, string userMessage, RichTextBox theirResponse) {
		CancellationToken ct = _cts.Token;

		// The 'inner monologue' of the model as they reason
		// Does not contain the initial <think> token
		StringBuilder thinkingTextBuilder = new();
		// Final response or solution to whatever problem they're addressing

		// The flow document that inevitably becomes 'their response'
		FlowDocument flowDoc = new();
		//  ^
		//  |
		Paragraph streamingParagraph = new();
		//  ^
		//  |
		Run streamingRun = new(string.Empty);
		//streamingParagraph.Inlines.Add(streamingRun);
		flowDoc.Blocks.Add(streamingParagraph);

		await Application.Current.Dispatcher.InvokeAsync(() => {
			theirResponse.Document = flowDoc;
			_ = BeginThinkingOverlayAsync(theirResponse, string.Empty);
		}, DispatcherPriority.Render, ct);

		await Task.Run(() => {
			using Sequences sequences = modelState.Tokenizer!.Encode(systemAndUserMessage);
			modelState.SetGeneratorParameterSearchOptions();
			modelState.RefreshGenerator();
			modelState.Generator!.AppendTokenSequences(sequences);
			using TokenizerStream ts = modelState.Tokenizer!.CreateStream();

			bool thinking = true;
			while (!modelState.Generator.IsDone() && !ct.IsCancellationRequested) {
				modelState.Generator.GenerateNextToken();
				string piece = ts.Decode(modelState.Generator.GetSequence(0)[^1]);
				if (piece == _thinkStart) {
					continue;
				}

				switch (thinking) {
					case true when !piece.Contains(_thinkEnd):
						// Thinking
						thinkingTextBuilder.Append(piece);
						Application.Current.Dispatcher.InvokeAsync(() => {
							streamingRun!.Text += piece;
							Task think = UpdateThinkingOverlay(streamingRun!.Text, ct);
							if (think.IsCanceled) {
								theirResponse.Document = new FlowDocument();
							}
						}, DispatcherPriority.Render, ct);
						break;
					default:
						// Construct final response
						thinkingTextBuilder.Append(piece);
						break;
				}
			}
		}, ct);

		string response = thinkingTextBuilder.ToString();

		string[] s = response.Split(_solutionMessage);
		response = s[1];

		await Application.Current.Dispatcher.InvokeAsync(() => {
			_ = EndThinkingOverlay(theirResponse);
			theirResponse.Document = Fd.Render([new ParagraphFdBlockMd([new TextSpan(_writing)])]);
			theirResponse.ScrollToEnd();
		}, DispatcherPriority.Render, ct);

		await Application.Current.Dispatcher.InvokeAsync(() => {
			List<FdBlockMd> finalParagraphBlocks = Md.Parse(response);
			theirResponse.Document = Fd.Render(finalParagraphBlocks);
			theirResponse.ScrollToHome();
		}, DispatcherPriority.Render, ct);

		try {
			await Application.Current.Dispatcher.InvokeAsync(() => {
				if (userMessage.StartsWith(_learnStart)) {
					_ = _memories?.MemorizeDiscussionAsync(response, ct);
				}
			}, DispatcherPriority.Background, ct);
		} catch (Exception memoryException) {
			// Continue;
			MessageBox.Show(memoryException.Message);
		}
	}

	private static void SomethingWentWrong(RichTextBox theirResponse, bool? couldNotParseUserInput = false, string? exceptionMessage = null) {
		theirResponse.Document = new FlowDocument();
		if (exceptionMessage != null) {
			MessageBox.Show(exceptionMessage);
		}
	}

	private void AllowUserInputEntry(Button chatButton) {
		ToggleInterruptButton();
		chatButton.IsEnabled = true;
	}

	private void ToggleInterruptButton() => InterruptButtonEnabled = !InterruptButtonEnabled;

	[GeneratedRegex(@"(\\*\\*[^*]+\\*\\*|\\*[^*]+\\*|`[^`]+\`)", RegexOptions.Singleline)]
	private static partial Regex TokensRegex();

	[GeneratedRegex(@"^(#{1,6})\s+(.*)")]
	private static partial Regex HeadingsRegex();

	#region Unused
	//internal static IEnumerable<Run> ParseMd(IEnumerable<string> tokens) {
	//	bool inBold = false;
	//	bool inItalic = false;
	//	bool inCode = false;

	//	foreach (string token in tokens) {
	//		switch (token) {
	//			case _tss:
	//			case _tse:
	//				inBold = !inBold;
	//				continue;
	//			case _os:
	//			case _ose:
	//				inItalic = !inItalic;
	//				continue;
	//			case _t:
	//				inCode = !inCode;
	//				continue;
	//		}

	//		string remaining = token;
	//		while (remaining.Length > 0) {
	//			int nextBold = remaining.IndexOf(_tss, StringComparison.Ordinal);
	//			int nextItalic = remaining.IndexOf(_os, StringComparison.Ordinal);
	//			int nextCode = remaining.IndexOf(_t, StringComparison.Ordinal);
	//			int nextPos = int.MaxValue;
	//			string? mdIndicator = null;
	//			if (nextBold >= 0 && nextBold < nextPos) { nextPos = nextBold; mdIndicator = _tss; }
	//			if (nextItalic >= 0 && nextItalic < nextPos) { nextPos = nextItalic; mdIndicator = _os; }
	//			if (nextCode >= 0 && nextCode < nextPos) { nextPos = nextCode; mdIndicator = _t; }
	//			if (nextPos == int.MaxValue) {
	//				yield return CreateRun(remaining);
	//				break;
	//			}
	//			if (nextPos > 0) {
	//				yield return CreateRun(remaining[..nextPos]);
	//			}
	//			switch (mdIndicator) {
	//				case _ts:
	//					inBold = !inBold;
	//					break;
	//				case _oss:
	//					inItalic = !inItalic;
	//					break;
	//				case _t:
	//					inCode = !inCode;
	//					break;
	//			}
	//			remaining = remaining[(nextPos + mdIndicator?.Length ?? 1)..];
	//		}
	//	}

	//	yield break;
	//	#region Local functions
	//	Run CreateRun(string text) {
	//		Run run = new(text);
	//		if (inBold) {
	//			run.FontWeight = FontWeights.Bold;
	//		}
	//		if (inItalic) {
	//			run.FontStyle = FontStyles.Italic;
	//		}
	//		if (inCode) {
	//			run.FontFamily = _fontFamily0x;
	//			run.Background = _owd;
	//		}
	//		return run;
	//	}
	//	#endregion
	//}
	#endregion
}