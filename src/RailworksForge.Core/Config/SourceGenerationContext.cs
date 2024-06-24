using System.Text.Json.Serialization;

using RailworksForge.Core.Models;

namespace RailworksForge.Core.Config;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SavedConsist))]
[JsonSerializable(typeof(ConfigurationOptions))]
internal partial class SourceGenerationContext : JsonSerializerContext;
