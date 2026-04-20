namespace ODataUriArenaParser.Syntax;

internal static class ArenaTokenizer
{

    // and keep tokenizer logic in a dedicated type for parser navigability.
    private readonly ref struct TokenizeInput(ReadOnlySpan<byte> request)
    {
        public ReadOnlySpan<byte> Request { get; } = request;
    }

    private ref struct TokenizeState(Span<Token> tokens)
    {
        public Span<Token> Tokens { get; } = tokens;
        public int Position { get; set; }
        public int Count { get; set; }
    }

    internal static int Tokenize(ReadOnlySpan<byte> request, Span<Token> tokens)
    {
        var input = new TokenizeInput(request);
        var state = new TokenizeState(tokens)
        {
            Position = 0,
            Count = 0
        };

        while (state.Position < input.Request.Length)
        {
            while (state.Position < input.Request.Length && input.Request[state.Position] == (byte)' ')
            {
                state.Position++;
            }

            if (state.Position >= input.Request.Length)
            {
                break;
            }

            byte ch = input.Request[state.Position];
            if (ch == (byte)'(')
            {
                state.Tokens[state.Count++] = new Token { Kind = TokenKind.OpenParen, Offset = state.Position, Length = 1 };
                state.Position++;
                continue;
            }

            if (ch == (byte)')')
            {
                state.Tokens[state.Count++] = new Token { Kind = TokenKind.CloseParen, Offset = state.Position, Length = 1 };
                state.Position++;
                continue;
            }

            if (ch == (byte)'\'')
            {
                int start = state.Position + 1;
                state.Position++;
                while (state.Position < input.Request.Length)
                {
                    if (input.Request[state.Position] == (byte)'\'')
                    {
                        if (state.Position + 1 < input.Request.Length && input.Request[state.Position + 1] == (byte)'\'')
                        {
                            state.Position += 2;
                            continue;
                        }

                        break;
                    }

                    state.Position++;
                }

                int len = state.Position - start;
                if (state.Position < input.Request.Length && input.Request[state.Position] == (byte)'\'')
                {
                    state.Position++;
                }

                state.Tokens[state.Count++] = new Token
                {
                    Kind = TokenKind.Literal,
                    Offset = start,
                    Length = len
                };

                continue;
            }

            int tokenStart = state.Position;
            while (state.Position < input.Request.Length && input.Request[state.Position] != (byte)' ' && input.Request[state.Position] != (byte)'(' && input.Request[state.Position] != (byte)')')
            {
                state.Position++;
            }

            int tokenLength = state.Position - tokenStart;
            ReadOnlySpan<byte> value = input.Request.Slice(tokenStart, tokenLength);
            TokenKind kind = ClassifyToken(value);

            state.Tokens[state.Count++] = new Token
            {
                Kind = kind,
                Offset = tokenStart,
                Length = tokenLength
            };
        }

        return state.Count;
    }

    private static TokenKind ClassifyToken(ReadOnlySpan<byte> value)
    {
        // Classification order matters: operators/variables/literals first, identifier fallback last.
        if (value.Length == 0)
        {
            throw new InvalidOperationException("Token cannot be empty.");
        }

        if (EqualsUtf8(value, "or"u8))
        {
            return TokenKind.OperatorOr;
        }

        if (EqualsUtf8(value, "and"u8))
        {
            return TokenKind.OperatorAnd;
        }

        if (EqualsUtf8(value, "not"u8))
        {
            return TokenKind.OperatorNot;
        }

        if (EqualsUtf8(value, "eq"u8))
        {
            return TokenKind.OperatorEq;
        }

        if (EqualsUtf8(value, "ne"u8))
        {
            return TokenKind.OperatorNe;
        }

        if (EqualsUtf8(value, "lt"u8))
        {
            return TokenKind.OperatorLt;
        }

        if (EqualsUtf8(value, "le"u8))
        {
            return TokenKind.OperatorLe;
        }

        if (EqualsUtf8(value, "gt"u8))
        {
            return TokenKind.OperatorGt;
        }

        if (EqualsUtf8(value, "ge"u8))
        {
            return TokenKind.OperatorGe;
        }

        if (EqualsUtf8(value, "add"u8))
        {
            return TokenKind.OperatorAdd;
        }

        if (EqualsUtf8(value, "sub"u8))
        {
            return TokenKind.OperatorSub;
        }

        if (EqualsUtf8(value, "mul"u8))
        {
            return TokenKind.OperatorMul;
        }

        if (EqualsUtf8(value, "div"u8))
        {
            return TokenKind.OperatorDiv;
        }

        if (EqualsUtf8(value, "mod"u8))
        {
            return TokenKind.OperatorMod;
        }

        if (value[0] == (byte)'@' || EqualsUtf8(value, "$it"u8) || EqualsUtf8(value, "$this"u8))
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
        if (EqualsUtf8(value, "null"u8) || EqualsUtf8(value, "true"u8) || EqualsUtf8(value, "false"u8))
        {
            return true;
        }

        if (EqualsUtf8(value, "NaN"u8) || EqualsUtf8(value, "INF"u8) || EqualsUtf8(value, "-INF"u8))
        {
            return true;
        }

        if (StartsWithUtf8(value, "duration'"u8) && EndsWith(value, (byte)'\''))
        {
            return true;
        }

        if (StartsWithUtf8(value, "guid'"u8) && EndsWith(value, (byte)'\''))
        {
            return true;
        }

        if (StartsWithUtf8(value, "binary'"u8) && EndsWith(value, (byte)'\''))
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

    private static bool EqualsUtf8(ReadOnlySpan<byte> value, ReadOnlySpan<byte> text)
    {
        // SequenceEqual is a highly optimized comparison aware of cache lines and using SIMD where possible
        return value.SequenceEqual(text);
    }

    private static bool StartsWithUtf8(ReadOnlySpan<byte> value, ReadOnlySpan<byte> prefix)
    {
        // SequenceEqual is faster than StartsWith for small spans, and since our tokens are typically short, this is a good choice. If we had longer tokens or prefixes, we might want to optimize this further.
        return value.Length >= prefix.Length && value[..prefix.Length].SequenceEqual(prefix);
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
}
