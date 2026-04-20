using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ODataUriArenaParser.Syntax;

public static class ArenaParser
{

    public static int GetRequiredArenaSize(int inputLength)
    {
        if (inputLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputLength));
        }

        var (_, _, _, required) = GetArenaLayout(inputLength);
        return required;
    }


    public static int GetRequiredArenaSize(ReadOnlySpan<byte> input)
    {
        return GetRequiredArenaSize(input.Length);
    }


    public static IMemoryOwner<byte> RentArena(int inputLength, MemoryPool<byte>? pool = null)
    {
        return (pool ?? MemoryPool<byte>.Shared).Rent(GetRequiredArenaSize(inputLength));
    }


    public static IMemoryOwner<byte> RentArena(ReadOnlySpan<byte> input, MemoryPool<byte>? pool = null)
    {
        return RentArena(input.Length, pool);
    }

    public static ArenaSyntax Parse(IMemoryOwner<byte> arena, ReadOnlySpan<byte> input)
    {
        Span<byte> storage = arena.Memory.Span;

        var (maxTokenCount, maxNodeCount, maxChildCount, required) = GetArenaLayout(input.Length);

        if (storage.Length < required)
        {
            throw new ArgumentException("Arena is too small for parse output.", nameof(arena));
        }

        int cursor = 0;
        Span<byte> request = storage.Slice(cursor, input.Length);
        input.CopyTo(request);
        cursor += input.Length;

        cursor = Align(cursor, 4);
        Span<Token> tokenTable = MemoryMarshal.Cast<byte, Token>(
            storage.Slice(cursor, maxTokenCount * Unsafe.SizeOf<Token>()));
        cursor += maxTokenCount * Unsafe.SizeOf<Token>();

        cursor = Align(cursor, 4);
        Span<SyntaxNode> nodeTable = MemoryMarshal.Cast<byte, SyntaxNode>(
            storage.Slice(cursor, maxNodeCount * Unsafe.SizeOf<SyntaxNode>()));
        cursor += maxNodeCount * Unsafe.SizeOf<SyntaxNode>();

        cursor = Align(cursor, 4);
        Span<int> childTable = MemoryMarshal.Cast<byte, int>(
            storage.Slice(cursor, maxChildCount * Unsafe.SizeOf<int>()));

        int tokenCount = Tokenize(request, tokenTable);
        if (tokenCount == 0)
        {
            throw new InvalidOperationException("Expression is empty.");
        }

        int nodeCount = 0;
        int childCount = 0;
        int tokenIndex = 0;

        int rootNodeIndex = ParseOrExpression(
            request,
            tokenTable[..tokenCount],
            ref tokenIndex,
            nodeTable,
            ref nodeCount,
            childTable,
            ref childCount);

        if (tokenIndex != tokenCount)
        {
            throw new InvalidOperationException("Unexpected tokens at end of expression.");
        }

        return new ArenaSyntax(
            request,
            tokenTable[..tokenCount],
            nodeTable[..nodeCount],
            childTable[..childCount],
            rootNodeIndex);
    }

    public static ArenaSyntax ParseConstant(IMemoryOwner<byte> arena, ReadOnlySpan<byte> input)
    {
        return Parse(arena, input);
    }

    public static ArenaSyntax ParseProperty(IMemoryOwner<byte> arena, ReadOnlySpan<byte> input)
    {
        return Parse(arena, input);
    }

    private static int ParseOrExpression(
        ReadOnlySpan<byte> request,
        ReadOnlySpan<Token> tokens,
        ref int tokenIndex,
        Span<SyntaxNode> nodes,
        ref int nodeCount,
        Span<int> children,
        ref int childCount)
    {
        int left = ParseAndExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);

        while (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorOr))
        {
            int right = ParseAndExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);
            left = CreateBinaryNode(OperatorKind.Or, left, right, nodes, ref nodeCount, children, ref childCount);
        }

        return left;
    }

    private static int ParseAndExpression(
        ReadOnlySpan<byte> request,
        ReadOnlySpan<Token> tokens,
        ref int tokenIndex,
        Span<SyntaxNode> nodes,
        ref int nodeCount,
        Span<int> children,
        ref int childCount)
    {
        int left = ParseComparisonExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);

        while (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorAnd))
        {
            int right = ParseComparisonExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);
            left = CreateBinaryNode(OperatorKind.And, left, right, nodes, ref nodeCount, children, ref childCount);
        }

        return left;
    }

    private static int ParseComparisonExpression(
        ReadOnlySpan<byte> request,
        ReadOnlySpan<Token> tokens,
        ref int tokenIndex,
        Span<SyntaxNode> nodes,
        ref int nodeCount,
        Span<int> children,
        ref int childCount)
    {
        int left = ParseAdditiveExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);

        while (true)
        {
            OperatorKind op;
            if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorEq))
            {
                op = OperatorKind.Equal;
            }
            else if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorNe))
            {
                op = OperatorKind.NotEqual;
            }
            else if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorLt))
            {
                op = OperatorKind.LessThan;
            }
            else if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorLe))
            {
                op = OperatorKind.LessOrEqual;
            }
            else if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorGt))
            {
                op = OperatorKind.GreaterThan;
            }
            else if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorGe))
            {
                op = OperatorKind.GreaterOrEqual;
            }
            else
            {
                break;
            }

            int right = ParseAdditiveExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);
            left = CreateBinaryNode(op, left, right, nodes, ref nodeCount, children, ref childCount);
        }

        return left;
    }

    private static int ParseAdditiveExpression(
        ReadOnlySpan<byte> request,
        ReadOnlySpan<Token> tokens,
        ref int tokenIndex,
        Span<SyntaxNode> nodes,
        ref int nodeCount,
        Span<int> children,
        ref int childCount)
    {
        int left = ParseMultiplicativeExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);

        while (true)
        {
            OperatorKind op;
            if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorAdd))
            {
                op = OperatorKind.Add;
            }
            else if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorSub))
            {
                op = OperatorKind.Subtract;
            }
            else
            {
                break;
            }

            int right = ParseMultiplicativeExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);
            left = CreateBinaryNode(op, left, right, nodes, ref nodeCount, children, ref childCount);
        }

        return left;
    }

    private static int ParseMultiplicativeExpression(
        ReadOnlySpan<byte> request,
        ReadOnlySpan<Token> tokens,
        ref int tokenIndex,
        Span<SyntaxNode> nodes,
        ref int nodeCount,
        Span<int> children,
        ref int childCount)
    {
        int left = ParseUnaryExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);

        while (true)
        {
            OperatorKind op;
            if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorMul))
            {
                op = OperatorKind.Multiply;
            }
            else if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorDiv))
            {
                op = OperatorKind.Divide;
            }
            else if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorMod))
            {
                op = OperatorKind.Modulo;
            }
            else
            {
                break;
            }

            int right = ParseUnaryExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);
            left = CreateBinaryNode(op, left, right, nodes, ref nodeCount, children, ref childCount);
        }

        return left;
    }

    private static int ParseUnaryExpression(
        ReadOnlySpan<byte> request,
        ReadOnlySpan<Token> tokens,
        ref int tokenIndex,
        Span<SyntaxNode> nodes,
        ref int nodeCount,
        Span<int> children,
        ref int childCount)
    {
        if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorNot))
        {
            int operand = ParseUnaryExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);
            return CreateUnaryNode(OperatorKind.Not, operand, nodes, ref nodeCount, children, ref childCount);
        }

        if (TryConsume(tokens, ref tokenIndex, TokenKind.OperatorSub))
        {
            int operand = ParseUnaryExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);
            return CreateUnaryNode(OperatorKind.Negate, operand, nodes, ref nodeCount, children, ref childCount);
        }

        return ParsePrimaryExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);
    }

    private static int ParsePrimaryExpression(
        ReadOnlySpan<byte> request,
        ReadOnlySpan<Token> tokens,
        ref int tokenIndex,
        Span<SyntaxNode> nodes,
        ref int nodeCount,
        Span<int> children,
        ref int childCount)
    {
        if (TryConsume(tokens, ref tokenIndex, TokenKind.OpenParen))
        {
            int nested = ParseOrExpression(request, tokens, ref tokenIndex, nodes, ref nodeCount, children, ref childCount);
            if (!TryConsume(tokens, ref tokenIndex, TokenKind.CloseParen))
            {
                throw new InvalidOperationException("Missing closing parenthesis.");
            }

            return nested;
        }

        if (tokenIndex >= tokens.Length)
        {
            throw new InvalidOperationException("Unexpected end of expression.");
        }

        int currentTokenIndex = tokenIndex;
        Token token = tokens[currentTokenIndex];
        tokenIndex++;

        return token.Kind switch
        {
            TokenKind.Identifier => CreateLeafNode(SyntaxKind.PropertyAccess, currentTokenIndex, nodes, ref nodeCount),
            TokenKind.Variable => CreateLeafNode(SyntaxKind.VariableAccess, currentTokenIndex, nodes, ref nodeCount),
            TokenKind.Literal => CreateLeafNode(SyntaxKind.Constant, currentTokenIndex, nodes, ref nodeCount),
            _ => throw new InvalidOperationException($"Unexpected token kind in primary expression: {token.Kind}.")
        };
    }

    private static int CreateLeafNode(SyntaxKind kind, int payload, Span<SyntaxNode> nodes, ref int nodeCount)
    {
        int index = nodeCount;
        nodes[nodeCount++] = new SyntaxNode
        {
            Kind = kind,
            FirstChild = -1,
            ChildCount = 0,
            Payload = checked((ushort)payload)
        };

        return index;
    }

    private static int CreateUnaryNode(
        OperatorKind op,
        int child,
        Span<SyntaxNode> nodes,
        ref int nodeCount,
        Span<int> children,
        ref int childCount)
    {
        int childStart = childCount;
        children[childCount++] = child;

        int index = nodeCount;
        nodes[nodeCount++] = new SyntaxNode
        {
            Kind = SyntaxKind.UnaryExpression,
            FirstChild = childStart,
            ChildCount = 1,
            Payload = (ushort)op
        };

        return index;
    }

    private static int CreateBinaryNode(
        OperatorKind op,
        int left,
        int right,
        Span<SyntaxNode> nodes,
        ref int nodeCount,
        Span<int> children,
        ref int childCount)
    {
        int childStart = childCount;
        children[childCount++] = left;
        children[childCount++] = right;

        int index = nodeCount;
        nodes[nodeCount++] = new SyntaxNode
        {
            Kind = SyntaxKind.BinaryExpression,
            FirstChild = childStart,
            ChildCount = 2,
            Payload = (ushort)op
        };

        return index;
    }

    private static bool TryConsume(ReadOnlySpan<Token> tokens, ref int tokenIndex, TokenKind kind)
    {
        if (tokenIndex < tokens.Length && tokens[tokenIndex].Kind == kind)
        {
            tokenIndex++;
            return true;
        }

        return false;
    }

    private static int Tokenize(ReadOnlySpan<byte> request, Span<Token> tokens)
    {
        int i = 0;
        int count = 0;

        while (i < request.Length)
        {
            while (i < request.Length && request[i] == (byte)' ')
            {
                i++;
            }

            if (i >= request.Length)
            {
                break;
            }

            byte ch = request[i];
            if (ch == (byte)'(')
            {
                tokens[count++] = new Token { Kind = TokenKind.OpenParen, Offset = i, Length = 1 };
                i++;
                continue;
            }

            if (ch == (byte)')')
            {
                tokens[count++] = new Token { Kind = TokenKind.CloseParen, Offset = i, Length = 1 };
                i++;
                continue;
            }

            if (ch == (byte)'\'')
            {
                int start = i + 1;
                i++;
                while (i < request.Length)
                {
                    if (request[i] == (byte)'\'')
                    {
                        if (i + 1 < request.Length && request[i + 1] == (byte)'\'')
                        {
                            i += 2;
                            continue;
                        }

                        break;
                    }

                    i++;
                }

                int len = i - start;
                if (i < request.Length && request[i] == (byte)'\'')
                {
                    i++;
                }

                tokens[count++] = new Token
                {
                    Kind = TokenKind.Literal,
                    Offset = start,
                    Length = len
                };

                continue;
            }

            int tokenStart = i;
            while (i < request.Length && request[i] != (byte)' ' && request[i] != (byte)'(' && request[i] != (byte)')')
            {
                i++;
            }

            int tokenLength = i - tokenStart;
            ReadOnlySpan<byte> value = request.Slice(tokenStart, tokenLength);
            TokenKind kind = ClassifyToken(value);

            tokens[count++] = new Token
            {
                Kind = kind,
                Offset = tokenStart,
                Length = tokenLength
            };
        }

        return count;
    }

    private static TokenKind ClassifyToken(ReadOnlySpan<byte> value)
    {
        if (value.Length == 0)
        {
            throw new InvalidOperationException("Token cannot be empty.");
        }

        if (IsAscii(value, "or"))
        {
            return TokenKind.OperatorOr;
        }

        if (IsAscii(value, "and"))
        {
            return TokenKind.OperatorAnd;
        }

        if (IsAscii(value, "not"))
        {
            return TokenKind.OperatorNot;
        }

        if (IsAscii(value, "eq"))
        {
            return TokenKind.OperatorEq;
        }

        if (IsAscii(value, "ne"))
        {
            return TokenKind.OperatorNe;
        }

        if (IsAscii(value, "lt"))
        {
            return TokenKind.OperatorLt;
        }

        if (IsAscii(value, "le"))
        {
            return TokenKind.OperatorLe;
        }

        if (IsAscii(value, "gt"))
        {
            return TokenKind.OperatorGt;
        }

        if (IsAscii(value, "ge"))
        {
            return TokenKind.OperatorGe;
        }

        if (IsAscii(value, "add"))
        {
            return TokenKind.OperatorAdd;
        }

        if (IsAscii(value, "sub"))
        {
            return TokenKind.OperatorSub;
        }

        if (IsAscii(value, "mul"))
        {
            return TokenKind.OperatorMul;
        }

        if (IsAscii(value, "div"))
        {
            return TokenKind.OperatorDiv;
        }

        if (IsAscii(value, "mod"))
        {
            return TokenKind.OperatorMod;
        }

        if (value[0] == (byte)'@' || IsAscii(value, "$it") || IsAscii(value, "$this"))
        {
            return TokenKind.Variable;
        }

        if (IsLiteralToken(value))
        {
            return TokenKind.Literal;
        }

        return TokenKind.Identifier;
    }

    private static bool IsLiteralToken(ReadOnlySpan<byte> value)
    {
        if (IsAscii(value, "null") || IsAscii(value, "true") || IsAscii(value, "false"))
        {
            return true;
        }

        if (IsAscii(value, "NaN") || IsAscii(value, "INF") || IsAscii(value, "-INF"))
        {
            return true;
        }

        if (StartsWithAscii(value, "duration'") && EndsWith(value, (byte)'\''))
        {
            return true;
        }

        if (StartsWithAscii(value, "guid'") && EndsWith(value, (byte)'\''))
        {
            return true;
        }

        if (StartsWithAscii(value, "binary'") && EndsWith(value, (byte)'\''))
        {
            return true;
        }

        if (Contains(value, (byte)':') || Contains(value, (byte)'T'))
        {
            return true;
        }

        bool hasDigit = false;
        for (int i = 0; i < value.Length; i++)
        {
            byte ch = value[i];
            if (ch >= (byte)'0' && ch <= (byte)'9')
            {
                hasDigit = true;
                continue;
            }

            if (ch == (byte)'-' || ch == (byte)'+' || ch == (byte)'.' || ch == (byte)'e' || ch == (byte)'E')
            {
                continue;
            }

            return false;
        }

        return hasDigit;
    }

    private static bool IsAscii(ReadOnlySpan<byte> value, string text)
    {
        if (value.Length != text.Length)
        {
            return false;
        }

        for (int i = 0; i < text.Length; i++)
        {
            if (value[i] != (byte)text[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool StartsWithAscii(ReadOnlySpan<byte> value, string prefix)
    {
        if (value.Length < prefix.Length)
        {
            return false;
        }

        for (int i = 0; i < prefix.Length; i++)
        {
            if (value[i] != (byte)prefix[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool EndsWith(ReadOnlySpan<byte> value, byte last)
    {
        return value.Length > 0 && value[^1] == last;
    }

    private static bool Contains(ReadOnlySpan<byte> value, byte ch)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == ch)
            {
                return true;
            }
        }

        return false;
    }

    private static int Align(int value, int alignment)
    {
        int mask = alignment - 1;
        return (value + mask) & ~mask;
    }

    private static (int MaxTokenCount, int MaxNodeCount, int MaxChildCount, int RequiredBytes) GetArenaLayout(int inputLength)
    {
        int maxTokenCount = Math.Max(4, inputLength);
        int maxNodeCount = Math.Max(4, (maxTokenCount * 2) + 1);
        int maxChildCount = maxNodeCount * 2;

        int required = inputLength;
        required = Align(required, 4);
        required += maxTokenCount * Unsafe.SizeOf<Token>();
        required = Align(required, 4);
        required += maxNodeCount * Unsafe.SizeOf<SyntaxNode>();
        required = Align(required, 4);
        required += maxChildCount * Unsafe.SizeOf<int>();

        return (maxTokenCount, maxNodeCount, maxChildCount, required);
    }
}
