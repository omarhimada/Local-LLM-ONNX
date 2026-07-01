using OLLM.Utility.J2CS.AbstractSyntaxTree;
using OLLM.Utility.J2CS.AbstractSyntaxTree.Models;
namespace OLLM.Utility.J2CS.LexicalAndParsingModels.Jinja;
using static Constants;
/// <summary>
/// Parses Jinja template strings using the Lexer to form an abstract syntax tree (AST).
/// </summary>
/// <remarks>
internal sealed class Parser {
	private readonly JinjaLexer _lexer;
	private Token _currentToken;
	public Parser(string s) {
		_lexer = new JinjaLexer(s);
		_currentToken = _lexer.Next();
	}
	public RootNode ParseTemplate() {
		List<Node> nodes = ParseUntil(endTags: null);
		Expect(TokenKind.Eof);
		return new RootNode(nodes);
	}
	private List<Node> ParseUntil(HashSet<string>? endTags) {
		List<Node> nodes = [];
		while (_currentToken.Kind != TokenKind.Eof) {
			switch (_currentToken.Kind) {
				case TokenKind.Text:
					nodes.Add(new TextNode(_currentToken.Value));
					_currentToken = _lexer.Next();
					continue;
				case TokenKind.Output:
					nodes.Add(new OutputNode(_currentToken.Value.Trim()));
					_currentToken = _lexer.Next();
					continue;
				case TokenKind.Tag:
				case TokenKind.Eof:
					break;
				default:
					throw new NotImplementedException();
			}
			string tag = _currentToken.Value.Trim();
			string head = FirstWord(tag);
			if (endTags != null && endTags.Contains(head)) {
				return nodes;
			}
			switch (head) {
				case _if:
					nodes.Add(ParseIf());
					continue;
				case _for:
					nodes.Add(ParseFor());
					continue;
				case _set:
					nodes.Add(ParseSet());
					continue;
				default:
					// Unknown tag in this subset
					throw new InvalidCastException($"Unsupported tag '{head}' at position {_currentToken.Pos}: '{tag}'");
			}
		}
		return endTags != null
			? throw new Exception($"Unexpected EOF; expected one of: {string.Join(", ", endTags)}")
			: nodes;
	}
	private IfNode ParseIf() {
		// Current: {% if cond %}
		string tag = _currentToken.Value.Trim();
		string cond = tag[_if.Length..].Trim();
		_currentToken = _lexer.Next();
		List<(string Cond, List<Node> Body)> branches = [
			(cond, ParseUntil([_elif, _else, _endif]))
		];
		List<Node>? elseBody = null;
		while (_currentToken.Kind == TokenKind.Tag) {
			string t = _currentToken.Value.Trim();
			string h = FirstWord(t);
			if (h == _elif) {
				string c = t[_elif.Length..].Trim();
				_currentToken = _lexer.Next();
				List<Node> body = ParseUntil([_elif, _else, _endif]);
				branches.Add((c, body));
				continue;
			}
			if (h == _else) {
				_currentToken = _lexer.Next();
				elseBody = ParseUntil([_endif]);
				break;
			}
			if (h == _endif) {
				break;
			}
			throw new Exception($"Unexpected tag inside {_if}: {t}");
		}
		// Cease with endif
		ExpectTag(_endif);
		_currentToken = _lexer.Next();
		return new IfNode(branches, elseBody);
	}
	private ForNode ParseFor() {
		// {% for x in xs %}
		string tag = _currentToken.Value.Trim();
		string rest = tag[_for.Length..].Trim();
		// Naive split around " in "
		int idx = rest.IndexOf(" in ", StringComparison.Ordinal);
		if (idx < 0) {
			throw new Exception("Malformed for tag: " + tag);
		}
		string varName = rest[..idx].Trim();
		string iterable = rest[(idx + 4)..].Trim();
		_currentToken = _lexer.Next();
		List<Node> body = ParseUntil(["endfor"]);
		ExpectTag("endfor");
		_currentToken = _lexer.Next();
		return new ForNode(varName, iterable, body);
	}
	private SetNode ParseSet() {
		// {% set name = expr %}
		string tag = _currentToken.Value.Trim();
		string rest = tag[$"{_set}".Length..].Trim();
		int eq = rest.IndexOf('=', StringComparison.Ordinal);
		if (eq < 0) {
			throw new Exception($"Malformed {_set} tag: " + tag);
		}
		string name = rest[..eq].Trim();
		string expr = rest[(eq + 1)..].Trim();
		_currentToken = _lexer.Next();
		return new SetNode(name, expr);
	}
	private void Expect(TokenKind k) {
		if (_currentToken.Kind != k) {
			throw new Exception($"Expected {k} but got {_currentToken.Kind} at {_currentToken.Pos}");
		}
	}
	private void ExpectTag(string head) {
		if (_currentToken.Kind != TokenKind.Tag) {
			throw new Exception($"Expected tag '{head}' but got {_currentToken.Kind} at {_currentToken.Pos}");
		}
		string h = FirstWord(_currentToken.Value.Trim());
		if (h != head) {
			throw new Exception($"Expected tag '{head}' but got '{h}' at {_currentToken.Pos}");
		}
	}
	private static string FirstWord(string s) {
		int i = 0;
		while (i < s.Length && char.IsWhiteSpace(s[i]))
			i++;
		int j = i;
		while (j < s.Length && !char.IsWhiteSpace(s[j]))
			j++;
		return s[i..j];
	}
}