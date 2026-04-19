# T0009 – JSON array and object literal forms

## OData ABNF (OData 4.01 filter-in-query extension)

```abnf
; Used in $filter for collection-valued parameters
jsonArrayOrObject = begin-array
                    [ value *( value-separator value ) ]
                    end-array
                  / begin-object
                    [ member *( value-separator member ) ]
                    end-object
```

These appear as `[...]` or `{...}` literals in filter expressions, primarily as arguments to bound functions.

## Work required

- Add `TokenKind.JsonLiteral` (or lex the entire JSON fragment as a single opaque literal token using a mini-scanner for balanced `[`/`{`).
- Decide scope: treat JSON blobs as opaque string literals in the arena (zero-cost) vs. full structural parse (separate task).
- Recommended approach: lex as a single token (scan for balanced brackets, record span), classify as `TokenKind.Literal` with a sub-kind flag, store the raw bytes in the arena buffer.
- Add `JsonLiteralNode(string rawJson)` in the semantic layer.
- Update binder and serializer.
- Tests:
  - Array: `$filter=NS.Func(ids=[1,2,3])`
  - Object: `$filter=NS.Func(opt={"key":"val"})`
