using System.Text.RegularExpressions;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace OLLM.Utility.Syntax;

public enum TokenKind {
	Plain,
	Keyword,
	TypeName,
	String,
	Number,
	Comment,
	Punctuation,
	Operator,
	Identifier,
	Preprocessor,
	Attribute,
}

public sealed record TokenRule(TokenKind Kind, Regex Regex, int Priority);

public sealed class LanguageDefinition(string name) {
	public string Name { get; } = name;
	public HashSet<string> Keywords { get; } = new(StringComparer.Ordinal);
	public HashSet<string> Types { get; } = new(StringComparer.Ordinal);
	public List<TokenRule> Rules { get; } = [];
	public override string ToString() => Name;
}

public static class Theme {
	static Theme() {
		Plain.Freeze();
		Keyword.Freeze();
		TypeName.Freeze();
		String.Freeze();
		Number.Freeze();
		Comment.Freeze();
		Punctuation.Freeze();
		Operator.Freeze();
		Identifier.Freeze();
		Preprocessor.Freeze();
		Attribute.Freeze();
	}

	public static readonly SolidColorBrush Plain = new(Color.FromRgb(242, 232, 223));
	public static readonly SolidColorBrush Keyword = new(Color.FromRgb(242, 178, 99));
	public static readonly SolidColorBrush TypeName = new(Color.FromRgb(88, 211, 219));
	public static readonly SolidColorBrush String = new(Color.FromRgb(193, 168, 224));
	public static readonly SolidColorBrush Number = new(Color.FromRgb(137, 226, 164));
	public static readonly SolidColorBrush Comment = new(Color.FromRgb(134, 166, 157));
	public static readonly SolidColorBrush Punctuation = new(Color.FromRgb(242, 232, 223));
	public static readonly SolidColorBrush Operator = new(Color.FromRgb(242, 232, 223));
	public static readonly SolidColorBrush Identifier = new(Color.FromRgb(242, 232, 223));
	public static readonly SolidColorBrush Preprocessor = new(Color.FromRgb(246, 220, 153));
	public static readonly SolidColorBrush Attribute = new(Color.FromRgb(242, 0, 82));

	public static Brush For(TokenKind kind) => kind switch {
		TokenKind.Keyword => Keyword,
		TokenKind.TypeName => TypeName,
		TokenKind.String => String,
		TokenKind.Number => Number,
		TokenKind.Comment => Comment,
		TokenKind.Punctuation => Punctuation,
		TokenKind.Operator => Operator,
		TokenKind.Identifier => Identifier,
		TokenKind.Preprocessor => Preprocessor,
		TokenKind.Attribute => Attribute,
		_ => Plain
	};
}
