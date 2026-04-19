namespace ODataUriArenaParser.Semantic;


public sealed class ConstantNode(string value) : SemanticNode
{
    public string Value { get; } = value;


}
