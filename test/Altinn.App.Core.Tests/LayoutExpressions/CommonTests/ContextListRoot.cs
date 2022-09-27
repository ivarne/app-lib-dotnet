#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Expressions;

namespace Altinn.App.Core.Tests.LayoutExpressions;

public class ContextListRoot
{
    [JsonIgnore]
    public string? Filename { get; set; }

    [JsonIgnore]
    public string? FullPath { get; set; }

    [JsonIgnore]
    public string? Folder { get; set; }

    [JsonIgnore]
    public string? RawJson { get; set; }

    [JsonIgnore]
    public Exception? ParsingException { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("layouts")]
    public ComponentModel ComponentModel { get; set; } = default!;

    [JsonPropertyName("dataModel")]
    public JsonElement? DataModel { get; set; }

    [JsonPropertyName("expectedContexts")]
    public List<ComponentContextForTestSpec> Expected { get; set; } = default!;

    public override string ToString()
    {
        return $"{Name} ({Folder}/{Filename})";
    }
}