namespace OLLM.Utility.J2CS.AbstractSyntaxTree.Models;
internal sealed class TextNode(string text) : Node {
	public string Text { get; } = text;
}