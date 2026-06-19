// CAP.Avalonia/Controls/Canvas/ComponentPreview/GdsPreviewKey.cs
using System.Security.Cryptography;
using System.Text;

namespace CAP.Avalonia.Controls.Canvas.ComponentPreview;

/// <summary>
/// Resolution-independent render identity for a GDS preview: the geometry depends
/// only on the Nazca module/function/parameters, not on the display size. Used as
/// both the in-memory and on-disk cache key.
/// </summary>
public readonly record struct GdsPreviewKey(string? Module, string? Function, string? Parameters)
{
    /// <summary>Bump to invalidate every cached entry (format or render-semantics change).</summary>
    public const int FormatVersion = 1;

    /// <summary>True when there is a function to render (built-in components have none).</summary>
    public bool IsRenderable => !string.IsNullOrWhiteSpace(Function);

    /// <summary>Stable filesystem-safe hash, prefixed with the format version.</summary>
    public string Hash()
    {
        var material = $"{Module}{Function}{Parameters}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        var hex = Convert.ToHexString(bytes, 0, 12).ToLowerInvariant(); // 24 hex chars
        return $"v{FormatVersion}-{hex}";
    }
}
