namespace OLLM.Utility.J2CS.AbstractSyntaxTree.Models;
internal sealed class OutputNode(string expr) : Node {
	public string Expr { get; } = expr;
}