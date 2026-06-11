using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Vision;
using Microsoft.SemanticKernel;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;

var ml = new MLContext(seed: 1);

var data = ml.Data.LoadFromEnumerable(
	Directory.GetFiles("data", "*.*", SearchOption.AllDirectories)
		.Where(f => f.EndsWith(".png") || f.EndsWith(".jpg"))
		.Select(f => new ImageData {
			ImagePath = f,
			Label = Directory.GetParent(f)!.Name
		}));

var split = ml.Data.TrainTestSplit(data, testFraction: 0.2);

var pipeline =
	ml.Transforms.Conversion.MapValueToKey("LabelKey", "Label")
	.Append(ml.Transforms.LoadRawImageBytes(
		outputColumnName: "Image",
		imageFolder: "",
		inputColumnName: "ImagePath"))
	.Append(ml.MulticlassClassification.Trainers.ImageClassification(
		new ImageClassificationTrainer.Options {
			FeatureColumnName = "Image",
			LabelColumnName = "LabelKey",
			Arch = ImageClassificationTrainer.Architecture.ResnetV2101,
			Epoch = 50,
			BatchSize = 10,
			LearningRate = 0.01f,
			ValidationSet = split.TestSet
		}))
	.Append(ml.Transforms.Conversion.MapKeyToValue(
		outputColumnName: "PredictedLabel",
		inputColumnName: "PredictedLabel"));

var model = pipeline.Fit(split.TrainSet);

var predictions = model.Transform(split.TestSet);
var metrics = ml.MulticlassClassification.Evaluate(
	predictions,
	labelColumnName: "LabelKey",
	predictedLabelColumnName: "PredictedLabel");

Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy:P2}");
Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy:P2}");

ml.Model.Save(model, split.TrainSet.Schema, "pattern-model.zip");

var engine = ml.Model.CreatePredictionEngine<ImageData, ImagePrediction>(model);

var result = engine.Predict(new ImageData {
	ImagePath = "test.png"
});

Console.WriteLine($"Prediction: {result.PredictedLabel}");
Console.WriteLine($"Score: {result.Score.Max():P2}");

public sealed class ImageData {
	public string ImagePath { get; set; } = "";
	public string Label { get; set; } = "";
}

public sealed class ImagePrediction {
	public string PredictedLabel { get; set; } = "";
	public float[] Score { get; set; } = Array.Empty<float>();
}

using Overlay;

namespace WFA {

	internal static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.Run(new OverlayForm());
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}


	public class OverlayForm : Form {
		private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
		private Color lastAverage = Color.Black;

		public OverlayForm() {
			FormBorderStyle = FormBorderStyle.None;
			TopMost = true;
			ShowInTaskbar = false;
			BackColor = Color.Magenta;
			TransparencyKey = Color.Magenta;
			Bounds = Screen.PrimaryScreen.Bounds;

			timer.Interval = 250;
			timer.Tick += (_, _) => CheckPixelChange();
			timer.Start();
		}

		protected override CreateParams CreateParams {
			get {
				var cp = base.CreateParams;
				cp.ExStyle |= 0x80000;   // WS_EX_LAYERED
				cp.ExStyle |= 0x20;      // WS_EX_TRANSPARENT click-through
				cp.ExStyle |= 0x80;      // WS_EX_TOOLWINDOW
				return cp;
			}
		}

		private void CheckPixelChange() {
			Rectangle region = new Rectangle(100, 100, 100, 100);
			Color avg = GetAverageColor(region);

			int diff =
				Math.Abs(avg.R - lastAverage.R) +
				Math.Abs(avg.G - lastAverage.G) +
				Math.Abs(avg.B - lastAverage.B);

			if (diff > 40) {
				Invalidate();
			}

			lastAverage = avg;
		}

		private static Color GetAverageColor(Rectangle region) {
			using Bitmap bmp = new(region.Width, region.Height);
			using Graphics g = Graphics.FromImage(bmp);

			g.CopyFromScreen(region.Location, Point.Empty, region.Size);

			long r = 0, gr = 0, b = 0;
			int count = region.Width * region.Height;

			for (int x = 0; x < bmp.Width; x++)
				for (int y = 0; y < bmp.Height; y++) {
					Color c = bmp.GetPixel(x, y);
					r += c.R;
					gr += c.G;
					b += c.B;
				}

			return Color.FromArgb((int)(r / count), (int)(gr / count), (int)(b / count));
		}

		protected override void OnPaint(PaintEventArgs e) {
			using Font font = new("Segoe UI", 18);
			e.Graphics.DrawString(
				"Pixel change detected",
				font,
				Brushes.Lime,
				100,
				80
			);
		}
	}

}
