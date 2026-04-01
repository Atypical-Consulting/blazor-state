using System.ComponentModel;
using BlazorStatePlus.Abstractions;

namespace BlazorStatePlus.Generators;

/// <summary>
/// Fluent configuration builder for a single [Slice] field.
/// Used by the generated SliceInitContext to collect runtime configuration
/// before slices are created.
/// </summary>
public sealed class SliceBuilder<T>
{
    private object[]? _suffixParts;
    private string? _keyOverride;
    private T _defaultValue = default!;
    private Func<Task<T>>? _asyncFactory;

    public SliceBuilder<T> KeySuffix(params object[] parts)
    {
        _suffixParts = parts;
        return this;
    }

    public SliceBuilder<T> KeyOverride(string key)
    {
        _keyOverride = key;
        return this;
    }

    public SliceBuilder<T> DefaultValue(T value)
    {
        _defaultValue = value;
        return this;
    }

    public SliceBuilder<T> InitializeFrom(Func<Task<T>> factory)
    {
        _asyncFactory = factory;
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string ResolveKey(string baseKey)
    {
        if (_keyOverride is not null)
            return _keyOverride;

        if (_suffixParts is null or { Length: 0 })
            return baseKey;

        return baseKey + ":" + string.Join(":", _suffixParts);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public T GetDefaultValue() => _defaultValue;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool HasAsyncFactory => _asyncFactory is not null;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task InitializeAsync(IStateSlice<T> slice)
    {
        if (_asyncFactory is not null)
            await slice.InitializeIfNeededAsync(_asyncFactory);
    }
}
