using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;

namespace Reggex.Desktop.Converters;

public sealed class InvisibleCharactersConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not string text)
        {
            return string.Empty;
        }

        return values[1] is true ? MakeCharactersVisible(text) : text;
    }

    private static string MakeCharactersVisible(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            builder.Append(character switch
            {
                ' ' => "␣",
                '\t' => "⇥",
                '\r' => "␍",
                '\n' => "⏎\n",
                '\0' => "␀",
                '\u007F' => "␡",
                >= '\u0001' and <= '\u001F' => char.ConvertFromUtf32(0x2400 + character),
                >= '\u0080' and <= '\u009F' => $"\\u{(int)character:X4}",
                _ => character.ToString()
            });
        }

        return builder.ToString();
    }
}
