# T0004 – `cast` expression

## OData ABNF

```abnf
castExpr = "cast" OPEN BWS [ commonExpr BWS COMMA BWS ] optionallyQualifiedTypeName BWS CLOSE
```

## Work required

- `cast` shares the method-call surface syntactically but has special semantics (first arg optional, last arg is always a type name not a value expression).
- Tokenise `cast` as `TokenKind.KeywordCast` (distinct from ordinary method names to keep the parser branch clean).
- Add `SyntaxKind.CastExpression`.
- Parse in `ParsePrimaryExpression`: optional first `commonExpr` argument followed by a qualified-type-name token.
- Qualified type name tokenisation: `Edm.String`, `NS.MyType` – dot-separated identifiers form a single `TypeName` token (or use the existing `Identifier` token and validate post-parse).
- Add `CastNode(SemanticNode? source, string typeName)` in the semantic layer.
- Extend `ArenaBinder` and `ArenaSerializer`.
- Tests: `cast(amount,Edm.Decimal)`, `cast(related,NS.SpecialEntity)`.
