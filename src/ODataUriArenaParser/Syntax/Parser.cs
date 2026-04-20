using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ODataUriParser.Syntax;

public static class Parser
{


    public static Syntax Parse(IMemoryOwner<byte> arena, ReadOnlySpan<byte> input)
    {
        // Parse pipeline overview:
        // 1) Partition one arena into request/token/node/child tables.
        // 2) Tokenize into tokenTable.
        // 3) Parse with precedence methods while threading shared mutable state
        //    (tokenIndex, nodeCount, childCount).
        // 4) Return compact slices as Syntax.
        Span<byte> storage = arena.Memory.Span;

        var (maxTokenCount, maxNodeCount, maxChildCount, required) = Arena.GetArenaLayout(input.Length);

        if (storage.Length < required)
        {
            throw new ArgumentException("Arena is too small for parse output.", nameof(arena));
        }

        int cursor = 0;
        Span<byte> request = storage.Slice(cursor, input.Length);
        input.CopyTo(request);
        cursor += input.Length;

        cursor = Arena.Align(cursor, 4);
        Span<Token> tokenTable = MemoryMarshal.Cast<byte, Token>(
            storage.Slice(cursor, maxTokenCount * Unsafe.SizeOf<Token>()));
        cursor += maxTokenCount * Unsafe.SizeOf<Token>();

        cursor = Arena.Align(cursor, 4);
        Span<SyntaxNode> nodeTable = MemoryMarshal.Cast<byte, SyntaxNode>(
            storage.Slice(cursor, maxNodeCount * Unsafe.SizeOf<SyntaxNode>()));
        cursor += maxNodeCount * Unsafe.SizeOf<SyntaxNode>();

        cursor = Arena.Align(cursor, 4);
        Span<int> childTable = MemoryMarshal.Cast<byte, int>(
            storage.Slice(cursor, maxChildCount * Unsafe.SizeOf<int>()));

        int tokenCount = Tokenizer.Tokenize(request, tokenTable);
        if (tokenCount == 0)
        {
            throw new InvalidOperationException("Expression is empty.");
        }


        var parseInput = new ParseInput(request, tokenTable[..tokenCount]);
        var parseState = new ParseState(nodeTable, childTable);

        int rootNodeIndex = ParseOrExpression(parseInput, ref parseState);

        if (parseState.TokenIndex != tokenCount)
        {
            throw new InvalidOperationException("Unexpected tokens at end of expression.");
        }

        return new Syntax(
            request,
            parseInput.Tokens,
            parseState.Nodes[..parseState.NodeCount],
            parseState.Children[..parseState.ChildCount],
            rootNodeIndex);
    }

    public static Syntax ParseConstant(IMemoryOwner<byte> arena, ReadOnlySpan<byte> input)
    {
        return Parse(arena, input);
    }

    public static Syntax ParseProperty(IMemoryOwner<byte> arena, ReadOnlySpan<byte> input)
    {
        return Parse(arena, input);
    }

    // /////////////////////////////////////////////////////////////////////////////////
    // Helper types and methods for parsing with shared mutable state.

    // ParseInput is a ref struct to ensure it stays on the stack and isn't accidentally captured by lambdas or async methods.
    // It holds the immutable input data for the parser, including the request buffer and the token table.
    private readonly ref struct ParseInput
    {
        public ParseInput(ReadOnlySpan<byte> request, ReadOnlySpan<Token> tokens)
        {
            Request = request;
            Tokens = tokens;
        }

        public ReadOnlySpan<byte> Request { get; }
        public ReadOnlySpan<Token> Tokens { get; }
    }

    // ParseState is a ref struct to ensure it stays on the stack and isn't accidentally captured by lambdas or async methods.
    // It holds the mutable state of the parser, including the current token index and the counts of nodes and children created so far.
    private ref struct ParseState
    {
        public ParseState(Span<SyntaxNode> nodes, Span<int> children)
        {
            Nodes = nodes;
            Children = children;
            TokenIndex = 0;
            NodeCount = 0;
            ChildCount = 0;
        }

        public Span<SyntaxNode> Nodes;
        public Span<int> Children;
        public int TokenIndex;
        public int NodeCount;
        public int ChildCount;
    }

    // Precedence level 1 (lowest): OR
    private static int ParseOrExpression(in ParseInput input, ref ParseState state)
    {
        int left = ParseAndExpression(input, ref state);

        while (TryConsume(input, ref state, TokenKind.OperatorOr))
        {
            int right = ParseAndExpression(input, ref state);
            left = CreateBinaryNode(OperatorKind.Or, left, right, ref state);
        }

        return left;
    }

    // Precedence level 2: AND
    private static int ParseAndExpression(in ParseInput input, ref ParseState state)
    {
        int left = ParseComparisonExpression(input, ref state);

        while (TryConsume(input, ref state, TokenKind.OperatorAnd))
        {
            int right = ParseComparisonExpression(input, ref state);
            left = CreateBinaryNode(OperatorKind.And, left, right, ref state);
        }

        return left;
    }

    // Precedence level 3: comparison operators (eq/ne/lt/le/gt/ge)
    private static int ParseComparisonExpression(in ParseInput input, ref ParseState state)
    {
        int left = ParseAdditiveExpression(input, ref state);

        while (true)
        {
            OperatorKind op;
            if (TryConsume(input, ref state, TokenKind.OperatorEq))
            {
                op = OperatorKind.Equal;
            }
            else if (TryConsume(input, ref state, TokenKind.OperatorNe))
            {
                op = OperatorKind.NotEqual;
            }
            else if (TryConsume(input, ref state, TokenKind.OperatorLt))
            {
                op = OperatorKind.LessThan;
            }
            else if (TryConsume(input, ref state, TokenKind.OperatorLe))
            {
                op = OperatorKind.LessOrEqual;
            }
            else if (TryConsume(input, ref state, TokenKind.OperatorGt))
            {
                op = OperatorKind.GreaterThan;
            }
            else if (TryConsume(input, ref state, TokenKind.OperatorGe))
            {
                op = OperatorKind.GreaterOrEqual;
            }
            else
            {
                break;
            }

            int right = ParseAdditiveExpression(input, ref state);
            left = CreateBinaryNode(op, left, right, ref state);
        }

        return left;
    }

    // Precedence level 4: additive operators (add/sub)
    private static int ParseAdditiveExpression(in ParseInput input, ref ParseState state)
    {
        int left = ParseMultiplicativeExpression(input, ref state);

        while (true)
        {
            OperatorKind op;
            if (TryConsume(input, ref state, TokenKind.OperatorAdd))
            {
                op = OperatorKind.Add;
            }
            else if (TryConsume(input, ref state, TokenKind.OperatorSub))
            {
                op = OperatorKind.Subtract;
            }
            else
            {
                break;
            }

            int right = ParseMultiplicativeExpression(input, ref state);
            left = CreateBinaryNode(op, left, right, ref state);
        }

        return left;
    }

    // Precedence level 5: multiplicative operators (mul/div/mod)
    private static int ParseMultiplicativeExpression(in ParseInput input, ref ParseState state)
    {
        int left = ParseUnaryExpression(input, ref state);

        while (true)
        {
            OperatorKind op;
            if (TryConsume(input, ref state, TokenKind.OperatorMul))
            {
                op = OperatorKind.Multiply;
            }
            else if (TryConsume(input, ref state, TokenKind.OperatorDiv))
            {
                op = OperatorKind.Divide;
            }
            else if (TryConsume(input, ref state, TokenKind.OperatorMod))
            {
                op = OperatorKind.Modulo;
            }
            else
            {
                break;
            }

            int right = ParseUnaryExpression(input, ref state);
            left = CreateBinaryNode(op, left, right, ref state);
        }

        return left;
    }

    // Precedence level 6 (highest before primary): unary operators (not/-)
    private static int ParseUnaryExpression(in ParseInput input, ref ParseState state)
    {
        if (TryConsume(input, ref state, TokenKind.OperatorNot))
        {
            int operand = ParseUnaryExpression(input, ref state);
            return CreateUnaryNode(OperatorKind.Not, operand, ref state);
        }

        if (TryConsume(input, ref state, TokenKind.OperatorSub))
        {
            int operand = ParseUnaryExpression(input, ref state);
            return CreateUnaryNode(OperatorKind.Negate, operand, ref state);
        }

        return ParsePrimaryExpression(input, ref state);
    }

    // Primary expressions: parenthesized expressions and leaf values.
    private static int ParsePrimaryExpression(in ParseInput input, ref ParseState state)
    {
        if (TryConsume(input, ref state, TokenKind.OpenParen))
        {
            int nested = ParseOrExpression(input, ref state);
            if (!TryConsume(input, ref state, TokenKind.CloseParen))
            {
                throw new InvalidOperationException("Missing closing parenthesis.");
            }

            return nested;
        }

        if (state.TokenIndex >= input.Tokens.Length)
        {
            throw new InvalidOperationException("Unexpected end of expression.");
        }

        int currentTokenIndex = state.TokenIndex;
        Token token = input.Tokens[currentTokenIndex];
        state.TokenIndex++;

        return token.Kind switch
        {
            TokenKind.Identifier => CreateLeafNode(SyntaxKind.PropertyAccess, currentTokenIndex, ref state),
            TokenKind.Variable => CreateLeafNode(SyntaxKind.VariableAccess, currentTokenIndex, ref state),
            TokenKind.Literal => CreateLeafNode(SyntaxKind.Constant, currentTokenIndex, ref state),
            _ => throw new InvalidOperationException($"Unexpected token kind in primary expression: {token.Kind}.")
        };
    }

    private static int CreateLeafNode(SyntaxKind kind, int payload, ref ParseState state)
    {
        // Leaf nodes keep token index in Payload and have no children table entry.
        int index = state.NodeCount;
        state.Nodes[state.NodeCount++] = new SyntaxNode
        {
            Kind = kind,
            FirstChild = -1,
            ChildCount = 0,
            Payload = checked((ushort)payload)
        };

        return index;
    }

    private static int CreateUnaryNode(OperatorKind op, int child, ref ParseState state)
    {
        // Unary/binary nodes append child indices into the shared children table.
        int childStart = state.ChildCount;
        state.Children[state.ChildCount++] = child;

        int index = state.NodeCount;
        state.Nodes[state.NodeCount++] = new SyntaxNode
        {
            Kind = SyntaxKind.UnaryExpression,
            FirstChild = childStart,
            ChildCount = 1,
            Payload = (ushort)op
        };

        return index;
    }

    private static int CreateBinaryNode(OperatorKind op, int left, int right, ref ParseState state)
    {
        int childStart = state.ChildCount;
        state.Children[state.ChildCount++] = left;
        state.Children[state.ChildCount++] = right;

        int index = state.NodeCount;
        state.Nodes[state.NodeCount++] = new SyntaxNode
        {
            Kind = SyntaxKind.BinaryExpression,
            FirstChild = childStart,
            ChildCount = 2,
            Payload = (ushort)op
        };

        return index;
    }

    private static bool TryConsume(in ParseInput input, ref ParseState state, TokenKind kind)
    {
        if (state.TokenIndex < input.Tokens.Length && input.Tokens[state.TokenIndex].Kind == kind)
        {
            state.TokenIndex++;
            return true;
        }

        return false;
    }

}

public static class Arena
{

    public static IMemoryOwner<byte> RentArena(ReadOnlySpan<byte> input, MemoryPool<byte>? pool = null)
    {
        return (pool ?? MemoryPool<byte>.Shared).Rent(GetRequiredArenaSize(input.Length));
    }

    internal static int Align(int value, int alignment)
    {
        int mask = alignment - 1;
        return (value + mask) & ~mask;
    }

    internal static int GetRequiredArenaSize(int inputLength)
    {
        if (inputLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputLength));
        }

        var (_, _, _, required) = GetArenaLayout(inputLength);
        return required;
    }

    internal static (int MaxTokenCount, int MaxNodeCount, int MaxChildCount, int RequiredBytes) GetArenaLayout(int inputLength)
    {
        // Keep arena sizing and alignment in one place so parser and callers stay consistent.
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
