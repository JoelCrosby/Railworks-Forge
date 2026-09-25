# Internal SERZ performance

File conversion now sends the binary decoder's `XmlWriter` output directly to a
file. The previous app path wrote XML into memory, parsed that XML into an
AngleSharp DOM, serialized the entire DOM back into a string, and wrote that
string to disk. Callers that need an `IDocument` can still use `ToXml()` or
`SerzInternal.Convert(inputPath)`.

`SerzInternal.Convert(inputPath, outputPath, cancellationToken)` writes a temporary
file alongside the destination and replaces the destination after successful
conversion. Invalid input or cancellation leaves an existing destination intact.
Cancellation is checked between binary chunks and before publishing the output.
The app runs this synchronous conversion on a worker thread.

## Measurements

Measured locally on 2026-09-25 with .NET 10 Release builds,
`DOTNET_TieredCompilation=0`, and five iterations with warmed file caches.
Conversion times include input reads and output writes, with no application
cache hits. Wine/SERZ measurements include process launch through CliWrap; its
first invocation was excluded to avoid counting Wine initialization. File writes
use normal OS buffering, not forced disk synchronization. These are local
measurements, not guaranteed timings on other systems.

| Sample | BIN bytes | Previous DOM round trip | Direct file conversion | serz64.exe via Wine |
| --- | ---: | ---: | ---: | ---: |
| Repository Scenario.bin | 74,322 | 27.58 ms | 4.85 ms | 91.41 ms |
| Scenery tile | 306,288 | 165.37 ms | 20.96 ms | 132.66 ms |
| MixMap tile | 98,534 | 4.70 ms | 1.81 ms | 87.68 ms |
| Tracks.bin | 7,927,219 | 6,877.39 ms | 748.61 ms | 1,740.46 ms |

Instrumenting the previous implementation separated input reads, binary decoding
with XML emission, DOM parsing, DOM serialization, and file writing. For the
Tracks sample, median input reading took 1.74 ms, binary decoding/XML emission
573.05 ms, and DOM parsing 5,375.81 ms. DOM construction was the dominant cost.
The medians of individual stages need not sum to the median total.

Managed allocations per conversion fell from 85.00 to 2.11 MiB for the scenery
tile and from 3,079.74 to 51.53 MiB for Tracks. These are cumulative allocations,
not peak memory usage. The input BIN is still loaded into a byte array; the much
larger XML no longer needs a DOM or a complete in-memory string for file output.

Game sample paths, relative to RailWorks:

- `Content/Routes/fc8df6e1-dc30-4efc-92a6-2fe065b39826/Scenery/+000026+000014.bin`
- `Content/Routes/fc8df6e1-dc30-4efc-92a6-2fe065b39826/MixMap/+000031+000014.bin`
- `Content/Routes/a7037b8a-e8cf-4963-847f-58469a87c0d5/Networks/Tracks.bin`

The local `Content/SDBCache.bin` was excluded: it contains NUL characters that
the existing internal reader rejects, and `serz64.exe` emits those NULs into
invalid XML. That pre-existing compatibility issue is separate from this
performance change.

## Repeating the benchmark

From the repository root on Linux:

```bash
DOTNET_TieredCompilation=0 WINEDEBUG=-all \
SERZ_EXE_PATH=/cache/SteamLibrary/steamapps/common/RailWorks/serz64.exe \
dotnet run --project tools/SerzBenchmark -c Release -- \
  src/RailworksForge.UnitTests/Resources/Scenario.bin \
  /path/to/Tracks.bin
```

The tool reports median read time, DOM round-trip time, direct file conversion
time, and managed allocations over five iterations. `SERZ_EXE_PATH` is optional;
when present it also measures the game executable, using Wine on Linux. All
outputs go into a temporary directory that is removed afterward. Source BIN
files and application settings are not modified. The DOM round-trip measurement
reconstructs the old app path using the retained document API.
