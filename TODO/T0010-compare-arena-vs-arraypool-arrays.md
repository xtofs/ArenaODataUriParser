# T0010 – Compare arena vs pooled separate arrays

## Context

The parser currently uses a single pooled arena layout for request bytes, token table, node table, and child table.

This TODO captures the hypothesis:

> Separate arrays + ArrayPool can get very close to arena performance, with clearer code in some designs.

## Goal

Evaluate whether the current arena approach is worth its complexity versus a design using separate pooled arrays.

## Variants to benchmark

1. Single pooled arena (current implementation).
2. Separate arrays allocated fresh per parse.
3. Separate arrays rented from `ArrayPool<T>` per parse.

## Measurements

- Throughput (ops/sec).
- Mean/median parse time.
- Allocated bytes per operation.
- Gen0/Gen1 collections over benchmark run.
- Optional: p95 latency if scenario tooling supports it.

## Maintainability comparison

- Lines of code and code duplication.
- Complexity of lifetime management/disposal.
- Ease of extending parser structures.
- Error-proneness (bounds/layout bugs).
- Test readability and required fixture/setup complexity.

## Exit criteria

- Benchmark report captured for representative expressions (small/medium/complex).
- Short decision note: keep arena, switch approach, or support both behind strategy/API.
