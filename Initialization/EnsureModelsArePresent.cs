using System.IO;
using System.Text;
using System.Windows;
using static OLLM.Constants;
namespace OLLM.Initialization;

internal static class EnsureModelsArePresent {
	internal const bool _enableAutoTemplateConvert = false;
	internal const bool _embedModelIsRequired = false;

	/// <summary>
	/// All required models are present and accessible.
	/// </summary>
	internal static (string? modelPath, string? embedModelPath) EnsureRequiredModelsArePresent() {
		string? modelPathToReturn = null;
		// Construct a detailed message to show the user if neither of the required models could be found.
		StringBuilder potentialFriendlyUserErrorMessage = new();
		#region Grab the chat_template from the tokenizer_config.json, convert it to C#, then remove it and never call it again
		if (_enableAutoTemplateConvert) {
			//// Find the tokenizer_config.json
			//string? tokenizerJsonPath = Converter.FindTokenizerConfig(_preBuildPhi4ModelPath);
			//if (tokenizerJsonPath == null) {
			//	MessageBox.Show(potentialFriendlyUserErrorMessage.ToString(),
			//		_userFriendlyMissingTokenizerConfigJson);
			//	return (null, null);
			//}
			//(string chatTemplate, string csName) = (null, null);
			//try {
			//	// Check if it has a chat_template
			//	(chatTemplate, csName) = Converter.ReadChatTemplateFromTokenizerConfig(tokenizerJsonPath);
			//	const string verbatimNewLineAndFourSpaces = @"\n    ";
			//	const string nonVerbatimNewLineWithArbitraryWhitespace = "\n        ";
			//	const string nonVerbatimReturnNewline = "\r\n    ";
			//	const char tab = '\t';
			//	// "\n    "			verbatim new line and then four whitespaces <== remove.
			//	// "\n        "		actual new line and whitespace <== remove.
			//	// "\r\n    "		actual return and new line <== remove.
			//	// \t				r
			//	chatTemplate = chatTemplate.Replace(verbatimNewLineAndFourSpaces, string.Empty);
			//	chatTemplate = chatTemplate.Replace(nonVerbatimNewLineWithArbitraryWhitespace, string.Empty);
			//	chatTemplate = chatTemplate.Replace(nonVerbatimReturnNewline, string.Empty);
			//	chatTemplate = chatTemplate.Replace($"{tab}", string.Empty);
			//} catch (InvalidOperationException) {
			//	// It is possible we already converted the chat_template to C# dynamically. 
			//} finally {
			//	if (chatTemplate != null && csName != null) {
			//		// We haven't done this yet, there is a chat_template in the tokenizer_config.json. Remove it.
			//		string csharpOutputPath = Converter.WriteOutput(tokenizerJsonPath!);
			//		if (!string.IsNullOrEmpty(csharpOutputPath)) {
			//			// Made the C# chat dynamically, remove the chat_template from the tokenizer_config.json
			//			using FileStream stream = File.OpenRead(tokenizerJsonPath);
			//			using JsonDocument doc = JsonDocument.Parse(stream);
			//			JsonObject? jsonRoot = JsonObject.Create(doc.RootElement);
			//			// jsonRoot has the chat_template removed - now overwrite the tokenizer_config.json
			//			if (jsonRoot != null) {
			//				jsonRoot.Remove(_chatTemplateKey);
			//				string updatedJson = jsonRoot.ToJsonString(new JsonSerializerOptions {
			//					WriteIndented = true
			//				});
			//				File.WriteAllText(tokenizerJsonPath, updatedJson);
			//			}
			//		}
			//	} else {
			//		// We've already removed the chat_template, we can assume the generated C# is available for the model we want to use.
			//		// TODO move the generated .cs to one of the project directories
			//	}
			//}
		}
		#endregion
		// Attempt to retrieve the LLM ONNX
		if (!TryRequiredModelIsPresent(_preBuildGPT20BModelPath, out string? modelPathToUse) && modelPathToUse == null) {
			potentialFriendlyUserErrorMessage.AppendLine(
				$"{_userFriendlyModelDirectoryErrorResponse}{Environment.NewLine}{modelPathToUse}");
		}
		// Attempt to retrieve the embedding model ONNX
		if (!TryRequiredModelIsPresent(_preBuildEmbedModelDirectory, out string? embedModelPathToUse) ||
			embedModelPathToUse == null) {
			if (_embedModelIsRequired) {
#pragma warning disable CS0162 // Unreachable code detected
				potentialFriendlyUserErrorMessage.AppendLine(
					$"{_userFriendlyModelDirectoryErrorResponse}{Environment.NewLine}{embedModelPathToUse}");
#pragma warning restore CS0162 // Unreachable code detected
			}
		}
		if (modelPathToReturn == null && (_embedModelIsRequired && embedModelPathToUse == null)) {
			MessageBox.Show(potentialFriendlyUserErrorMessage.ToString(),
				_userFriendlyErrorOccurredTryingToLoadModels);
		}
		return (modelPathToUse!, embedModelPathToUse);
	}

	/// <summary>
	/// The required model directories are present at either the specified debug path or its published location.
	/// </summary>
	internal static bool TryRequiredModelIsPresent(string debugPath, out string? pathToUse) {
		pathToUse = null;
		if (debugPath == _preBuildEmbedModelDirectory) {
			#region Verify embed model is present (check for debug mode first, then assume release/published)
			if (!Directory.Exists(debugPath)) {
				// Try the embed model from the published directory instead of the debugging directory:
				pathToUse = debugPath.TrimStart($"{AppContext.BaseDirectory}..\\..\\..").ToString();
				if (!Directory.Exists(pathToUse)) {
					// If we still cannot find the required model(s) then show the user a friendly message and return false.
					MessageBox.Show($"{_userFriendlyModelDirectoryErrorResponse}{pathToUse}");
					return false;
				}
			} else {
				pathToUse = debugPath;
			}
			#endregion
		} else {
			#region Verify LLM is present (check for debug mode first, then assume release/published)
			if (!Directory.Exists(debugPath)) {
				// Assume the program is published and the directory is close to the executable.
				string publishedLocation = debugPath.TrimStart($"{AppContext.BaseDirectory}..\\..\\..").ToString();
				if (!Directory.Exists(publishedLocation)) {
					// If we still cannot find the required model(s) then show the user a friendly message and return false.
					MessageBox.Show($"{_userFriendlyModelDirectoryErrorResponse}{publishedLocation}");
					return false;
				}
			} else {
				pathToUse = debugPath;
			}
			#endregion
		}
		return pathToUse != null;
	}
}