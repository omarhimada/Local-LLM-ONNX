using System;
using System.Collections.Generic;
using System.Text;

namespace OLLM.SD;

using static Constants;
public sealed class DiffusionOptions {
	public string ModelRoot { get; init; } = _sdModelPath;
	public string Prompt { get; init; } = _sdPrompt;
	public int Steps { get; init; } = 60;
	public float Guidance { get; init; } = 7.5f;
	public int Height { get; init; } = 1920;
	public int Width { get; init; } = 1080;
	public int Seed { get; init; } = -1;

	//public static DiffusionOptions FromArgs(string[] args) {
	//	static int I(string s) => int.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
	//	static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

	//	return new DiffusionOptions {
	//		ModelRoot = args.Length > 0 ? args[0] : _sdModelPath,
	//		Prompt = args.Length > 1 ? args[1] : _sdPrompt,
	//		Steps = args.Length > 2 ? I(args[2]) : 20,
	//		Guidance = args.Length > 3 ? F(args[3]) : 7.5f,
	//		Height = args.Length > 4 ? I(args[4]) : 512,
	//		Width = args.Length > 5 ? I(args[5]) : 512,
	//		Seed = args.Length > 6 ? I(args[6]) : 12345,
	//	};
	//}
}

