namespace ODataUriArenaParser.Semantic;


public sealed class PropertyAccessNode(string name) : SemanticNode
{
    public string Name { get; } = name;

}
