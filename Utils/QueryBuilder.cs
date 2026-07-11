using System;
using System.Collections.Generic;
using System.Linq;

namespace CheckModsExtended.Utils;

/// <summary>
/// A utility for building URI query strings safely.
/// </summary>
public sealed class QueryBuilder
{
    private readonly List<KeyValuePair<string, string?>> _parameters = new();

    /// <summary>
    /// Adds a key-value pair to the query string, automatically escaping the value.
    /// </summary>
    public QueryBuilder Add(string key, string? value)
    {
        _parameters.Add(new KeyValuePair<string, string?>(key, value != null ? Uri.EscapeDataString(value) : null));
        return this;
    }

    /// <summary>
    /// Adds a key-value pair to the query string without escaping the value.
    /// </summary>
    public QueryBuilder AddRaw(string key, string? rawValue)
    {
        _parameters.Add(new KeyValuePair<string, string?>(key, rawValue));
        return this;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (_parameters.Count == 0)
        {
            return string.Empty;
        }

        var pairs = _parameters.Select(p => p.Value != null ? $"{p.Key}={p.Value}" : p.Key);
        return "?" + string.Join("&", pairs);
    }
}
