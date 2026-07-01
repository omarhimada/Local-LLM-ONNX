namespace OLLM.Utility.J2CS.AbstractSyntaxTree.Models;
// 'set' node. 
internal sealed class SetNode(string name, string expr) : Node {
	public string Name { get; } = name;
	public string Expr { get; } = expr;
}