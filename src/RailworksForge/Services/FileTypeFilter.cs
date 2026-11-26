using System.Collections.Generic;

namespace RailworksForge.Services;

public record FileTypeFilter(string Name, IReadOnlyList<string> Patterns);
