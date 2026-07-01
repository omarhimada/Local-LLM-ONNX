using Microsoft.ML.OnnxRuntime;
using System.Windows;

namespace OLLM.Utility {
	internal static class CheckForCUDA{
		internal static string GetAvailableProviders() {
			try {
				string[] providers = OrtEnv.Instance().GetAvailableProviders();
				Console.WriteLine($"{string.Join(",", providers)}");
				foreach (string provider in providers) {
					Console.WriteLine(provider);
					Console.WriteLine();
				}

				using SessionOptions sessionOptions = new();
				OrtCUDAProviderOptions cudaOptions = new();
				sessionOptions.AppendExecutionProvider_CUDA(0);

				return "CUDA is available and initialized via ONNX Runtime.";
			} catch (Exception exception) {
				MessageBox.Show($"{exception.Message}");
				return exception.Message;
			}
		}
	}
}