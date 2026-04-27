namespace ODataUriParser.Syntax;

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

public static class Arena
{

    public static IMemoryOwner<byte> RentArena(ReadOnlySpan<byte> input, MemoryPool<byte>? pool = null)
    {
        return (pool ?? MemoryPool<byte>.Shared).Rent(GetRequiredArenaSize(input.Length));
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

        int required = 0;
        required.Increment(inputLength);
        required.Increment<Token>(maxTokenCount);
        required.Increment<SyntaxNode>(maxNodeCount);
        required.Increment<int>(maxChildCount);

        return (maxTokenCount, maxNodeCount, maxChildCount, required);
    }

    internal static void CreateTables(IMemoryOwner<byte> arena, ReadOnlySpan<byte> input,
        out Span<byte> request,
        out Span<Token> tokenTable,
        out Span<SyntaxNode> nodeTable,
        out Span<int> childTable)
    {

        Span<byte> storage = arena.Memory.Span;

        var (maxTokenCount, maxNodeCount, maxChildCount, required) = Arena.GetArenaLayout(input.Length);

        if (storage.Length < required)
        {
            throw new ArgumentException("Arena is too small for parse output.", nameof(arena));
        }

        // create the individual tables as spans over the arena memory with correct alignment and sizing.
        int cursor = 0;

        request = storage.Slice(cursor, input.Length);
        cursor.Increment(input.Length, 4);

        // The token table is a span of Tokens that will be populated by the tokenizer.
        tokenTable = MemoryMarshal.Cast<byte, Token>(
        storage.Slice(cursor, maxTokenCount * Unsafe.SizeOf<Token>()));
        cursor.Increment<Token>(maxTokenCount);

        // The node table is a span of SyntaxNodes that will be populated by the parser.
        nodeTable = MemoryMarshal.Cast<byte, SyntaxNode>(
             storage.Slice(cursor, maxNodeCount * Unsafe.SizeOf<SyntaxNode>()));
        cursor.Increment<SyntaxNode>(maxNodeCount);

        childTable = MemoryMarshal.Cast<byte, int>(
            storage.Slice(cursor, maxChildCount * Unsafe.SizeOf<int>()));
        cursor.Increment<int>(maxChildCount);

    }

    private static int Increment<T>(this ref int cursor, int n, int alignment = 4)
    {
        return cursor.Increment(n * Unsafe.SizeOf<T>(), alignment);
    }

    private static int Increment(this ref int cursor, int amount, int alignment = 4)
    {
        cursor += amount;
        cursor = Align(cursor, 4);
        return cursor;


        static int Align(int value, int alignment)
        {
            int mask = alignment - 1;
            return (value + mask) & ~mask;
        }
    }
}
