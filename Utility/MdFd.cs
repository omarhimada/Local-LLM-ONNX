using System.Windows.Media;

namespace OLLM.Utility;

using static Constants;

internal static class MdFd {
	static MdFd() {
		_black.Freeze();
		_ow.Freeze();
		_owd.Freeze();
		_white.Freeze();
	}

	internal static readonly FontFamily _fontFamilyOverpass = new(
		new Uri(_resourceFontFamilyDeclarationPrefix),
		$"{_resourceFontFamilyLocationPrefix}Overpass");

	internal static readonly FontFamily _fontFamily0x = new(
		new Uri(_resourceFontFamilyDeclarationPrefix),
		$"{_resourceFontFamilyLocationPrefix}0xProto");

	internal static SolidColorBrush _black = new(Color.FromRgb(0, 0, 0));
	internal static SolidColorBrush _white = new(Color.FromRgb(255, 255, 255));
	internal static SolidColorBrush _ow = new(Color.FromRgb(150, 144, 144));
	internal static SolidColorBrush _owd = new(Color.FromRgb(24, 23, 23));

	internal static SolidColorBrush _readyDosOrange = new(Color.FromRgb(252, 164, 105));
	internal static SolidColorBrush _readyDosPurple = new(Color.FromRgb(60, 36, 145));
	internal static SolidColorBrush _readyDosPink = new(Color.FromRgb(236, 179, 205));

	// ReadyDOS logo colors
	//FCA469 (orange) 252, 164, 105
	//3C2491 (purple) 60, 36, 145
	//ECB3CD (pink) 236, 179, 205

	internal static int CountHeadingLevel(string line) {
		int i = 0;
		while (i < line.Length && i < 6 && line[i] == _pio)
			i++;
		return i == 0 ? 0 : i < line.Length && char.IsWhiteSpace(line[i]) ? i : 0;
	}

	internal static List<InlineSpan> ParseInlines(string text) {
		List<InlineSpan> result = [];
		int i = 0;

		while (i < text.Length) {
			if (text[i] == _tc) {
				int end = text.IndexOf(_tc, i + 1);
				if (end > i) {
					string code = text.Substring(i + 1, end - i - 1);
					if (code.Length > 0) {
						result.Add(new CodeSpan(code));
					}

					i = end + 1;
					continue;
				}

				AddText(_t);
				i++;
				continue;
			}

			if (i + 1 < text.Length && text[i] == _osc && text[i + 1] == _osc) {
				int end = text.IndexOf(_ts, i + 2, StringComparison.Ordinal);
				if (end > i) {
					string inner = text.Substring(i + 2, end - (i + 2));
					List<InlineSpan> children = ParseInlines(inner);
					result.Add(new BoldSpan(children));
					i = end + 2;
					continue;
				}

				AddText(_ts);
				i += 2;
				continue;
			}

			if (text[i] == _osc) {
				int end = text.IndexOf(_osc, i + 1);
				if (end > i) {
					string inner = text.Substring(i + 1, end - i - 1);
					List<InlineSpan> children = ParseInlines(inner);
					result.Add(new ItalicSpan(children));
					i = end + 1;
					continue;
				}

				AddText(_oss);
				i++;
				continue;
			}

			int next = NextSpecial(text, i);
			AddText(text.Substring(i, next - i));
			i = next;
		}

		return result;

		void AddText(string t) {
			if (string.IsNullOrEmpty(t)) {
				return;
			}

			if (result.Count > 0 && result[^1] is TextSpan ts) {
				result[^1] = ts with { Text = ts.Text + t };
			} else {
				result.Add(new TextSpan(t));
			}
		}
	}

	internal static int NextSpecial(string s, int start) {
		int best = s.Length;

		int t1 = s.IndexOf(_tc, start);
		if (t1 >= 0) {
			best = Math.Min(best, t1);
		}

		int t2 = s.IndexOf(_ts, start, StringComparison.Ordinal);
		if (t2 >= 0) {
			best = Math.Min(best, t2);
		}

		int t3 = s.IndexOf(_osc, start);
		if (t3 >= 0) {
			best = Math.Min(best, t3);
		}

		return best;
	}
}