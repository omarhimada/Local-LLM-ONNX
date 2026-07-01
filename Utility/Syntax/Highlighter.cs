using System.Text.RegularExpressions;

namespace OLLM.Utility.Syntax;

internal static class Highlighter {
	internal static IEnumerable<(TokenKind Kind, string Text)> Highlight(string code, LanguageDefinition lang) {
		if (string.IsNullOrEmpty(code))
			yield break;

		int i = 0;
		while (i < code.Length) {
			Match? best = null;
			TokenRule? bestRule = null;

			foreach (TokenRule rule in lang.Rules) {
				Match m = rule.Regex.Match(code, i);
				if (!m.Success) {
					continue;
				}

				if (best is null || m.Index < best.Index) {
					best = m;
					bestRule = rule;
					continue;
				}

				if (m.Index == best.Index) {
					if (rule.Priority > bestRule!.Priority || rule.Priority == bestRule!.Priority && m.Length > best.Length) {
						best = m;
						bestRule = rule;
					}
				}
			}

			if (best is null) {
				yield return (TokenKind.Plain, code[i..]);
				yield break;
			}

			if (best.Index > i) {
				yield return (TokenKind.Plain, code.Substring(i, best.Index - i));
			}

			string tokenText = best.Value;
			TokenKind kind = bestRule!.Kind;

			if (kind == TokenKind.Identifier) {
				if (lang.Keywords.Contains(tokenText)) {
					kind = TokenKind.Keyword;
				} else if (lang.Types.Contains(tokenText)) {
					kind = TokenKind.TypeName;
				}
			}

			yield return (kind, tokenText);
			i = best.Index + best.Length;
		}
	}
}