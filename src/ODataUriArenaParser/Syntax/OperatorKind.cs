namespace ODataUriParser.Syntax;

public enum OperatorKind : ushort
{
    Or = 1,
    And = 2,
    Equal = 3,
    NotEqual = 4,
    LessThan = 5,
    LessOrEqual = 6,
    GreaterThan = 7,
    GreaterOrEqual = 8,
    Add = 9,
    Subtract = 10,
    Multiply = 11,
    Divide = 12,
    Modulo = 13,
    Not = 14,
    Negate = 15
}
