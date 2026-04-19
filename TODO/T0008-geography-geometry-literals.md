# T0008 – Geography and geometry literals

## OData ABNF

```abnf
geographyLiteral = "geography'" fullCollectionLiteral "'"
                 / "geography'" fullLineStringLiteral "'"
                 / "geography'" fullMultiLineStringLiteral "'"
                 / "geography'" fullMultiPointLiteral "'"
                 / "geography'" fullMultiPolygonLiteral "'"
                 / "geography'" fullPointLiteral "'"
                 / "geography'" fullPolygonLiteral "'"
geometryLiteral  = "geometry'"  ... (same shapes)
```

The literals themselves are WKT (Well-Known Text) fragments embedded in single quotes.

## Work required

- The tokeniser already captures `geography'...'` / `geometry'...'` as `Literal` tokens via the prefixed-quoted heuristic.
- Add dedicated semantic nodes: `GeoLiteralNode(GeoKind kind, string wkt)` where `GeoKind` is an enum (`Geography`, `Geometry`).
- Optionally parse the WKT value into a structured form (Point, LineString, Polygon, …) – this may be out of scope for the arena parser layer; keep WKT as a string in the node.
- Update binder to route `geography'...'` / `geometry'...'` tokens to `GeoLiteralNode`.
- Update serializer to emit the canonical `geography'wkt'` form.
- Tests:
  - `geography'SRID=4326;Point(142.1 64.1)'`
  - `geometry'SRID=0;Polygon((0 0,1 0,1 1,0 0))'`
