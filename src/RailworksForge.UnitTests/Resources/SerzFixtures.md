# SERZ compatibility fixtures

The `Serz*.bin` files and their matching `.bin.xml` files were generated with
`serz64.exe` from the local RailWorks installation on 2026-09-25. The XML files
are the executable's output after decoding the binaries, rather than the XML
originally used to create them.

- `SerzTypes`: signed and unsigned integer widths, floats, and booleans.
- `SerzNodes`: Unicode, escaped text, blobs, and a nil node followed by a sibling.
- `SerzCache`: 300 distinct value names followed in reverse order, exercising
  reuse and rollover of the 255-entry chunk cache.
- `SerzFeatures`: empty parents, signed IDs/references, nested names, vectors,
  and blob grouping across lines.
- `SerzFloats`: scientific notation, negative zero, infinity, and NaN.

Tests compare parsed XML, allowing serialization whitespace and empty-element
syntax to differ while requiring element names, attributes, and values to match.
They require neither a game installation nor Wine.
