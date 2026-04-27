using System.Text;
using ODataUriParser.Syntax;

namespace ODataUriParser.Tests;

public class TokenizerTests
{
    public static TheoryData<string, TokenKind[]> TokenKindCases => new()
        {
            { "name eq @p1", [TokenKind.Identifier, TokenKind.OperatorEq, TokenKind.Variable] },
            { "(price add tax) gt 10", [TokenKind.OpenParen, TokenKind.Identifier, TokenKind.OperatorAdd, TokenKind.Identifier, TokenKind.CloseParen, TokenKind.OperatorGt, TokenKind.Literal] },
            { "not isDeleted and enabled", [TokenKind.OperatorNot, TokenKind.Identifier, TokenKind.OperatorAnd, TokenKind.Identifier] },
            { "a and (b or x)", [TokenKind.Identifier, TokenKind.OperatorAnd, TokenKind.OpenParen, TokenKind.Identifier, TokenKind.OperatorOr, TokenKind.Identifier, TokenKind.CloseParen] },
            { "(name eq @p1) or (price gt 10)", [TokenKind.OpenParen, TokenKind.Identifier, TokenKind.OperatorEq, TokenKind.Variable, TokenKind.CloseParen, TokenKind.OperatorOr, TokenKind.OpenParen, TokenKind.Identifier, TokenKind.OperatorGt, TokenKind.Literal, TokenKind.CloseParen] },
        };

    [Theory]
    [MemberData(nameof(TokenKindCases))]
    public void Tokenize_ProducesExpectedTokenKinds(string expression, TokenKind[] expectedKinds)
    {
        var request = Encoding.UTF8.GetBytes(expression);

        // tokenization doesn't require a full arena, so we can just allocate a tokens array for testing.
        var tokens = new Token[Math.Max(4, request.Length)];
        var count = Tokenizer.Tokenize(request, tokens);

        Assert.Equal(expectedKinds.Length, count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(expectedKinds[i], tokens[i].Kind);
        }
    }

    public static TheoryData<string, int, int, string> LiteralSpanCases => new()
        {
            { "'foo'", 1, 3, "foo" },
            { "'a''b'", 1, 4, "a''b" },
        };

    [Theory]
    [MemberData(nameof(LiteralSpanCases))]
    public void Tokenize_StringLiteralToken_HasExpectedOffsetAndLength(string expression, int expectedOffset, int expectedLength, string expectedSlice)
    {

        var request = Encoding.UTF8.GetBytes(expression);
        var tokens = new Token[4];

        var count = Tokenizer.Tokenize(request, tokens);

        Assert.Equal(1, count);
        Assert.Equal(TokenKind.Literal, tokens[0].Kind);
        Assert.Equal(expectedOffset, tokens[0].Offset);
        Assert.Equal(expectedLength, tokens[0].Length);

        var actualSlice = Encoding.UTF8.GetString(request.AsSpan(tokens[0].Offset, tokens[0].Length));
        Assert.Equal(expectedSlice, actualSlice);
    }

    [Theory]
    [InlineData("null", TokenKind.Literal)]
    [InlineData("INF", TokenKind.Literal)]
    [InlineData("duration'P1DT2H3M4S'", TokenKind.Literal)]
    [InlineData("guid'01234567-89ab-cdef-0123-456789abcdef'", TokenKind.Literal)]
    public void Tokenize_LiteralLikeTokens_AreClassifiedAsLiteral(string expression, TokenKind expectedKind)
    {

        var request = Encoding.UTF8.GetBytes(expression);
        var tokens = new Token[Math.Max(4, request.Length)];

        var count = Tokenizer.Tokenize(request, tokens);

        Assert.Equal(1, count);
        Assert.Equal(expectedKind, tokens[0].Kind);
    }
}
