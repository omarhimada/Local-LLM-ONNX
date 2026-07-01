namespace OLLM.Utility.J2CS.LexicalAndParsingModels.Jinja;
/// <summary>
/// Provides lexical analysis for Jinja template strings, producing tokens representing text, output expressions, and
/// tags, to eventually construct the abstract syntax tree required to form the generated C# code.
/// </summary>
internal sealed class JinjaLexer(string s) {
	private int _i;
	public Token Next() {
		if (_i >= s.Length) {
			return new Token(TokenKind.Eof, string.Empty, _i);
		}
		int start = _i;
		// Find next delimiter {{ or {%
		int idxOut = s.IndexOf("{{", _i, StringComparison.Ordinal);
		int idxTag = s.IndexOf("{%", _i, StringComparison.Ordinal);
		int idx;
		TokenKind kind;
		switch (idxOut) {
			case -1 when idxTag == -1:
				_i = s.Length;
				return new Token(TokenKind.Text, s[start..], start);
			case -1:
				idx = idxTag;
				kind = TokenKind.Tag;
				break;
			default: {
					if (idxTag == -1 || idxOut < idxTag) {
						idx = idxOut;
						kind = TokenKind.Output;
					} else {
						idx = idxTag;
						kind = TokenKind.Tag;
					}
					break;
				}
		}
		if (idx > _i) {
			_i = idx;
			return new Token(TokenKind.Text, s[start..idx], start);
		}
		// We are at a delimiter.
		if (kind == TokenKind.Output) {
			// {{ ... }}
			// Allow whitespace-control variants {{- and -}}
			bool leftTrim = StartsWith("{{-");
			_i += leftTrim ? 3 : 2;
			int end = s.IndexOf("}}", _i, StringComparison.Ordinal);
			if (end < 0) {
				throw new Exception("Unclosed output }} at " + start);
			}
			// handle -}} right trim
			bool rightTrim = end - 1 >= _i && s[end - 1] == '-';
			int innerEnd = rightTrim ? end - 1 : end;
			string inner = s[_i..innerEnd];
			_i = end + 2;
			return new Token(TokenKind.Output, inner, start);
		} else {
			// {% ... %}
			bool leftTrim = StartsWith("{%-");
			_i += leftTrim ? 3 : 2;
			int end = s.IndexOf("%}", _i, StringComparison.Ordinal);
			if (end < 0) {
				throw new Exception("Unclosed tag %} at " + start);
			}
			bool rightTrim = end - 1 >= _i && s[end - 1] == '-';
			int innerEnd = rightTrim ? end - 1 : end;
			string inner = s[_i..innerEnd];
			_i = end + 2;
			return new Token(TokenKind.Tag, inner, start);
		}
	}
	private bool StartsWith(string prefix)
		=> s.AsSpan(_i).StartsWith(prefix.AsSpan(), StringComparison.Ordinal);
}
