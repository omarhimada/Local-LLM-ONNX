using OLLM.Utility.J2CS.AbstractSyntaxTree;
using OLLM.Utility.J2CS.AbstractSyntaxTree.Models;
using System.Text;
using static OLLM.Utility.J2CS.Constants;
namespace OLLM.Utility.J2CS;
/// <summary>
/// Provides functionality to generate the C# source code that mimics the Jinja chat template, using an abstract syntax tree (AST).
/// </summary>
internal sealed class GenerateCode {
	private readonly StringBuilder _sb = new();
	private int _indent;
	/// <summary>
	/// Initializes the Jinja to C# code conversion with using statements, class structure, etc.
	/// </summary>
	internal string Generate(RootNode root, string className) {
		_sb.Clear();
		_indent = 0;
		Line(_usingSystem);
		Line(_usingCollections);
		Line(_usingCollectionsGeneric);
		Line(_usingLinq);
		Line(_usingText);
		Line(_usingJson);
		Line(string.Empty);
		Line($"{_publicSealedClass} {className}");
		Line(_openBrace);
		Indent();
		EmitHelpers();
		Line(string.Empty);
		EmitContextType();
		Line(string.Empty);
		EmitRender(root);
		Unindent();
		Line(_closeBrace);
		return _sb.ToString();
	}
	/// <summary>
	/// Generates the code required to render the specified root node and its child nodes, in a tree-like fashion.
	/// </summary>
	private void EmitRender(RootNode root) {
		Line(_renderMethodSignature);
		Line(_openBrace);
		Indent();
		Line(_stringBuilderInit);
		Line(_localsInit);
		EmitNodes(root.Nodes);
		Line(_returnSb);
		Unindent();
		Line(_closeBrace);
	}
	/// <summary>
	/// For each of the nodes in this abstract syntax tree emit the appropriate C# representation.
	/// </summary>
	/// <param name="nodes"></param>
	/// <exception cref="NotSupportedException">
	///	This program was created to support typical LLM/chat prompt templates and may not support the full
	///	range of abilities you may find within Jinja templates.
	///	It hypothetically should work with Qwen, Mistral, LLaMA, and other models like them.
	/// </exception>
	private void EmitNodes(List<Node> nodes) {
		foreach (Node n in nodes) {
			switch (n) {
				case TextNode t:
					//if (t.Text == "\n    ");
					AppendText(t.Text);
					break;
				case OutputNode o:
					string singleQuotedMisconversion = ConvertExpression(o.Expr);
					string corrected = singleQuotedMisconversion;
					if (corrected.Length > 1) {
						// Length is greater than 1, and it is wrapped in single quotes then clearly it isn't a char
						if (corrected[0] == '\'' && corrected[^1] == '\'') {
							corrected = corrected.Replace('\'', '\"');
						}
					}
					Line($"sb.Append(ToStringSafe({corrected}));");
					break;
				case SetNode s:
					Line($"locals[{Lit(s.Name)}] = {ConvertExpression(s.Expr)};");
					break;
				case ForNode f:
					EmitFor(f);
					break;
				case IfNode i:
					EmitIf(i);
					break;
				default:
					throw new NotSupportedException(n.GetType().Name);
			}
		}
	}
	/// <summary>
	/// Emit an 'if' condition.
	/// </summary>
	/// <param name="node">The current tree node.</param>
	private void EmitIf(IfNode node) {
		for (int i = 0; i < node.Branches.Count; i++) {
			(string cond, List<Node> body) = node.Branches[i];
			string kw = (i == 0) ? "if" : "else if";
			Line($"{kw} (Truthy({ConvertExpression(cond)}))");
			Line("{");
			Indent();
			EmitNodes(body);
			Unindent();
			Line("}");
		}
		if (node.ElseBody != null) {
			Line("else");
			Line("{");
			Indent();
			EmitNodes(node.ElseBody);
			Unindent();
			Line("}");
		}
	}
	/// <summary>
	/// Emit a loop.
	/// </summary>
	/// <param name="node">The current tree node.</param>
	private void EmitFor(ForNode node) {
		string iterExpr = ConvertExpression(node.IterableExpr);
		Line("{");
		Indent();
		Line($"var __iter = AsEnumerable({iterExpr}).ToList();");
		Line("for (int __i = 0; __i < __iter.Count; __i++)");
		Line("{");
		Indent();
		Line($"object? {SanitizeIdent(node.VarName)} = __iter[__i];");
		Line("LoopInfo loop = new(index0: __i, count: __iter.Count);");
		EmitNodes(node.Body);
		Unindent();
		Line("}");
		Unindent();
		Line("}");
	}
	/// <summary>
	/// Append text.
	/// </summary>
	/// <param name="text">This is when one of the nodes in the abstract syntax tree is simply text.</param>
	private void AppendText(string text) {
		if (text.Length == 0) {
			return;
		}
		// Use verbatim literality 
		Line($"sb.Append({Lit(text)});");
	}
	/// <summary>
	/// Convert the Jinja expression to 
	/// </summary>
	/// <param name="jinjaExpr"></param>
	private string ConvertExpression(string jinjaExpr) {
		string e = jinjaExpr.Trim();
		//Get rid of the single quotes at the edges
		// '<|im_start|>system\n' ==> <| im_start |> system\n
		//if (e[0] == '\'' && e[^1] == '\'') {
		//	e = e.Substring(1, e.Length - 2);
		//}
		// Handle filters: a | tojson
		// TODO only a single filter chain is assumed, this could cause issues for certain chat_templates
		// Example: tool | tojson  => ToJson(tool)
		// Example: tool_call.arguments | tojson => ToJson(...)
		int verticalBar = FindTopLevelVerticalBar(e);
		if (verticalBar >= 0) {
			string left = e[..verticalBar].Trim();
			string filter = e[(verticalBar + 1)..].Trim();
			return filter == "tojson" ? $"ToJson({ConvertExpression(left)})" :
				// TODO perhaps a better way to notify the user that the transpilation did not go as expected
				$"/* unsupported_filter */ToStringSafe({ConvertExpression(left)})";
		}
		// "is defined"
		// Example: tool_call.function is defined => IsDefined(Resolve(...))
		if (e.EndsWith(" is defined", StringComparison.Ordinal)) {
			string left = e[..^" is defined".Length].Trim();
			return $"IsDefined({ConvertExpression(left)})";
		}
		// Rewrite boolean operators (word-boundary)
		e = ReplaceWord(e, "and", "&&");
		e = ReplaceWord(e, "or", "||");
		e = ReplaceWord(e, "not", "!");
		// Special: true/false/null not typically in Jinja; keep as-is if present.
		return RewriteAccessChains(e);
	}
	private static int FindTopLevelVerticalBar(string s) {
		int depth = 0;
		bool isInsideSingleQuote = false, isInsideDoubleQuote = false;
		for (int i = 0; i < s.Length; i++) {
			char c = s[i];
			if (c == '\\') {
				i++;
				continue;
			}
			if (!isInsideDoubleQuote && c == '\'') {
				isInsideSingleQuote = !isInsideSingleQuote;
			} else if (!isInsideSingleQuote && c == '"') {
				isInsideDoubleQuote = !isInsideDoubleQuote;
			}
			if (isInsideSingleQuote || isInsideDoubleQuote) {
				continue;
			}
			switch (c) {
				case '(' or '[':
					depth++;
					break;
				case ')' or ']':
					depth--;
					break;
				case '|' when depth == 0:
					return i;
			}
		}
		return -1;
	}
	private static string ReplaceWord(string s, string word, string repl) {
		// naive but works for typical templates
		return string.Join(repl, s.Split([$" {word} "], StringSplitOptions.None));
	}
	/// <summary>
	///	Rewrite variable/property/index access into helper calls.
	///	Parse "primary" chain i.e.: messages[0]['role'] or message.tool_calls, etc.
	///	Keep operators like +, ==, !=, parentheses intact, and preserve string literals.
	/// </summary>
	private string RewriteAccessChains(string expression) {
		// When an identifier start is seen it is parsed as an access chain:
		// e.g.: ident ( .ident | [indexExpr] | ['key'] )*
		// Then, it is rewritten to Resolve(context, locals, ..., chain)
		// LoopInfo "loop" is a local variable in generated code; treat it as an identifier also.
		StringBuilder sb = new();
		int i = 0;
		while (i < expression.Length) {
			char c = expression[i];
			// strings
			if (c is '\'' or '"') {
				int j = i;
				char q = c;
				sb.Append(q);
				i++;
				while (i < expression.Length) {
					char cc = expression[i];
					sb.Append(cc);
					i++;
					if (cc == '\\' && i < expression.Length) {
						sb.Append(expression[i]);
						i++;
						continue;
					}
					if (cc == q) {
						break;
					}
				}
				continue;
			}
			// Identifier start
			if (IsIdentifierStart(c)) {
				int start = i;
				i++;
				while (i < expression.Length && IsIdentPart(expression[i]))
					i++;
				string ident = expression[start..i];
				// Parse chain
				List<string> ops = [];
				while (i < expression.Length) {
					if (expression[i] == '.') {
						int dot = i;
						i++;
						int s2 = i;
						if (i < expression.Length && IsIdentifierStart(expression[i])) {
							i++;
							while (i < expression.Length && IsIdentPart(expression[i]))
								i++;
							string prop = expression[s2..i];
							ops.Add($".{prop}");
							continue;
						}
						// Malformed? Keep literal.
						i = dot;
						break;
					}
					if (expression[i] == '[') {
						int bracketStart = i;
						int depth = 0;
						bool inS = false, inD = false;
						do {
							char ch = expression[i];
							if (ch == '\\') {
								i += 2;
								continue;
							}
							if (!inD && ch == '\'') {
								inS = !inS;
							} else if (!inS && ch == '"') {
								inD = !inD;
							}
							if (!inS && !inD) {
								switch (ch) {
									case '[':
										depth++;
										break;
									case ']':
										depth--;
										break;
								}
							}
							i++;
						} while (i < expression.Length && depth > 0);
						string inside = expression[(bracketStart + 1)..(i - 1)].Trim();
						ops.Add($"[{inside}]");
						continue;
					}
					break;
				}
				// Decide if this identifier is a known C# operator keyword etc.
				// We rewrite common Jinja variables into context/locals lookup:
				//   tools, messages, add_generation_prompt, plus loop, message, tool_call vars.
				// If chain exists, resolve through helpers.
				string rewritten = RewritePrimary(ident, ops);
				sb.Append(rewritten);
				continue;
			}
			// other char
			sb.Append(c);
			i++;
		}
		return sb.ToString();
	}
	/// <summary>
	/// Generates a string expression representing access to a primary variable or context field,
	/// applying a sequence of property and index operations.
	/// </summary>
	private string RewritePrimary(string ident, List<string> operations) {
		// Base object expression:
		// - locals["x"] if it exists, else context.X if matches known, else plain identifier (for loop/message/tool_call variable)
		// known context fields
		string baseExpression = ident is _tools or _messages or _addGenerationPrompt ? $"context.{ident}" :
			// Prefer 'locals' for set variables, but we don't know at compile time,
			// so we emit a runtime check:
			$"GetVar({_context}, {_locals}, {Lit(ident)}, fallback: {SanitizeIdent(ident)})";
		// Apply ops
		foreach (string op in operations) {
			if (op.StartsWith('.')) {
				string prop = op[1..];
				baseExpression = $"GetProp({baseExpression}, {Lit(prop)})";
			} else if (op.StartsWith('[')) {
				string inside = op[1..^1].Trim();
				// 'string key?' is similar to 'role'
				if ((inside is ['\'', _, ..] && inside[^1] == '\'') ||
					(inside[0] == '"' && inside[^1] == '"')) {
					// Keep key literal as-is
					baseExpression = $"GetIndex({baseExpression}, {inside})";
				} else {
					// Expression index
					baseExpression = $"GetIndex({baseExpression}, {RewriteAccessChains(inside)})";
				}
			}
		}
		return baseExpression;
	}
	private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';
	private static bool IsIdentPart(char c) => char.IsLetterOrDigit(c) || c == '_';
	private static string SanitizeIdent(string s) {
		// minimal sanitization
		return string.IsNullOrEmpty(s) ? "_" : !IsIdentifierStart(s[0]) ? "_" + s : s;
	}
	private static string Lit(string s)
		=> @"@""" + s.Replace("\"", "\"\"") + @"""";
	/// <summary>
	/// Emits helper method definitions and related code constructs required for generated output.
	/// </summary>
	/// <remarks>
	/// This method should be called to ensure that all necessary helper functions and supporting code
	/// are included in the generated output. The emitted helpers may include utility methods for type conversion,
	/// property and index access, truthiness evaluation, and other common operations needed by the generated
	/// code.
	/// </remarks>
	private void EmitHelpers() {
		Line(_toStringSafe);
		Line(_isDefined);
		Line(_toJson);
		Line(_asEnumerableSig);
		Line(_openBrace);
		Indent();
		Line(_asEnumNull);
		Line(_asEnumString);
		Line(_asEnumEnumerable);
		Line(_asEnumFallback);
		Unindent();
		Line(_closeBrace);
		Line(_getVariableMethodSignature);
		Line(_openBrace);
		Indent();
		Line(_getVarTry);
		Line(_getVarFallback);
		Unindent();
		Line(_closeBrace);
		Line(_getPropMethodSignature);
		Line(_openBrace);
		Indent();
		Line(_getPropNull);
		Line(_getPropType);
		Line(_getPropertyInfo);
		Line(_getPropPiReturn);
		Line(_getPropFi);
		Line(_getPropFiReturn);
		Line(_getPropDict1);
		Line(_getPropDict2);
		Line(_getPropReturnNull);
		Unindent();
		Line(_closeBrace);
		Line(_getIndexMethodSignature);
		Line(_openBrace);
		Indent();
		Line(_getIndexNull);
		Line(_getIfIndexIsString);
		Line(_openBrace);
		Indent();
		Line(_getIndexDict1);
		Line(_getIndexDict2);
		Line(_getIndexProp);
		Line(_getIndexPropReturn);
		Line(_getIndexStringReturnNull);
		Unindent();
		Line(_closeBrace);
		Line(_getIndexIntCast);
		Line(_getIndexIntNull);
		Line(_getIfObjectIsList);
		Line(_openBrace);
		Indent();
		Line(_getIndexListIi);
		Line(_getIndexListBounds);
		Line(_getIndexListReturn);
		Unindent();
		Line(_closeBrace);
		Line(_getIfObjectIsArray);
		Line(_openBrace);
		Indent();
		Line(_getIndexListIi);
		Line(_getIndexArrayBounds);
		Line(_getIndexArrayReturn);
		Unindent();
		Line(_closeBrace);
		Line(_getPropReturnNull);
		Unindent();
		Line(_closeBrace);
		Line(_getTruthyMethodSignature);
		Line(_openBrace);
		Indent();
		Line(_truthyNull);
		Line(_truthyBool);
		Line(_truthyString);
		Line(_truthyInt);
		Line(_truthyLong);
		Line(_truthyDouble);
		Line(_truthyEnumerable);
		Line(_truthyDefault);
		Unindent();
		Line(_closeBrace);
		Line(_loopInfoRecordSignature);
		Line(_openBrace);
		Indent();
		Line(_loopInfoFirst);
		Line(_loopInfoLast);
		Unindent();
		Line(_closeBrace);
	}
	/// <summary>
	/// Emits the definition of the template context class and its properties to the output.
	/// </summary>
	private void EmitContextType() {
		Line(_templateContextClass);
		Line(_openBrace);
		Indent();
		Line(_templateContextTools);
		Line(_templateContextMessages);
		Line(_templateContextAddGen);
		Line(string.Empty);
		Line(_templateContextComment);
		Unindent();
		Line(_closeBrace);
	}
	private void Line(string s) {
		_sb.Append(_sc, _indent * 4);
		_sb.AppendLine(s);
	}
	private void Indent() => _indent++;
	private void Unindent() => _indent--;
}