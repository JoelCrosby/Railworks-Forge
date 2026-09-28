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
- `SerzControlChars`: strings containing NUL and other characters XML forbids,
  mixed with markup and line breaks. `serz64.exe` writes these as raw bytes, so
  its output is not well-formed XML and is compared using AngleSharp instead.

Other tests compare parsed XML, allowing serialization whitespace and empty-element
syntax to differ while requiring element names, attributes, and values to match.
They require neither a game installation nor Wine.

`SerzDuplicateClose.bin` is a synthetic malformed blueprint with two consecutive
closing records for `AutomaticJunctionEntity`. It reproduces the structural
error in A_PK10/Pack_Commun's `Levier2_Levier3 D.bin` without including game data.
The native converter emits invalid XML for that structure, so there is no golden
XML fixture. Tests require a diagnostic containing the filename and element names
and verify that rolling-stock discovery skips the unrelated track-rules blueprint.
