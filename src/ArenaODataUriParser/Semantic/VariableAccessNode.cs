namespace ODataUriParser.Semantic;

public sealed class VariableAccessNode(string name) : SemanticNode
{
    public string Name { get; } = name;
}
