# T0002 – `has` operator

## OData ABNF

```abnf
hasExpr = commonExpr RWS "has" RWS enumLiteral
enumLiteral = [ namespace "." ] enumTypeName "'" enumValue "'"
enumValue  = singleEnumValue *( COMMA singleEnumValue )
```

## Work required

- Add `TokenKind.OperatorHas` and classify `has` during tokenisation.
- Add `OperatorKind.Has`.
- Parse at comparison level (same precedence as `in`/`eq`/…).
- Enum literal token: already covered by the prefixed-quoted literal heuristic (`namespace.Type'member'`) but verify the tokeniser handles multi-segment namespaces correctly.
- Add `HasExpressionNode` in the semantic layer (LHS expression, RHS `EnumLiteralNode`).
- Extend `ArenaBinder` and `ArenaSerializer`.
- Tests: `style has Sales.Color'Yellow'`, `status has NS.Flags'Active,Pending'`.
