namespace OLLM.Utility.J2CS.AbstractSyntaxTree;
internal sealed class RootNode(List<Node> nodes) : Node {
	public List<Node> Nodes { get; } = nodes;
}