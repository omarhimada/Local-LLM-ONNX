using Microsoft.ML.OnnxRuntime;
using System.IO;
using System.Windows;
using static OLLM.Constants;
namespace OLLM.State;

internal class EmbedderState {
	#region Fields & properties
	internal InferenceSession Session { get; set; }
	internal Dictionary<string, int> Vocabulary = [];
	internal string InputIdsName { get; set; }
	internal string AttentionMaskName { get; set; }
	internal string PreferredOutputName { get; set; }
	internal int PadId { get; set; }
	internal int ClsId { get; set; }
	internal int UnkId { get; set; }
	internal int SepId { get; set; }
	internal readonly string? EmbedModelDirectory;
	internal readonly string VocabularyPath;
	internal const int _maxTokenLength = 4096;
	#endregion
	internal EmbedderState(string? embedModelDirectory) {
		EmbedModelDirectory = embedModelDirectory;
		SessionOptions options = new();
		#region Point to the direct ONNX model itself to instantiate the inference session
		string? embedModelFilePath = Directory.GetFiles(embedModelDirectory ?? _ws, _onnxSearch).FirstOrDefault();
		if (string.IsNullOrEmpty(embedModelFilePath)) {
			MessageBox.Show(_userFriendlyErrorOccurredDuringInitialization);
			Application.Current.Shutdown();
		}
		#endregion
		Session = new InferenceSession(embedModelFilePath, options);
		#region Initialize vocabulary
		VocabularyPath = Initialization.Vocabulary.GetRequiredTextDocument(_preBuildEmbedModelVocabTextPath) ?? string.Empty;
		if (string.IsNullOrEmpty(VocabularyPath)) {
			// User was already shown a friendly error message, shut down the app.
			Application.Current.Shutdown();
		}
		Vocabulary = new(StringComparer.Ordinal);
		int i = 0;
		foreach (string line in File.ReadLines(VocabularyPath)) {
			string? token = line?.Trim();
			if (string.IsNullOrEmpty(token)) {
				continue;
			}
			Vocabulary[token] = i++;
		}
		#endregion
		List<string> outputs = Session.OutputMetadata.Keys.ToList();
		PreferredOutputName =
			outputs.FirstOrDefault(n => n.Contains(_pooled, StringComparison.OrdinalIgnoreCase))
			?? outputs.FirstOrDefault(n => n.Contains(_hidden, StringComparison.OrdinalIgnoreCase))
			?? outputs.First();
		InputIdsName =
			Session.InputMetadata.Keys
				.FirstOrDefault(key =>
					key.Contains(_inputIds, StringComparison.OrdinalIgnoreCase))
			?? Session.InputMetadata.Keys.First();
		AttentionMaskName =
			Session.InputMetadata.Keys
				.FirstOrDefault(key =>
					key.Contains(_attentionMask, StringComparison.OrdinalIgnoreCase))
			?? Session.InputMetadata.Keys.Skip(1).FirstOrDefault()
			?? InputIdsName;
		PadId = GetIdOrDefault(_pad, 0);
		ClsId = GetIdOrDefault(_cls, 1);
		SepId = GetIdOrDefault(_sep, 2);
		UnkId = GetIdOrDefault(_unk, 3);
	}
	internal int GetIdOrDefault(string token, int fallback) =>
		Vocabulary.GetValueOrDefault(token, fallback);
}