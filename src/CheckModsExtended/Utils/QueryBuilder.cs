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
    /// <param name="key">The key.</param>
    /// <param name="value">The value to be escaped.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance for method chaining.</returns>
    public QueryBuilder Add(string key, string? value)
    {
        _parameters.Add(new KeyValuePair<string, string?>(key, value != null ? Uri.EscapeDataString(value) : null));
        return this;
    }

    /// <summary>
    /// Adds a key-value pair to the query string without escaping the value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="rawValue">The raw value.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance for method chaining.</returns>
    public QueryBuilder AddRaw(string key, string? rawValue)
    {
        _parameters.Add(new KeyValuePair<string, string?>(key, rawValue));
        return this;
    }

    /// <inheritdoc />
    /// <returns>The constructed query string.</returns>
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
