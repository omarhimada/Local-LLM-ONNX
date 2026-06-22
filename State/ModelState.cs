using Microsoft.ML.OnnxRuntimeGenAI;
using System.IO;
using System.Windows;
using static OLLM.Constants;
namespace OLLM.State;

internal class ModelState {
	#region Fields, properties, expressions
	internal Model? Model;
	internal GeneratorParams? GeneratorParams;
	internal Tokenizer? Tokenizer;
	internal Generator? Generator;
	internal string? ModelDirectory { get; set; }
	internal bool ExpectingCodeResponse = false;
	internal float _getTemperature() => ExpectingCodeResponse ? 0.225f : 0.7f;
	#endregion
	internal ModelState(string modelDirectory) {
		//config.AppendProvider(_dml);
		#region Point to the direct ONNX model itself to instantiate the inference session
		var list = Directory.GetFiles(modelDirectory, _onnxSearch);
		string? modelFilePath = list.FirstOrDefault();
		if (string.IsNullOrEmpty(modelFilePath)) {
			System.Windows.MessageBox.Show(_userFriendlyErrorOccurredDuringInitialization);
			System.Windows.Application.Current.Shutdown();
		}

		var pathParts = modelFilePath.Split("\\");
		var path = string.Join("\\", pathParts[0..(pathParts.Length - 1)]);

		modelDirectory = path;
		#endregion
		ModelDirectory = modelDirectory;

		Config config = new(ModelDirectory);
		config.AppendProvider(_cuda);
		Model = new(config);
		Tokenizer = new(Model);
		GeneratorParams = new(Model);
	}
	/// <summary>
	/// Re-initialize the generator after each response as opposed to before your next input is tokenized.
	/// (i.e.: user reads initial output of the model and then by the time they comprehend, the generator is re-initialized)
	/// </summary>
	internal void RefreshGenerator() {
		Generator?.Dispose();
		Generator = new(Model, GeneratorParams);
	}
	internal void SetGeneratorParameterSearchOptions() {
		#region Set generator parameters
		GeneratorParams?.SetSearchOption(_maxLengthParameter, 131072);
		GeneratorParams?.SetSearchOption(_doSample, true);
		GeneratorParams?.SetSearchOption(_temperature, _getTemperature());
		GeneratorParams?.SetSearchOption(_topK, 51);
		GeneratorParams?.SetSearchOption(_topP, 0.9f);
		GeneratorParams?.SetSearchOption(_repetitionPenalty, 1.12f);
		#endregion
	}
}