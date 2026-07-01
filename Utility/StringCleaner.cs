using System.Text.RegularExpressions;
namespace OLLM.Utility;

/// <summary>
/// This class does not preserve formatting; it removes images, headings, lists, blockquotes, and
/// horizontal lines, and simplifies bold and italic text.
/// </summary>
public static partial class StringCleaner {
	private const string _secondMatchingGroup = "$2";
	public static string Md(string text) {
		if (string.IsNullOrEmpty(text)) {
			return text;
		}
		text = ImageRegex().Replace(text, string.Empty);
		text = BoldItalicsRegex().Replace(text, _secondMatchingGroup);
		text = HeadingsListsBlockquotesRegex().Replace(text, string.Empty);
		text = HorizontalLineRegex().Replace(text, string.Empty);
		return text.Trim();
	}
	[GeneratedRegex(@"!\[.*?\]\(.*?\)")]
	private static partial Regex ImageRegex();
	[GeneratedRegex(@"(\*\*|__|\*|_)(.*?)\1")]
	private static partial Regex BoldItalicsRegex();
	[GeneratedRegex(@"^-{3,}|^\*{3,}|^_{3,}", RegexOptions.Multiline)]
	private static partial Regex HorizontalLineRegex();
	[GeneratedRegex(@"^\s{0,3}(#{1,6}|>|[\*\-\+]|\d+\.)\s+", RegexOptions.Multiline)]
	private static partial Regex HeadingsListsBlockquotesRegex();
}