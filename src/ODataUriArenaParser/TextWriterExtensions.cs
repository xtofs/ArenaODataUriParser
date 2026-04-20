using System.Text;
using System.Buffers;
using System.IO;

namespace ODataUriArenaParser.Binding;

static class TextWriterExtensions
{
    private static readonly int MAX_CHAR_COUNT_TO_USE_STACK = 256;

    // allocatoin optimized writing of UTF-8 byte spans to TextWriter, using a stack buffer for small inputs and renting from the pool for larger inputs.
    public static void Write(this TextWriter writer, ReadOnlySpan<byte> bytes, Encoding sourceEncoding)
    {
        int maxChars = sourceEncoding.GetMaxCharCount(bytes.Length);
        if (maxChars <= MAX_CHAR_COUNT_TO_USE_STACK)
        {
            Span<char> stackBuffer = stackalloc char[256];
            int charsWritten = sourceEncoding.GetChars(bytes, stackBuffer);
            writer.Write(stackBuffer[..charsWritten]);
            return;
        }

        // For larger inputs, rent a buffer from the pool to avoid large stack allocations.
        // Note that renting a buffer incurs some overhead, so it's only beneficial for larger inputs.
        char[] rented = Pool.Rent(maxChars);
        try
        {
            int charsWritten = sourceEncoding.GetChars(bytes, rented);
            writer.Write(rented.AsSpan(0, charsWritten));
        }
        finally
        {
            Pool.Return(rented);
        }
    }

    private static readonly ArrayPool<char> Pool = ArrayPool<char>.Shared;

}
