# T0003 – Method call expressions

## OData ABNF (selected)

```abnf
methodCallExpr = methodName OPEN BWS [ commonExpr *( BWS COMMA BWS commonExpr ) BWS ] CLOSE

methodName = "contains" / "startswith" / "endswith"
           / "length"   / "indexof"    / "substring"
           / "tolower"  / "toupper"    / "trim"       / "concat"
           / "matchesPattern"
           / "year" / "month" / "day" / "hour" / "minute" / "second"
           / "fractionalseconds" / "totalseconds" / "date" / "time"
           / "totaloffsetminutes" / "now" / "mindatetime" / "maxdatetime"
           / "ceiling" / "floor" / "round" / "isof" / "cast"
           ; "isof" and "cast" have their own dedicated rule (see T0004/T0005)
```

## Work required

- Recognise method names during tokenisation (classify as `TokenKind.MethodName` or reuse `Identifier` with a flag/lookup).
- Add `SyntaxKind.MethodCallExpression`.
- Parse inside `ParsePrimaryExpression`: on `Identifier` followed by `(`, collect comma-separated `commonExpr` arguments until `)`.
- Arena sizing: each argument is a subtree; `maxNodeCount` formula may need revisiting for deeply nested calls.
- Add `MethodCallNode(string methodName, IReadOnlyList<SemanticNode> arguments)` in the semantic layer.
- Extend `ArenaBinder` and `ArenaSerializer`.
- Tests (one per arity category):
  - 0-arg: `now()`, `mindatetime()`
  - 1-arg: `length(name)`, `tolower(name)`, `year(birthday)`
  - 2-arg: `contains(name,'foo')`, `startswith(name,'A')`, `indexof(name,'x')`
  - 3-arg: `substring(name,1,3)`
