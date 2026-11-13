using System.Text;

namespace ERP.Core.Services;

public static class CnpjNormalizer
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character is >= '0' and <= '9') digits.Append(character);
            else if (!char.IsWhiteSpace(character) && character is not '.' and not '/' and not '-') return null;
        }

        var normalized = digits.ToString();
        if (normalized.Length != 14 || !normalized.Any(c => c != normalized[0])) return null;
        if (CheckDigit(normalized[..12], 5) != normalized[12] - '0' ||
            CheckDigit(normalized[..13], 6) != normalized[13] - '0') return null;
        return normalized;
    }

    private static int CheckDigit(string value, int offset)
    {
        var sum = 0;
        var weight = offset;
        for (var index = 0; index < value.Length; index++)
        {
            sum += (value[index] - '0') * weight--;
            if (weight == 1) weight = 9;
        }
        var digit = (sum % 11) < 2 ? 0 : 11 - (sum % 11);
        return digit;
    }
}
