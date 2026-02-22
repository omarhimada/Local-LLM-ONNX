using OLLM.Utility.Syntax;
using System.Windows;
using System.Windows.Documents;

namespace OLLM.Utility;

using static MdFd;

internal static class Fd {
	internal static FlowDocument Render(IEnumerable<FdBlockMd> blocks) {
		FlowDocument doc = new() {
			PageWidth = 800,
		};

		foreach (FdBlockMd b in blocks) {
			switch (b) {
				case ParagraphFdBlockMd p:
					doc.Blocks.Add(RenderParagraph(p));
					break;

				case BulletListFdBlockMd bl:
					doc.Blocks.Add(RenderBulletList(bl));
					break;

				case CodeFdBlockMd c:
					doc.Blocks.Add(MdSyntaxApply.RenderCodeBlock(
						c.Code,
						c.Lang,
						14d));
					break;
			}
		}
		return doc;
	}

	internal static Paragraph RenderParagraph(ParagraphFdBlockMd p) {
		Paragraph para = new() {
			Margin = new Thickness(0, p.HeadingLevel > 0 ? 10 : 6, 0, 6)
		};
		if (p.HeadingLevel > 0) {
			para.FontWeight = FontWeights.SemiBold;
			para.FontSize = p.HeadingLevel switch {
				1 => 22,
				2 => 18,
				3 => 16,
				4 => 14,
				_ => 13
			};
		}
		foreach (Inline inline in RenderInlines(p.Inlines)) {
			para.Inlines.Add(inline);
		}
		return para;
	}

	internal static List RenderBulletList(BulletListFdBlockMd bl) {
		List list = new() {
			MarkerStyle = TextMarkerStyle.Disc,
			Margin = new Thickness(18, 4, 0, 8)
		};

		foreach (List<InlineSpan> item in bl.Items) {
			Paragraph para = new() { Margin = new Thickness(0, 2, 0, 2) };
			foreach (Inline inline in RenderInlines(item))
				para.Inlines.Add(inline);

			list.ListItems.Add(new ListItem(para));
		}

		return list;
	}

	internal static Paragraph RenderCodeBlock(CodeFdBlockMd c) {
		Paragraph para = new(new Run(c.Code)) {
			FontFamily = _fontFamily0x,
			Background = _black,
			Foreground = _readyDosPink,
			Padding = new Thickness(10),
			Margin = new Thickness(0, 8, 0, 8),
			LineHeight = 14,
		};
		return para;
	}

	internal static IEnumerable<Inline> RenderInlines(IEnumerable<InlineSpan> spans) {
		foreach (InlineSpan s in spans) {
			switch (s) {
				case TextSpan t:
					yield return new Run(t.Text) {
						FontFamily = _fontFamilyOverpass,
						Background = _black,
						Foreground = _white,
					};
					break;

				case CodeSpan c:
					yield return new Run(c.Text) {
						FontFamily = _fontFamily0x,
						Background = _black,
						Foreground = _readyDosOrange,
					};
					break;

				case BoldSpan b: {
						Bold bold = new();
						foreach (Inline child in RenderInlines(b.Children))
							bold.Inlines.Add(child);
						yield return bold;
						break;
					}

				case ItalicSpan it: {
						Italic italic = new();
						foreach (Inline child in RenderInlines(it.Children))
							italic.Inlines.Add(child);
						yield return italic;
						break;
					}
			}
		}
	}
}
