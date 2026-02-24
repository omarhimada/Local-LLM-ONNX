using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntimeGenAI;
using Microsoft.ML.Tokenizers;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OLLM.SD;

using static Constants;

internal static class Diffusion {
#pragma warning disable IDE0051
	private static readonly OgaHandle _sdOgaHandle = new();
#pragma warning restore IDE0051

	internal static DenseTensor<float> ConvertFloat16ToFloat(DenseTensor<Float16> float16Tensor) {
		Float16[] float16Array = float16Tensor.ToArray();
		float[] floatArray = float16Array.Select(x => x.ToFloat()).ToArray();
		return new DenseTensor<float>(floatArray, float16Tensor.Dimensions);
	}

	internal static WriteableBitmap Diffuse(DiffusionOptions dOpt) {
		string modelRoot = $"{dOpt.ModelRoot}";
		string prompt = $"{dOpt.Prompt}";
		string negativePrompt = $"{dOpt.Negative}";

		int height = dOpt.Height;
		int width = dOpt.Width;
		int seed = dOpt.Seed;
		int steps = dOpt.Steps;
		Float16 guidance = dOpt.Guidance;

		using SessionOptions so = new();

		so.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
		so.EnableCpuMemArena = false;
		Config config = new Config(modelRoot);
		config.AppendProvider(_dml);

		using InferenceSession textEncoder = new(Path.Combine(modelRoot, "text_encoder", "model.onnx"), so);
		using InferenceSession unet = new(Path.Combine(modelRoot, "unet", "model.onnx"), so);
		using InferenceSession vaeDecoder = new(Path.Combine(modelRoot, "vae_decoder", "model.onnx"), so);

		string tokenizerPath = Path.Combine(modelRoot, "tokenizer");

		DenseTensor<Float16> textEmbeddings = EncodeTextChunked(textEncoder, prompt, negativePrompt, tokenizerPath);

		LmsScheduler scheduler =
			new(
				numTrainTimesteps: 1000,
				betaStart: (Float16)0.00085f,
				betaEnd: (Float16)0.012f);
		scheduler.SetTimesteps(steps);

		Random rng = new(seed);
		DenseTensor<Float16> latents = CreateRandomLatents(rng, height, width);
		DenseTensor<float> latentsFloat = ConvertFloat16ToFloat(latents);
		latentsFloat = Scale(latentsFloat, scheduler.Sigmas[0]);

		for (int i = 0; i < scheduler.Timesteps.Length; i++) {
			int timestep = scheduler.Timesteps[i];
			float sigma = scheduler.Sigmas[i];
			DenseTensor<Float16> latentInput = RepeatLatents(latents, 2);
			DenseTensor<float> latentInputFloat = ConvertFloat16ToFloat(latentInput);
			latentInputFloat = scheduler.ScaleModelInput(latentInputFloat, sigma);

			DenseTensor<Float16> noisePredicate = RunUnet(unet, latentInput, timestep, textEmbeddings);
			DenseTensor<Float16> noiseUnconditional = SliceBatch(noisePredicate, 0);
			DenseTensor<Float16> noiseText = SliceBatch(noisePredicate, 1);

			DenseTensor<float> noiseUnconditionalFloat = ConvertFloat16ToFloat(noiseUnconditional);

			DenseTensor<float> guided =
				Add(noiseUnconditionalFloat,
					Scale(
						Sub(
							ConvertFloat16ToFloat(noiseText),
							noiseUnconditionalFloat
						), guidance.ToFloat()));

			latentsFloat = scheduler.Step(guided, i, latentsFloat);
			Console.WriteLine($"Step {i + 1}/{steps} (t={timestep}, sigma={sigma:0.0000})");
		}

		WriteableBitmap image = DecodeToImage(vaeDecoder, latentsFloat, height, width);

		return image;
	}

	private static DenseTensor<Float16> EncodeTextChunked(
		InferenceSession textEncoder, string prompt, string negativePrompt, string tokenizerPath) {

		List<int> condRaw = GetRawBpeIds(prompt, tokenizerPath);
		List<int> uncondRaw = GetRawBpeIds(negativePrompt, tokenizerPath);

		int chunks = Math.Max(1, Math.Max(
			(int)Math.Ceiling(condRaw.Count / 75.0),
			(int)Math.Ceiling(uncondRaw.Count / 75.0)
		));

		DenseTensor<long> inputIds = new([2, chunks * MaxLength]);

		FillChunkedIds(inputIds, 0, uncondRaw, chunks);
		FillChunkedIds(inputIds, 1, condRaw, chunks);
		List<DenseTensor<Float16>> chunkEmbeddingsFloat16 = [];

		for (int c = 0; c < chunks; c++) {
			DenseTensor<long> chunkInput = new([2, MaxLength]);
			for (int b = 0; b < 2; b++) {
				for (int i = 0; i < MaxLength; i++) {
					chunkInput[b, i] = inputIds[b, c * MaxLength + i];
				}
			}

			List<NamedOnnxValue> inputs = [NamedOnnxValue.CreateFromTensor("input_ids", chunkInput)];
			using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = textEncoder.Run(inputs);

			DisposableNamedOnnxValue output = results.First(x => x.Name == "last_hidden_state");
			Tensor<Float16> output4 = results.First(x => x.Name == "last_hidden_state").AsTensor<Float16>();

			chunkEmbeddingsFloat16.Add(new DenseTensor<Float16>(output4.ToArray(), output4.Dimensions.ToArray()));
		}

		int dim = chunkEmbeddingsFloat16[0].Dimensions[2];
		DenseTensor<Float16> finalEmbeddings = new([2, chunks * MaxLength, dim]);

		for (int c = 0; c < chunks; c++) {
			var chunk = chunkEmbeddingsFloat16[c];
			for (int b = 0; b < 2; b++) {
				for (int s = 0; s < MaxLength; s++) {
					for (int d = 0; d < dim; d++) {
						finalEmbeddings[b, c * MaxLength + s, d] = chunk[b, s, d];
					}
				}
			}
		}

		return finalEmbeddings;
	}

	private static List<int> GetRawBpeIds(string text, string tokenizerPath) {
		if (string.IsNullOrWhiteSpace(text)) {
			return [];
		}

		using Stream vocabStream = File.OpenRead(Path.Combine(tokenizerPath, "vocab.json"));
		using Stream mergesStream = File.OpenRead(Path.Combine(tokenizerPath, "merges.txt"));

		Microsoft.ML.Tokenizers.Tokenizer bpeTokenizer = BpeTokenizer.Create(vocabStream, mergesStream);
		return bpeTokenizer.EncodeToIds(text.ToLowerInvariant()).ToList();
	}

	private static void FillChunkedIds(DenseTensor<long> tensor, int batchIndex, List<int> rawIds, int totalChunks) {
		for (int c = 0; c < totalChunks; c++) {
			int startOffset = c * MaxLength;
			int rawStart = c * 75;

			tensor[batchIndex, startOffset] = 49406;
			for (int i = 0; i < 75; i++) {
				int rawIndex = rawStart + i;
				if (rawIndex < rawIds.Count) {
					tensor[batchIndex, startOffset + 1 + i] = rawIds[rawIndex];
				} else {
					tensor[batchIndex, startOffset + 1 + i] = 49407;
				}
			}
			tensor[batchIndex, startOffset + 76] = 49407;
		}
	}
	private static DenseTensor<Float16> CreateRandomLatents(Random rng, int height, int width) {
		int h = height / DownsampleFactor;
		int w = width / DownsampleFactor;

		DenseTensor<Float16> t = new([1, LatentChannels, h, w]);
		for (int c = 0; c < LatentChannels; c++) {
			for (int y = 0; y < h; y++) {
				for (int x = 0; x < w; x++) {
					t[0, c, y, x] = NextGaussian(rng);
				}
			}
		}

		return t;
	}

	private static Float16 NextGaussian(Random rng) {
		double u1 = 1.0 - rng.NextDouble();
		double u2 = 1.0 - rng.NextDouble();
		return (Float16)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
	}

	private static DenseTensor<Float16> RunUnet(
		InferenceSession unet,
		DenseTensor<Float16> sample,
		int timestep,
		DenseTensor<Float16> textEmbeddings) {
		DenseTensor<int> t = new([1]) {
			[0] = timestep
		};

		List<NamedOnnxValue> inputs = [
			NamedOnnxValue.CreateFromTensor("sample", sample),
								NamedOnnxValue.CreateFromTensor("encoder_hidden_states", textEmbeddings)
		];

		using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = unet.Run(inputs);

		Tensor<Float16> output = results.First(x => x.Name == "out_sample").AsTensor<Float16>();
		return new DenseTensor<Float16>(output.ToArray(), output.Dimensions.ToArray());
	}

	private static WriteableBitmap DecodeToImage(InferenceSession vaeDecoder, DenseTensor<float> latents, int height, int width) {
		DenseTensor<float> scaled = Scale(latents, 1f / 0.18215f);

		List<NamedOnnxValue> inputs = [
			NamedOnnxValue.CreateFromTensor("latent_sample", scaled)
		];

		Tensor<float> decoded;
		using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = vaeDecoder.Run(inputs)) {
			var outputTensor = results.First(x => x.Name == "sample").AsTensor<float>();
			decoded = new DenseTensor<float>(outputTensor.ToArray(), outputTensor.Dimensions.ToArray());
		}

		float[] img = decoded.ToArray();

		WriteableBitmap bmp = new(width, height, 96, 96, PixelFormats.Bgr24, null);
		bmp.Lock();

		unsafe {
			byte* dst = (byte*)bmp.BackBuffer;
			int stride = bmp.BackBufferStride;

			int hw = height * width;
			for (int y = 0; y < height; y++) {
				byte* row = dst + y * stride;
				for (int x = 0; x < width; x++) {
					int idx = y * width + x;

					float r = img[0 * hw + idx];
					float g = img[1 * hw + idx];
					float b = img[2 * hw + idx];

					r = r / 2f + 0.5f;
					g = g / 2f + 0.5f;
					b = b / 2f + 0.5f;

					row[x * 3 + 0] = ToByte(b);
					row[x * 3 + 1] = ToByte(g);
					row[x * 3 + 2] = ToByte(r);
				}
			}
		}

		bmp.AddDirtyRect(new Int32Rect(0, 0, width, height));
		bmp.Unlock();

		bmp.Freeze();

		return bmp;

		static byte ToByte(float v) {
			v = Math.Clamp(v, 0f, 1f);
			return (byte)(v * 255f + 0.5f);
		}
	}

	private static DenseTensor<Float16> RepeatLatents(DenseTensor<Float16> latents, int repeat) {
		int[] d = latents.Dimensions.ToArray();
		DenseTensor<Float16> outT = new([repeat, d[1], d[2], d[3]]);
		for (int r = 0; r < repeat; r++) {
			for (int c = 0; c < d[1]; c++) {
				for (int y = 0; y < d[2]; y++) {
					for (int x = 0; x < d[3]; x++) {
						outT[r, c, y, x] = latents[0, c, y, x];
					}
				}
			}
		}

		return outT;
	}

	private static DenseTensor<Float16> SliceBatch(DenseTensor<Float16> t, int batchIndex) {
		int[] d = t.Dimensions.ToArray();
		DenseTensor<Float16> outT = new([1, d[1], d[2], d[3]]);
		for (int c = 0; c < d[1]; c++) {
			for (int y = 0; y < d[2]; y++) {
				for (int x = 0; x < d[3]; x++) {
					outT[0, c, y, x] = t[batchIndex, c, y, x];
				}
			}
		}

		return outT;
	}

	private static DenseTensor<float> Add(DenseTensor<float> a, DenseTensor<float> b)
		=> ElementWise(a, b, (x, y) => x + y);

	private static DenseTensor<float> Sub(DenseTensor<float> a, DenseTensor<float> b)
		=> ElementWise(a, b, (x, y) => (x - y));

	private static DenseTensor<float> Scale(DenseTensor<float> a, float s) {
		int[] d = a.Dimensions.ToArray();
		DenseTensor<float> outT = new(d);
		for (int i = 0; i < a.Length; i++) {
			outT.Buffer.Span[i] = a.Buffer.Span[i] * s;
		}

		return outT;
	}

	private static DenseTensor<float> ElementWise(DenseTensor<float> a, DenseTensor<float> b, Func<float, float, float> f) {
		int[] d = a.Dimensions.ToArray();
		DenseTensor<float> outT = new(d);
		for (int i = 0; i < a.Length; i++) {
			outT.Buffer.Span[i] = f(a.Buffer.Span[i], b.Buffer.Span[i]);
		}

		return outT;
	}

	private const int MaxLength = 77;
	private const int LatentChannels = 4;
	private const int DownsampleFactor = 8;
}