using System.Globalization;
using Reggex.Desktop.Converters;

namespace Reggex.Desktop.Tests;

public class InvisibleCharactersConverterTests
{
    private readonly InvisibleCharactersConverter _converter = new();

    [Fact]
    public void ConvertKeepsOriginalTextWhenDisplayIsDisabled()
    {
        const string text = " a\t\r\n";

        var result = _converter.Convert([text, false], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(text, result);
    }

    [Fact]
    public void ConvertMarksSpacesAndControlCharactersWhenDisplayIsEnabled()
    {
        const string text = "a \t\r\n\0\u0001\u007F\u0085";

        var result = _converter.Convert([text, true], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("a␣⇥␍⏎\n␀␁␡\\u0085", result);
    }
}
