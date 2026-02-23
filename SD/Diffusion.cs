using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OLLM.SD;

internal static class Diffusion {
	internal static WriteableBitmap Diffuse(DiffusionOptions dOpt) {
		string modelRoot = $"{dOpt.ModelRoot}";
		string prompt = $"{dOpt.Prompt}";
		string negativePrompt = ""; // Optional: pull from dOpt if you add it
		int height = dOpt.Height;
		int width = dOpt.Width;
		int seed = dOpt.Seed;
		int steps = dOpt.Steps;
		float guidance = dOpt.Guidance;

		using SessionOptions so = new();
		so.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
		so.EnableCpuMemArena = false;

		// DML execution for Windows. Change to AppendExecutionProvider_CUDA() for Nvidia.
		so.AppendExecutionProvider_DML();

		using InferenceSession textEncoder = new(Path.Combine(modelRoot, "text_encoder", "model.onnx"), so);
		using InferenceSession unet = new(Path.Combine(modelRoot, "unet", "model.onnx"), so);
		using InferenceSession vaeDecoder = new(Path.Combine(modelRoot, "vae_decoder", "model.onnx"), so);

		string tokenizerPath = Path.Combine(modelRoot, "tokenizer");

		// Encode chunked text embeddings to bypass the 77-token limit
		DenseTensor<float> textEmbeddings = EncodeTextChunked(textEncoder, prompt, negativePrompt, tokenizerPath);

		// Prepare scheduler & initial latents
		LmsScheduler scheduler = new(numTrainTimesteps: 1000, betaStart: 0.00085f, betaEnd: 0.012f);
		scheduler.SetTimesteps(steps);

		Random rng = new(seed);
		DenseTensor<float> latents = CreateRandomLatents(rng, height, width);
		latents = Scale(latents, scheduler.Sigmas[0]);

		// Denoise (UNet Loop)
		for (int i = 0; i < scheduler.Timesteps.Length; i++) {
			int t = scheduler.Timesteps[i];
			float sigma = scheduler.Sigmas[i];
			DenseTensor<float> latentInput = RepeatLatents(latents, 2);
			latentInput = scheduler.ScaleModelInput(latentInput, sigma);

			DenseTensor<float> noisePredicate = RunUnet(unet, latentInput, t, textEmbeddings);
			DenseTensor<float> noiseUnconditional = SliceBatch(noisePredicate, 0);
			DenseTensor<float> noiseText = SliceBatch(noisePredicate, 1);

			// Classifier-Free Guidance
			DenseTensor<float> guided = Add(noiseUnconditional, Scale(Sub(noiseText, noiseUnconditional), guidance));

			latents = scheduler.Step(guided, i, latents);
			Console.WriteLine($"Step {i + 1}/{steps} (t={t}, sigma={sigma:0.0000})");
		}

		// Decode latents directly to a native WPF WriteableBitmap
		WriteableBitmap image = DecodeToImage(vaeDecoder, latents, height, width);

		return image;
	}

	/// <summary>
	/// Chunks the prompt, runs the text encoder on each chunk, and concatenates the embeddings.
	/// </summary>
	private static DenseTensor<float> EncodeTextChunked(InferenceSession textEncoder, string prompt, string negativePrompt, string tokenizerPath) {
		// 1. Get raw BPE IDs (without BOS/EOS)
		List<int> condRaw = GetRawBpeIds(prompt, tokenizerPath);
		List<int> uncondRaw = GetRawBpeIds(negativePrompt, tokenizerPath);

		// 2. Determine number of chunks needed (max of both, at least 1)
		int chunks = Math.Max(1, Math.Max(
			(int)Math.Ceiling(condRaw.Count / 75.0),
			(int)Math.Ceiling(uncondRaw.Count / 75.0)
		));

		// 3. Create tensors for [2, chunks * 77]
		DenseTensor<long> inputIds = new([2, chunks * MaxLength]);

		// 4. Fill the tensors with appropriate BOS/EOS padding per chunk
		FillChunkedIds(inputIds, 0, uncondRaw, chunks); // Batch 0 = Unconditional
		FillChunkedIds(inputIds, 1, condRaw, chunks);   // Batch 1 = Conditional

		List<DenseTensor<float>> chunkEmbeddings = new();

		// 5. Run Text Encoder on each 77-token chunk individually
		for (int c = 0; c < chunks; c++) {
			DenseTensor<long> chunkInput = new([2, MaxLength]);
			for (int b = 0; b < 2; b++) {
				for (int i = 0; i < MaxLength; i++) {
					chunkInput[b, i] = inputIds[b, c * MaxLength + i];
				}
			}

			List<NamedOnnxValue> inputs = [NamedOnnxValue.CreateFromTensor("input_ids", chunkInput)];
			using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = textEncoder.Run(inputs);

			// Deep copy output to prevent memory access violations
			Tensor<float> output = results.First(x => x.Name == "last_hidden_state").AsTensor<float>();
			chunkEmbeddings.Add(new DenseTensor<float>(output.ToArray(), output.Dimensions.ToArray()));
		}

		// 6. Concatenate chunk embeddings along the sequence axis
		int dim = chunkEmbeddings[0].Dimensions[2]; // Usually 768 or 1024
		DenseTensor<float> finalEmbeddings = new([2, chunks * MaxLength, dim]);

		for (int c = 0; c < chunks; c++) {
			var chunk = chunkEmbeddings[c];
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
		if (string.IsNullOrWhiteSpace(text))
			return new List<int>();

		using Stream vocabStream = File.OpenRead(Path.Combine(tokenizerPath, "vocab.json"));
		using Stream mergesStream = File.OpenRead(Path.Combine(tokenizerPath, "merges.txt"));

		Tokenizer bpeTokenizer = BpeTokenizer.Create(vocabStream, mergesStream);
		return bpeTokenizer.EncodeToIds(text.ToLowerInvariant()).ToList();
	}

	private static void FillChunkedIds(DenseTensor<long> tensor, int batchIndex, List<int> rawIds, int totalChunks) {
		for (int c = 0; c < totalChunks; c++) {
			int startOffset = c * MaxLength;
			int rawStart = c * 75;

			tensor[batchIndex, startOffset] = 49406; // BOS Token

			for (int i = 0; i < 75; i++) {
				int rawIndex = rawStart + i;
				if (rawIndex < rawIds.Count) {
					tensor[batchIndex, startOffset + 1 + i] = rawIds[rawIndex];
				} else {
					tensor[batchIndex, startOffset + 1 + i] = 49407; // EOS / Pad Token
				}
			}

			tensor[batchIndex, startOffset + 76] = 49407; // EOS Token
		}
	}

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

		// Note: Depending on your exact ONNX export, this could be "out_sample" or simply "sample"
		Tensor<float> output = results.First(x => x.Name == "out_sample").AsTensor<float>();
		return new DenseTensor<float>(output.ToArray(), output.Dimensions.ToArray());
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

		// WPF native bitmap format (96 DPI is standard monitor resolution)
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

					// WriteableBitmap with Bgr24 requires Blue, Green, Red memory ordering
					row[x * 3 + 0] = ToByte(b);
					row[x * 3 + 1] = ToByte(g);
					row[x * 3 + 2] = ToByte(r);
				}
			}
		}

		bmp.AddDirtyRect(new Int32Rect(0, 0, width, height));
		bmp.Unlock();

		// CRITICAL: Freeze the bitmap so it can safely cross from Task.Run() to the Main UI thread
		bmp.Freeze();

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

	private const int MaxLength = 77;
	private const int LatentChannels = 4;
	private const int DownsampleFactor = 8;
}