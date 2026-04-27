# Arena ODataUriParser 

## Overview

ODataUriArenaParser is an allocation-efficient parser for OData URI filter expressions. The parser is designed to minimize allocations and maximize throughput by using a single arena-allocated memory buffer for all parsing stages. The approach is inspired by compiler and query engine design, focusing on:

- **Arena allocation**: All tokens, syntax nodes, and child indices are allocated in a single contiguous memory block, reducing GC pressure and improving cache locality.
- **Single-pass parsing**: The parser tokenizes and builds the syntax tree in one pass, using spans and ref structs to avoid heap allocations.
- **Separation of concerns**: Syntactic parsing and semantic binding are cleanly separated f.

## Demo Program Results

The demo program (`src/demo/Program.cs`) showcases the parser's capabilities by running three example inputs of increasing complexity and showing the Allocation statistics for parsing and binding to the semantic tree.

**Results:**
- The parser consistently shows very low allocation counts for all stages, even for complex expressions.
- The arena-based approach ensures that allocations scale linearly with input size and are reclaimed immediately after parsing.
- The output trees demonstrate correct parsing and semantic binding for a variety of OData filter constructs.

## Usage

- See `src/demo/Program.cs` for example usage and output.
- The parser and supporting types are in `src/ODataUriArenaParser/Syntactic/`.

## Requirements
- .NET 10.0 or later

## License
MIT License
