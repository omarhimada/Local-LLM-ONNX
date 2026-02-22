using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace OLLM.SD;

using static Constants;

internal static class Diffusion {
	private static int MaxLength => _sdPrompt.Length; // almost
	private const int LatentChannels = 4;
	private const int DownsampleFactor = 8;

	internal static void Diffuse(DiffusionOptions dOpt) {
		string modelRoot = $"{dOpt.ModelRoot}";
		string prompt = $"{dOpt.Prompt}";
		int height = dOpt.Height;
		int width = dOpt.Width;
		int seed = dOpt.Seed;
		int steps = dOpt.Steps;
		float guidance = dOpt.Guidance;

		using SessionOptions so = new();
		so.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
		so.EnableCpuMemArena = false;
		// TODO change to CUDA
		so.AppendExecutionProvider_DML();

		using InferenceSession textEncoder = new(Path.Combine(modelRoot, "text_encoder", "model.onnx"), so);
		using InferenceSession unet = new(Path.Combine(modelRoot, "unet", "model.onnx"), so);
		using InferenceSession vaeDecoder = new(Path.Combine(modelRoot, "vae_decoder", "model.onnx"), so);

		// Tokenize prompt & unconditional prompt
		long[] inputIds = TokenizeClipLike(prompt);
		long[] unconditionalInputIds = TokenizeClipLike(_sdNegative);

		// Text encoder -> embeddings (unconditional + conditional)
		DenseTensor<float> textEmbeddings = EncodeText2Batch(textEncoder, unconditionalInputIds, inputIds);

		// Prepare scheduler & initial latents
		LmsScheduler scheduler = new(numTrainTimesteps: 1000, betaStart: 0.00085f, betaEnd: 0.012f);
		scheduler.SetTimesteps(steps);

		Random rng = new(seed);
		DenseTensor<float> latents = CreateRandomLatents(rng, height, width);
		latents = Scale(latents, scheduler.Sigmas[0]);

		// Denoise
		for (int i = 0; i < scheduler.Timesteps.Length; i++) {
			int t = scheduler.Timesteps[i];
			float sigma = scheduler.Sigmas[i];
			DenseTensor<float> latentInput = RepeatLatents(latents, 2);
			latentInput = scheduler.ScaleModelInput(latentInput, sigma);

			DenseTensor<float> noisePredicate = RunUnet(unet, latentInput, t, textEmbeddings);
			DenseTensor<float> noiseUnconditional = SliceBatch(noisePredicate, 0);
			DenseTensor<float> noiseText = SliceBatch(noisePredicate, 1);
			DenseTensor<float> guided = Add(noiseUnconditional, Scale(Sub(noiseText, noiseUnconditional), guidance));

			latents = scheduler.Step(guided, i, latents);
			Console.WriteLine($"Step {i + 1}/{steps} (t={t}, sigma={sigma:0.0000})");
		}

		// Decode latents to image
		Bitmap image = DecodeToImage(vaeDecoder, latents, height, width);

		string outPath = Path.Combine(Environment.CurrentDirectory, $"sd_out_{DateTime.UtcNow.Ticks}.png");
		image.Save(outPath, ImageFormat.Png);
		Console.WriteLine($"Wrote: {outPath}");
	}

	private static long[] TokenizeClipLike(string text) {
		long[] ids = new long[MaxLength];
		ids[0] = 49406;
		ids[1] = 0;
		ids[2] = 49407;
		for (int i = 3; i < MaxLength; i++) {
			ids[i] = 49407;
		}
		return ids;
	}

	// Text encoder -> [2, *, *] embeddings
	private static DenseTensor<float> EncodeText2Batch(InferenceSession textEncoder, long[] unconditionalIds, long[] condIds) {
		DenseTensor<long> input = new([2, MaxLength]);
		for (int i = 0; i < MaxLength; i++) {
			input[0, i] = unconditionalIds[i];
			input[1, i] = condIds[i];
		}

		List<NamedOnnxValue> inputs = [NamedOnnxValue.CreateFromTensor("input_ids", input)];

		using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = textEncoder.Run(inputs);
		// Output name varies by export; often "last_hidden_state"
		Tensor<float> output = results.First().AsTensor<float>();

		// Ensure DenseTensor<float>
		return output is DenseTensor<float> dt
			? dt
			: new DenseTensor<float>(output.ToArray(), output.Dimensions.ToArray());
	}

	// Latents / noise / scheduler
	// TODO ladder
	private static DenseTensor<float> CreateRandomLatents(Random rng, int height, int width) {
		int h = height / DownsampleFactor;
		int w = width / DownsampleFactor;

		DenseTensor<float> t = new(new[] { 1, LatentChannels, h, w });
		for (int c = 0; c < LatentChannels; c++)
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					t[0, c, y, x] = NextGaussian(rng);

		return t;
	}

	private static float NextGaussian(Random rng) {
		double u1 = 1.0 - rng.NextDouble();
		double u2 = 1.0 - rng.NextDouble();
		return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
	}

	private static DenseTensor<float> RunUnet(InferenceSession unet, DenseTensor<float> sample, int timestep, DenseTensor<float> textEmbeddings) {
		DenseTensor<long> t = new([1]) {
			[0] = timestep
		};

		List<NamedOnnxValue> inputs = [
			NamedOnnxValue.CreateFromTensor("sample", sample),
			NamedOnnxValue.CreateFromTensor("timestep", t),
			NamedOnnxValue.CreateFromTensor("encoder_hidden_states", textEmbeddings)
		];

		using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = unet.Run(inputs);
		Tensor<float> output = results.First().AsTensor<float>();

		return output is DenseTensor<float> dt
			? dt
			: new DenseTensor<float>(output.ToArray(), output.Dimensions.ToArray());
	}

	private static Bitmap DecodeToImage(InferenceSession vaeDecoder, DenseTensor<float> latents, int height, int width) {
		DenseTensor<float> scaled = Scale(latents, 1f / 0.18215f);

		List<NamedOnnxValue> inputs = [
			NamedOnnxValue.CreateFromTensor("latent_sample", scaled)
		];

		using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = vaeDecoder.Run(inputs);
		Tensor<float> decoded = results.First().AsTensor<float>();
		float[] img = decoded.ToArray();

		Bitmap bmp = new(width, height, PixelFormat.Format24bppRgb);
		BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmp.PixelFormat);

		unsafe {
			byte* dst = (byte*)bd.Scan0;
			int stride = bd.Stride;

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

					row[x * 3 + 2] = ToByte(r);
					row[x * 3 + 1] = ToByte(g);
					row[x * 3 + 0] = ToByte(b);
				}
			}
		}

		bmp.UnlockBits(bd);
		return bmp;

		static byte ToByte(float v) {
			v = Math.Clamp(v, 0f, 1f);
			return (byte)(v * 255f + 0.5f);
		}
	}

	private static DenseTensor<float> RepeatLatents(DenseTensor<float> latents, int repeat) {
		int[] d = latents.Dimensions.ToArray();
		DenseTensor<float> outT = new(new[] { repeat, d[1], d[2], d[3] });
		for (int r = 0; r < repeat; r++)
			for (int c = 0; c < d[1]; c++)
				for (int y = 0; y < d[2]; y++)
					for (int x = 0; x < d[3]; x++)
						outT[r, c, y, x] = latents[0, c, y, x];
		return outT;
	}

	private static DenseTensor<float> SliceBatch(DenseTensor<float> t, int batchIndex) {
		int[] d = t.Dimensions.ToArray();
		DenseTensor<float> outT = new(new[] { 1, d[1], d[2], d[3] });
		for (int c = 0; c < d[1]; c++)
			for (int y = 0; y < d[2]; y++)
				for (int x = 0; x < d[3]; x++)
					outT[0, c, y, x] = t[batchIndex, c, y, x];
		return outT;
	}

	private static DenseTensor<float> Add(DenseTensor<float> a, DenseTensor<float> b)
		=> ElementWise(a, b, (x, y) => x + y);

	private static DenseTensor<float> Sub(DenseTensor<float> a, DenseTensor<float> b)
		=> ElementWise(a, b, (x, y) => x - y);

	private static DenseTensor<float> Scale(DenseTensor<float> a, float s) {
		int[] d = a.Dimensions.ToArray();
		DenseTensor<float> outT = new(d);
		for (int i = 0; i < a.Length; i++)
			outT.Buffer.Span[i] = a.Buffer.Span[i] * s;
		return outT;
	}

	private static DenseTensor<float> ElementWise(DenseTensor<float> a, DenseTensor<float> b, Func<float, float, float> f) {
		int[] d = a.Dimensions.ToArray();
		DenseTensor<float> outT = new(d);
		for (int i = 0; i < a.Length; i++)
			outT.Buffer.Span[i] = f(a.Buffer.Span[i], b.Buffer.Span[i]);
		return outT;
	}
}