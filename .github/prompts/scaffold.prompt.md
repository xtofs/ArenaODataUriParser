FULL LIFETIME FLOW – TEST-FIRST ARENA SYNTAX BINDING

GOAL
-----
Build a small C# library demonstrating request-scoped parsing using a rented memory arena (IMemoryOwner<byte>) to achieve:

1) Zero allocations during lexing + syntax tree construction
2) Arena-resident syntax representation (tokens + nodes)
3) Heap-resident semantic binding
4) Immediate arena disposal after binding without breaking semantics

ARCHITECTURE
------------
The parser must implement a 3-layer lifetime flow:

LAYER 1: Transport (ARENA)
    - Raw request buffer (UTF-8 bytes)
    - Token table (Span<Token>)

LAYER 2: Syntax (ARENA)
    - Syntax node table (Span<SyntaxNode>)
    - Child index table (Span<int>)
    - No references
    - No strings
    - Only offsets, lengths, indices

LAYER 3: Semantic (HEAP)
    - Materialized strings
    - Semantic nodes
    - CLR references
    - Must survive arena.Dispose()

DATA TYPES (ARENA-RESIDENT)
---------------------------
struct Token
{
    public TokenKind Kind;
    public int Offset;
    public int Length;
}

struct SyntaxNode
{
    public SyntaxKind Kind;
    public int FirstChild;
    public ushort ChildCount;
    public ushort Payload; // e.g. TokenIndex or Operator
}

REPRESENTATION
--------------
Input:  name eq 'foo'

TokenTable[] contains slices into request buffer.
SyntaxNodeTable[] contains nodes referencing:
    - token indices
    - child index slices in ChildTable[]

NO GC OBJECTS in these layers.

BINDING (HEAP)
--------------
Binder walks SyntaxNodeTable[] and TokenTable[] and MATERIALIZES:

new BinaryOperatorNode(
    new PropertyAccessNode("name"),
    new ConstantNode("foo"),
    OperatorKind.Equals);

After this, semantic graph must NOT depend on arena memory.

---------------------------
TEST-FIRST REQUIREMENTS
---------------------------
Create tests FIRST before implementation.

TEST 1: No allocations during Tokenization + Syntax Build
--------------------------------------------------------
Arrange:
    using IMemoryOwner<byte> arena =
        MemoryPool<byte>.Shared.Rent(65536);

    ReadOnlySpan<byte> input =
        Encoding.UTF8.GetBytes("name eq 'foo'");

Act:
    long before = GC.GetAllocatedBytesForCurrentThread();

    var syntax = Parse(arena, input);

    long after = GC.GetAllocatedBytesForCurrentThread();

Assert:
    after - before == 0

(No Gen0 allocations permitted before binding)


TEST 2: Binding Allocates Expected Heap Objects
-----------------------------------------------
Act:
    var semantic = Bind(syntax);

Assert:
    GC allocation > 0
    semantic.PropertyName == "name"
    semantic.Constant == "foo"


TEST 3: Arena Disposal Does Not Break Semantics
----------------------------------------------
Act:
    arena.Dispose();

Assert:
    semantic still usable
    semantic.PropertyName == "name"
    semantic.Constant == "foo"


IMPLEMENT Parse()
-----------------
Must produce:
    TokenTable (Span<Token>)
    SyntaxNodeTable (Span<SyntaxNode>)
    ChildIndexTable (Span<int>)

All allocated from arena memory only.

IMPLEMENT Bind()
----------------
Must:
    read offsets from TokenTable
    decode UTF8 slices
    create heap-resident semantic nodes

IMPLEMENTATION CONSTRAINTS
--------------------------
• Syntax layer must contain NO classes
• Syntax nodes must be pure unmanaged structs
• No strings may exist before Bind()
• Semantic layer MUST allocate real heap objects
• Arena disposal must invalidate syntax tables safely

SUCCESS CRITERIA
-----------------
✓ 0 allocations before Bind()
✓ >0 allocations during Bind()
✓ Semantic graph survives arena.Dispose()

END
