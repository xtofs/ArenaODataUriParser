# T0005 – `isof` expression

## OData ABNF

```abnf
isofExpr = "isof" OPEN BWS [ commonExpr BWS COMMA BWS ] optionallyQualifiedTypeName BWS CLOSE
```

## Work required

Essentially the same shape as `cast` (T0004) but with boolean result semantics.

- Tokenise `isof` as `TokenKind.KeywordIsof`.
- Add `SyntaxKind.IsofExpression`.
- Parse in `ParsePrimaryExpression` (same optional-first-arg pattern as `cast`).
- Add `IsofNode(SemanticNode? source, string typeName)` in the semantic layer.
- Extend `AranaBinder` and `ArenaSerializer`.
- Tests: `isof(NS.SpecialEntity)`, `isof(related,NS.SpecialEntity)`.

## Note

Consider sharing a helper (`ParseTypeTestOrCast`) between T0004 and T0005 to avoid duplication.
