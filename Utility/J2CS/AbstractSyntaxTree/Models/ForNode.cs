namespace OLLM.Utility.J2CS.AbstractSyntaxTree.Models;
// For-loop node.
internal sealed class ForNode(string varName, string iterableExpr, List<Node> body) : Node {
	public string VarName { get; } = varName;
	public string IterableExpr { get; } = iterableExpr;
	public List<Node> Body { get; } = body;
}