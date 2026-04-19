# T0007 – Named-parameter function calls

## OData ABNF

```abnf
functionExpr = [ namespace "." ] functionName functionParameters
functionParameters = OPEN BWS [ functionParameter *( BWS COMMA BWS functionParameter ) BWS ] CLOSE
functionParameter  = parameterName EQ ( parameterAlias / commonExpr )
parameterAlias     = AT odataIdentifier
```

## Work required

- Distinguish function calls (named params) from method calls (positional params, T0003).
  - Heuristic: if the first token inside `(` matches `identifier EQ`, treat as function call.
- Add `TokenKind.ParameterName` or reuse `Identifier` and detect `=` lookahead.
- Add `SyntaxKind.FunctionCallExpression`.
- Add `FunctionCallNode(string qualifiedFunctionName, IReadOnlyList<(string Name, SemanticNode Value)> parameters)` in the semantic layer.
- Extend `ArenaBinder` and `ArenaSerializer`.
- Tests:
  - `NS.MyFunc(x=1,y='hello')`
  - `NS.MyFunc(x=@myAlias)` (parameter alias)
  - Unqualified: `Func(a=true)`
