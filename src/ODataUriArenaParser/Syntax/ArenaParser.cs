using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ODataUriArenaParser.Syntax;


public static class ArenaParser
{
    public static ArenaSyntax Parse(IMemoryOwner<byte> arena, ReadOnlySpan<byte> input)
    {
        Span<byte> storage = arena.Memory.Span;

        int maxTokenCount = Math.Max(4, input.Length);
        int maxNodeCount = 3;
        int maxChildCount = 2;

        int required = input.Length;
        required = Align(required, 4);
        required += maxTokenCount * Unsafe.SizeOf<Token>();
        required = Align(required, 4);
        required += maxNodeCount * Unsafe.SizeOf<SyntaxNode>();
        required = Align(required, 4);
        required += maxChildCount * Unsafe.SizeOf<int>();

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
        if (tokenCount != 3)
        {
            throw new InvalidOperationException("Expected exactly 3 tokens.");
        }

        if (tokenTable[0].Kind != TokenKind.Identifier ||
            tokenTable[1].Kind != TokenKind.OperatorEq ||
            tokenTable[2].Kind != TokenKind.StringLiteral)
        {
            throw new InvalidOperationException("Expected: identifier eq 'literal'.");
        }

        childTable[0] = 1;
        childTable[1] = 2;

        nodeTable[0] = new SyntaxNode
        {
            Kind = SyntaxKind.BinaryExpression,
            FirstChild = 0,
            ChildCount = 2,
            Payload = (ushort)OperatorKind.Equal
        };
        nodeTable[1] = new SyntaxNode
        {
            Kind = SyntaxKind.PropertyAccess,
            FirstChild = -1,
            ChildCount = 0,
            Payload = 0
        };
        nodeTable[2] = new SyntaxNode
        {
            Kind = SyntaxKind.Constant,
            FirstChild = -1,
            ChildCount = 0,
            Payload = 2
        };

        return new ArenaSyntax(
            request,
            tokenTable.Slice(0, tokenCount),
            nodeTable.Slice(0, 3),
            childTable.Slice(0, 2),
            0);
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

            if (request[i] == (byte)'\'')
            {
                int start = i + 1;
                i++;
                while (i < request.Length && request[i] != (byte)'\'')
                {
                    i++;
                }

                int len = i - start;
                if (i < request.Length && request[i] == (byte)'\'')
                {
                    i++;
                }

                tokens[count++] = new Token
                {
                    Kind = TokenKind.StringLiteral,
                    Offset = start,
                    Length = len
                };

                continue;
            }

            int tokenStart = i;
            while (i < request.Length && request[i] != (byte)' ')
            {
                i++;
            }

            int tokenLength = i - tokenStart;
            TokenKind kind = IsEq(request.Slice(tokenStart, tokenLength))
                ? TokenKind.OperatorEq
                : TokenKind.Identifier;

            tokens[count++] = new Token
            {
                Kind = kind,
                Offset = tokenStart,
                Length = tokenLength
            };
        }

        return count;
    }

    private static bool IsEq(ReadOnlySpan<byte> value)
    {
        return value.Length == 2 && value[0] == (byte)'e' && value[1] == (byte)'q';
    }

    private static int Align(int value, int alignment)
    {
        int mask = alignment - 1;
        return (value + mask) & ~mask;
    }


    public static ArenaSyntax ParseConstant(IMemoryOwner<byte> arena, ReadOnlySpan<byte> input)
    {
        Span<byte> storage = arena.Memory.Span;

        int maxTokenCount = 1;
        int maxNodeCount = 1;

        int required = input.Length;
        required = Align(required, 4);
        required += maxTokenCount * Unsafe.SizeOf<Token>();
        required = Align(required, 4);
        required += maxNodeCount * Unsafe.SizeOf<SyntaxNode>();

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

        int tokenCount = TokenizeIdentifierOrStringLiteral(request, tokenTable);
        if (tokenCount != 1)
        {
            throw new InvalidOperationException("Expected exactly 1 token for constant expression.");
        }

        if (tokenTable[0].Kind != TokenKind.StringLiteral)
        {
            throw new InvalidOperationException("Expected string literal token.");
        }

        nodeTable[0] = new SyntaxNode
        {
            Kind = SyntaxKind.Constant,
            FirstChild = -1,
            ChildCount = 0,
            Payload = 0
        };

        return new ArenaSyntax(
            request,
            tokenTable.Slice(0, tokenCount),
            nodeTable.Slice(0, 1),
            ReadOnlySpan<int>.Empty,
            0);
    }

    public static ArenaSyntax ParseProperty(IMemoryOwner<byte> arena, ReadOnlySpan<byte> input)
    {
        Span<byte> storage = arena.Memory.Span;

        int maxTokenCount = 1;
        int maxNodeCount = 1;

        int required = input.Length;
        required = Align(required, 4);
        required += maxTokenCount * Unsafe.SizeOf<Token>();
        required = Align(required, 4);
        required += maxNodeCount * Unsafe.SizeOf<SyntaxNode>();

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

        int tokenCount = TokenizeIdentifierOrStringLiteral(request, tokenTable);
        if (tokenCount != 1)
        {
            throw new InvalidOperationException("Expected exactly 1 token for property expression.");
        }

        if (tokenTable[0].Kind != TokenKind.Identifier)
        {
            throw new InvalidOperationException("Expected identifier token.");
        }

        nodeTable[0] = new SyntaxNode
        {
            Kind = SyntaxKind.PropertyAccess,
            FirstChild = -1,
            ChildCount = 0,
            Payload = 0
        };

        return new ArenaSyntax(
            request,
            tokenTable.Slice(0, tokenCount),
            nodeTable.Slice(0, 1),
            ReadOnlySpan<int>.Empty,
            0);
    }

    private static int TokenizeIdentifierOrStringLiteral(ReadOnlySpan<byte> request, Span<Token> tokens)
    {
        int i = 0;
        int count = 0;

        while (i < request.Length && request[i] == (byte)' ')
        {
            i++;
        }

        if (i >= request.Length)
        {
            return count;
        }

        if (request[i] == (byte)'\'')
        {
            int start = i + 1;
            i++;
            while (i < request.Length && request[i] != (byte)'\'')
            {
                i++;
            }

            int len = i - start;
            if (i < request.Length && request[i] == (byte)'\'')
            {
                i++;
            }

            tokens[count++] = new Token
            {
                Kind = TokenKind.StringLiteral,
                Offset = start,
                Length = len
            };
        }
        else
        {
            int tokenStart = i;
            while (i < request.Length && request[i] != (byte)' ')
            {
                i++;
            }

            int tokenLength = i - tokenStart;
            tokens[count++] = new Token
            {
                Kind = TokenKind.Identifier,
                Offset = tokenStart,
                Length = tokenLength
            };
        }

        return count;
    }
}
