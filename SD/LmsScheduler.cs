using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace OLLM.SD {
	internal sealed class LmsScheduler {
		private readonly float[] _alphasCumulativeProd;
		public int[] Timesteps { get; private set; } = [];
		public float[] Sigmas { get; private set; } = [];

		private readonly List<DenseTensor<float>> _derivatives = [];

		public LmsScheduler(int numTrainTimesteps, Float16 betaStart, Float16 betaEnd) {
			// Linear betas as in common SD schedulers
			float[] betas = new float[numTrainTimesteps];
			for (int i = 0; i < numTrainTimesteps; i++)
				betas[i] = betaStart.ToFloat() + (betaEnd.ToFloat() - betaStart.ToFloat()) * i / (numTrainTimesteps - 1);

			float[] alphas = betas.Select(b => 1f - b).ToArray();
			_alphasCumulativeProd = new float[numTrainTimesteps];
			float prod = 1f;
			for (int i = 0; i < numTrainTimesteps; i++) {
				prod *= alphas[i];
				_alphasCumulativeProd[i] = prod;
			}
		}

		public void SetTimesteps(int numInferenceSteps) {
			// Common: evenly spaced timesteps descending
			Timesteps =
				Enumerable.Range(0, numInferenceSteps)
					.Select(i =>
						(int)Math.Round(
							(1 - (double)i / (numInferenceSteps - 1)) *
							(_alphasCumulativeProd.Length - 1)))
					.ToArray();

			// Convert to sigmas
			Sigmas = Timesteps.Select(t => {
				float acp = _alphasCumulativeProd[t];
				return (float)Math.Sqrt((1 - acp) / acp);
			}).ToArray();

			_derivatives.Clear();
		}

		public DenseTensor<float> ScaleModelInput(DenseTensor<float> latents, float sigma) {
			// latents / sqrt(sigma^2 + 1)
			float s = 1f / (float)Math.Sqrt(sigma * sigma + 1f);
			return Scale(latents, s);

			static DenseTensor<float> Scale(DenseTensor<float> a, float k) {
				int[] d = a.Dimensions.ToArray();
				DenseTensor<float> outT = new(d);
				for (int i = 0; i < a.Length; i++)
					outT.Buffer.Span[i] = a.Buffer.Span[i] * k;
				return outT;
			}
		}

		public DenseTensor<float> Step(DenseTensor<float> modelOutput, int stepIndex, DenseTensor<float> latents) {
			float sigma = Sigmas[stepIndex];
			float sigmaNext = (stepIndex == Sigmas.Length - 1) ? 0f : Sigmas[stepIndex + 1];

			// Convert model output to derivative: (x - eps*sigma)/sigma
			DenseTensor<float> derivative = new(latents.Dimensions.ToArray());
			for (int i = 0; i < latents.Length; i++)
				derivative.Buffer.Span[i] = (latents.Buffer.Span[i] - modelOutput.Buffer.Span[i]) / sigma;

			_derivatives.Add(derivative);
			if (_derivatives.Count > 4)
				_derivatives.RemoveAt(0); // small order

			// Simple 1st-order update (kept compact). Improve by LMS multistep coeffs if you want.
			float dt = sigmaNext - sigma;
			DenseTensor<float> prev = new(latents.Dimensions.ToArray());
			for (int i = 0; i < latents.Length; i++)
				prev.Buffer.Span[i] = latents.Buffer.Span[i] + derivative.Buffer.Span[i] * dt;

			return prev;
		}
	}
}
