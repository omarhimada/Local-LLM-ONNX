namespace OLLM.SD;

using Microsoft.ML.OnnxRuntime;
using static Constants;
public sealed class DiffusionOptions {
	public string ModelRoot { get; init; } = _sdModelPath;
	public string Prompt { get; init; } = _sdPrompt;
	public string Negative { get; init; } = _sdNegative;
	public int Steps { get; init; } = 60;
	public Float16 Guidance { get; init; } = (Float16)7.5f;
	public int Height { get; init; } = 1920;
	public int Width { get; init; } = 1080;
	public int Seed { get; init; } = -1;
}

