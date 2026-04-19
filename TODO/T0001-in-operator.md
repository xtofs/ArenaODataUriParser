# T0001 – `in` operator

## OData ABNF

```abnf
inExpr = commonExpr RWS "in" RWS ( listExpr / commonExpr )
listExpr = OPEN BWS commonExpr *( BWS COMMA BWS commonExpr ) BWS CLOSE
```

## Work required

- Add `TokenKind.OperatorIn` and classify `in` during tokenisation.
- Add `OperatorKind.In`.
- Add `SyntaxKind.InExpression` (or reuse `BinaryExpression` with a list RHS node).
- Decide representation: separate `SyntaxKind.ListExpression` to hold N child nodes, or inline N+1 children under the `in` node.
- Parse at comparison level (same precedence as `eq`/`ne`/…).
- Add `InExpressionNode` (or `BinaryOperatorNode` variant) in the semantic layer.
- Extend `ArenaBinder` and `ArenaSerializer`.
- Tests: `name in ('a','b','c')`, `age in (1,2,3)`, `x in someOtherCollection`.
