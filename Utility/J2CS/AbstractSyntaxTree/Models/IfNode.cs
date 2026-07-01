namespace OLLM.Utility.J2CS.AbstractSyntaxTree.Models;
/// <summary>
/// Condition node in the AST. (e.g.: if, else, etc.)
/// </summary>
internal sealed class IfNode(List<(string Cond, List<Node> Body)> branches, List<Node>? elseBody) : Node {
	public List<(string Cond, List<Node> Body)> Branches { get; } = branches;
	public List<Node>? ElseBody { get; } = elseBody;
}