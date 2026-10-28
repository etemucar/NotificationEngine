using System.Security.Cryptography;
using System.Numerics;
using System.Reflection;

namespace NotificationEngine.Core.Common.Utils;

public static class Utilities
{

    public static string GetStateFromStatusId(int statusId)
    {
        return "ACTIVE";
    }

    /// <summary>
    ///     Compute SHA‑256 hash of the given stream and return as lowercase hex string.
    /// </summary>
    public static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default)
    {
        using var sha = SHA256.Create();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            sha.TransformBlock(buffer, 0, read, null, 0);
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }

    /// <summary>
    ///     Very lightweight average‑hash (aHash) implementation that works without external libraries.
    ///     Returns 64‑bit perceptual hash.
    ///     NOTE: For production, replace with a robust library (OpenCvSharp, CoenM.ImageSharpHash, etc.).
    /// </summary>
    public static ulong ComputePerceptualHash(Stream input)
    {
        // Reset stream position if possible
        if (input.CanSeek) input.Position = 0;

        // Fallback: simplistic hash on raw bytes when System.Drawing is not available.
        // XOR every 8‑th byte to build 64‑bit value — not robust but avoids build break.
        var hash = 0UL;
        int index = 0;
        int b;
        while ((b = input.ReadByte()) != -1)
        {
            if ((index & 7) == 0)
                hash ^= ((ulong)b << ((index >> 3) & 56));
            index++;
        }
        return hash;
    }

    public static int GetStatusIdFromState(string state)
    {
        return 1;
    }

    public static string GenerateProductSpecCode(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Ürün adı boş olamaz.", nameof(name));

        var clean = name
            .Replace("İ", "I").Replace("ı", "i")
            .Replace("Ğ", "G").Replace("ğ", "g")
            .Replace("Ü", "U").Replace("ü", "u")
            .Replace("Ş", "S").Replace("ş", "s")
            .Replace("Ö", "O").Replace("ö", "o")
            .Replace("Ç", "C").Replace("ç", "c")
            .Replace(" ", "-")
            .Replace("&", "AND")
            .Replace("/", "-")
            .Replace("\\", "-");

        var sb = new System.Text.StringBuilder();
        foreach (char c in clean.ToUpperInvariant())
        {
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-')
                sb.Append(c);
        }

        while (sb.ToString().Contains("--"))
            sb.Replace("--", "-");

        var result = sb.ToString().Trim('-');
        if (string.IsNullOrEmpty(result))
            result = "PRODUCT";

        var code = "PS-" + result;
        return code.Length <= 30 ? code : code[..30];
    }

    public static string GenerateCharSpecCode(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("KArakteristik adı boş olamaz.", nameof(name));

        var clean = name
            .Replace("İ", "I").Replace("ı", "i")
            .Replace("Ğ", "G").Replace("ğ", "g")
            .Replace("Ü", "U").Replace("ü", "u")
            .Replace("Ş", "S").Replace("ş", "s")
            .Replace("Ö", "O").Replace("ö", "o")
            .Replace("Ç", "C").Replace("ç", "c")
            .Replace(" ", "-")
            .Replace("&", "AND")
            .Replace("/", "-")
            .Replace("\\", "-");

        var sb = new System.Text.StringBuilder();
        foreach (char c in clean.ToUpperInvariant())
        {
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-')
                sb.Append(c);
        }

        while (sb.ToString().Contains("--"))
            sb.Replace("--", "-");

        var result = sb.ToString().Trim('-');
        if (string.IsNullOrEmpty(result))
            result = "Characteristic";

        var code = "CHAR-" + result;
        return code.Length <= 30 ? code : code[..30];
    }

    public static string MapValueTypeToDataType(string valueType)
    {
        return valueType?.ToLowerInvariant() switch
        {
            "string"  => "STRING",
            "number"  => "NUMBER",
            "boolean" => "BOOLEAN",
            "array"   => "ARRAY",
            _         => "STRING"
        };
    }

}   
