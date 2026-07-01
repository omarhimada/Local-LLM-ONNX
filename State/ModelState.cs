using Microsoft.ML.OnnxRuntimeGenAI;
using System.Data.OleDb;
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
		ModelDirectory = modelDirectory;
		Config config = new(ModelDirectory);
		config.AppendProvider(_cuda);
		//config.AppendProvider(_dml);
		#region Point to the direct ONNX model itself to instantiate the inference session
		string? modelFilePath = Directory.GetFiles(modelDirectory, _onnxSearch).FirstOrDefault();
		if (string.IsNullOrEmpty(modelFilePath)) {
			MessageBox.Show(_userFriendlyErrorOccurredDuringInitialization);
			Application.Current.Shutdown();
		}
		#endregion
		try {
			Model = new(config);
		}
		catch (Exception ex) {
			MessageBox.Show(ex.Message);

		}
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