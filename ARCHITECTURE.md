# ArenaODataUriParser

This is a **high-performance, allocation-efficient OData URI `$filter` parser** written in C# targeting .NET 10. It parses OData filter expressions (e.g. `name eq 'foo' and active eq true`) into a semantic tree.

## Three-Stage Pipeline

| Stage | Input | Output | Key Files |
|-------|-------|--------|-----------|
| **1. Tokenization** | Raw UTF-8 string | Token spans | `Tokenizer.cs`, `Token.cs`, `TokenKind.cs` |
| **2. Parsing** | Token stream | Flat syntax tree | `Parser.cs`, `SyntaxNode.cs`, `SyntaxKind.cs` |
| **3. Binding** | Syntax tree | Object-graph semantic tree | `SyntaxBinder.cs`, `SemanticNode.cs` + subclasses |

## Key Design: Arena Allocation (`Arena.cs`)

The core innovation — a **single contiguous memory block** holds all tokens, syntax nodes, and child indices. Memory is rented from `MemoryPool<byte>.Shared`, sized based on input length, so there's **near-zero GC pressure** during parsing. `ref struct` types prevent data from escaping to the heap.

## Parser (`Parser.cs`)

A **recursive-descent precedence-climbing parser** with 6 precedence levels (`or` → `and` → comparisons → additive → multiplicative → unary → primary), supporting operators like `eq`, `ne`, `gt`, `lt`, `and`, `or`, `not`, arithmetic, and OData-specific features like `@variables` and property paths (`x.y`).

## Binding (`SyntaxBinder.cs`)

Walks the flat arena-backed syntax tree and produces a conventional **object-graph** of `SemanticNode` subclasses (`BinaryOperatorNode`, `UnaryOperatorNode`, `ConstantNode`, `PropertyAccessNode`, `VariableAccessNode`) suitable for downstream evaluation or translation.

## Supporting Code

- **`src/demo/`** — Demo program showing parsing + allocation measurement
- **`src/benchmarks/`** — BenchmarkDotNet benchmarks for parsing and binding
- **`tests/`** — xUnit tests for tokenizer and semantic parser
- **`TODO/`** — Design docs for planned features (e.g. `in` operator, `has`, method calls, casts)