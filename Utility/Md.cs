using System.Text;

namespace OLLM.Utility;

using static Constants;
using static MdFd;

public static class Md {
	public static List<FdBlockMd> Parse(string? s) {
		if (s == null) {
			return [];
		}

		List<FdBlockMd> blocks = [];
		string[] lines = s.Replace(_nlrs, _nl).Split(_nlc);

		bool inCode = false;
		StringBuilder code = new();

		List<string> paraLines = [];
		int pendingHeading = 0;

		List<string> bulletLines = [];

		string[] thinkingLineRange = [];
		string postThinkingTextStart = string.Empty;
		int indexOfThinkEnd = -1;

		if (lines[0].StartsWith("<think>")) {
			lines[0] = string.Empty;
			// With the system prompt construction in Constants, the reasoning model will begin with <think>
			int indexOfThinkStart = 0;
			string? lineWithEndThink = lines.FirstOrDefault(line => line.Contains("</think>"));
			if (lineWithEndThink != null) {
				indexOfThinkEnd = lines.IndexOf(lineWithEndThink);

				thinkingLineRange = lines[indexOfThinkStart..indexOfThinkEnd];
				string[] splitLineWithEndThink = lineWithEndThink.Split("</think>");

				string lastThinkingPortion = splitLineWithEndThink[0];
				postThinkingTextStart = splitLineWithEndThink[1];

				ParagraphFdBlockMd thinkingFdBlockMd = new(ParseInlines(string.Join(_wsc, thinkingLineRange.Append(lastThinkingPortion))), 0);
				blocks.Add(thinkingFdBlockMd);
			} else {
				// Malformed - thinking starts but does not end. 
				lines[0] = lines[0].Replace("<think>", string.Empty);
			}
		}

		if (!string.IsNullOrEmpty(postThinkingTextStart) && indexOfThinkEnd != -1) {
			// The model was thinking
			// Separate the thinking in its own paragraph block that we can later style differently, or we can keep it hidden.
			lines = lines.Except(thinkingLineRange).ToArray();
		}

		string? lang = null;
		foreach (string raw in lines) {
			if (raw == _lineBreak) {
				blocks.Add(new LineBreakMdBlock());
				continue;
			}

			bool tbt = raw.StartsWith(_tbt);
			if (tbt) {
				if (!inCode) {
					FlushBullets();
					FlushParagraph();
					inCode = true;
					if (lang == null && tbt) {
						// ```csharp?
						lang = raw.Split(_tbt)[1].TrimEnd();
					}
				} else {
					inCode = false;
					blocks.Add(new CodeFdBlockMd(code.ToString().TrimEnd(_nlc, _rc), null));
				}

				code.Clear();
				continue;
			}

			if (inCode) {
				code.AppendLine(raw);
				continue;
			}

			if (string.IsNullOrWhiteSpace(raw)) {
				FlushBullets();
				FlushParagraph();
				continue;
			}

			int level = CountHeadingLevel(raw);
			if (level > 0) {
				FlushBullets();
				FlushParagraph();

				string headingText = raw[level..].TrimStart();
				pendingHeading = level;
				paraLines.Add(headingText);
				FlushParagraph();
				continue;
			}

			if (raw.StartsWith(_ds) || raw.StartsWith(_os)) {
				FlushParagraph();
				bulletLines.Add(raw[2..].Trim());
				continue;
			}

			FlushBullets();
			paraLines.Add(raw.Trim());
		}

		FlushBullets();
		FlushParagraph();

		if (inCode && code.Length > 0) {
			blocks.Add(new CodeFdBlockMd(code.ToString().TrimEnd(_nlc, _rc), lang));
		}

		return blocks;

		void FlushParagraph() {
			if (paraLines.Count == 0) {
				return;
			}

			string text = string.Join(_wsc, paraLines.Select(x => x.Trim()).Where(x => x.Length > 0)).Trim();
			paraLines.Clear();

			if (text.Length == 0) {
				return;
			}

			List<InlineSpan> inlines = ParseInlines(text);
			blocks.Add(new ParagraphFdBlockMd(inlines, pendingHeading));
			pendingHeading = 0;
		}

		void FlushBullets() {
			if (bulletLines.Count == 0) {
				return;
			}

			List<List<InlineSpan>> items = [];
			foreach (string b in bulletLines) {
				items.Add(ParseInlines(b));
			}

			bulletLines.Clear();
			blocks.Add(new BulletListFdBlockMd(items));
		}
	}
}