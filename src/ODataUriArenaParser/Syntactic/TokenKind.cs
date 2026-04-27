namespace ODataUriParser.Syntax;


public enum TokenKind : byte
{
    Identifier = 1,
    Variable = 2,
    Literal = 3,
    OperatorOr = 4,
    OperatorAnd = 5,
    OperatorNot = 6,
    OperatorEq = 7,
    OperatorNe = 8,
    OperatorLt = 9,
    OperatorLe = 10,
    OperatorGt = 11,
    OperatorGe = 12,
    OperatorAdd = 13,
    OperatorSub = 14,
    OperatorMul = 15,
    OperatorDiv = 16,
    OperatorMod = 17,
    OpenParen = 18,
    CloseParen = 19
}
