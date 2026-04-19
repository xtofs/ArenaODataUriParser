# T0006 – Enum literal semantics

## OData ABNF

```abnf
enumLiteral     = [ namespace "." ] enumTypeName "'" enumValue "'"
enumValue       = singleEnumValue *( COMMA singleEnumValue )
singleEnumValue = enumerationMember / enumMemberValue
```

## Current state

The tokeniser already classifies `Namespace.Type'Member'` as a `Literal` token (prefixed-quoted heuristic). However the binder stores it as a plain `ConstantNode` with the raw string value.

## Work required

- Add `SyntaxKind.EnumLiteralExpression` (or keep as `Constant` but add a sub-kind / dedicated node).
- Preferred: add `EnumLiteralNode(string qualifiedTypeName, string[] memberValues)` in the semantic layer.
- Update binder to parse the raw token text and split `TypeName'Member1,Member2'` into `qualifiedTypeName` and `memberValues`.
- Update serializer to reconstruct the canonical form.
- Tests:
  - Single value: `Sales.Color'Red'`
  - Multi value: `NS.Flags'Read,Write'`
  - Bare (no namespace): `Color'Blue'`
