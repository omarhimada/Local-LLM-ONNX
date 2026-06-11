using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Vision;
using Microsoft.SemanticKernel;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;

/*

# ML.NET Image Pattern Detector

A simple image classification project built with **ML.NET** and **Microsoft.ML.Vision** that detects whether a target pattern exists in an image.

This project uses transfer learning (ResNet) to train a classifier on labeled images and then predicts whether new images contain the pattern of interest.

---

## Features

- Train a custom image classifier using ML.NET
- Uses transfer learning with ResNet
- Supports PNG and JPG images
- Save and load trained models
- Evaluate model accuracy
- Predict labels on new images

---

## Requirements

- .NET 8.0 (or newer)
- Visual Studio 2022 / Rider / VS Code

NuGet packages:

```bash
dotnet add package Microsoft.ML
dotnet add package Microsoft.ML.ImageAnalytics
dotnet add package Microsoft.ML.Vision
```

---

## Dataset Structure

Organize images into folders named after their labels.

Example:

```text
data/
├── pattern/
│   ├── image1.png
│   ├── image2.png
│   └── image3.png
│
└── no_pattern/
    ├── image1.png
    ├── image2.png
    └── image3.png
```

Folder names become classification labels automatically.

---

## Training

Run the application:

```bash
dotnet run
```

The training pipeline will:

1. Load images from the dataset
2. Split data into train/test sets
3. Train a ResNet-based classifier
4. Evaluate accuracy
5. Save the model

Example output:

```text
MicroAccuracy: 95.4%
MacroAccuracy: 94.8%
```

Generated model:

```text
pattern-model.zip
```

---

## Making Predictions

After training:

```csharp
var result = engine.Predict(new ImageData
{
    ImagePath = "test.png"
});

Console.WriteLine(result.PredictedLabel);
```

Example:

```text
pattern
```

---

## Model Architecture

The default implementation uses:

```csharp
ImageClassificationTrainer.Architecture.ResnetV2101
```

Other available architectures include:

```csharp
ResnetV250
InceptionV3
MobilenetV2
```

---

## Improving Accuracy

For best results:

- Use at least 100-500 images per class
- Keep class sizes balanced
- Use varied examples
- Include difficult negative samples
- Use consistent image dimensions
- Increase epochs for larger datasets

Example:

```csharp
Epoch = 100
```

---

## Project Structure

```text
Project/
│
├── data/
│   ├── pattern/
│   └── no_pattern/
│
├── pattern-model.zip
│
├── Program.cs
│
└── README.md
```

---

## Limitations

This project performs image classification, not object detection.

Classification answers:

> Does this image contain the pattern?

It does not answer:

> Where is the pattern located?

For localization or bounding boxes, consider:

- ONNX Runtime
- YOLO
- Azure Custom Vision
- TensorFlow Object Detection

---

## License

MIT License

Use at your own risk.
 
*/
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
