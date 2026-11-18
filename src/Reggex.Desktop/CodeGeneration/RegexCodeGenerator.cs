using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Reggex.Desktop.CodeGeneration;

public enum RegexCodeSampleKind
{
    Find,
    Replace,
    Split,
}

public sealed record RegexCodeGenerationOptions(
    string Pattern,
    string Replacement,
    RegexOptions RegexOptions,
    RegexCodeSampleKind SampleKind,
    bool UseInstanceMethod,
    bool AssumeUsingDirectives,
    bool UseVerbatimStrings,
    bool GenerateLoop,
    bool UseGeneratedRegex = false);

public static class RegexCodeGenerator
{
    public static string Generate(RegexCodeGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var regexType = options.AssumeUsingDirectives
            ? "Regex"
            : "System.Text.RegularExpressions.Regex";
        var regexOptions = FormatRegexOptions(options.RegexOptions, options.AssumeUsingDirectives);
        var pattern = FormatString(options.Pattern, options.UseVerbatimStrings);
        var input = FormatString("text", verbatim: false);
        var replacement = FormatString(options.Replacement, verbatim: false);
        var builder = new StringBuilder();

        if (options.UseGeneratedRegex)
        {
            AppendGeneratedRegex(builder, options, regexType, pattern, input, replacement, regexOptions);
            return builder.ToString().TrimEnd();
        }

        switch (options.SampleKind)
        {
            case RegexCodeSampleKind.Find:
                AppendFind(builder, options, regexType, pattern, input, regexOptions);
                break;
            case RegexCodeSampleKind.Replace:
                AppendReplace(builder, options, regexType, pattern, input, replacement, regexOptions);
                break;
            case RegexCodeSampleKind.Split:
                AppendSplit(builder, options, regexType, pattern, input, regexOptions);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), options.SampleKind, "Unknown sample kind.");
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendGeneratedRegex(
        StringBuilder builder,
        RegexCodeGenerationOptions options,
        string regexType,
        string pattern,
        string input,
        string replacement,
        string regexOptions)
    {
        var attributeType = options.AssumeUsingDirectives
            ? "GeneratedRegex"
            : "System.Text.RegularExpressions.GeneratedRegex";

        builder.AppendLine("partial class Program");
        builder.AppendLine("{");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    [{attributeType}({pattern}, {regexOptions})]");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    private static partial {regexType} MyRegex();");
        builder.AppendLine();
        builder.AppendLine("    private static void Main()");
        builder.AppendLine("    {");

        switch (options.SampleKind)
        {
            case RegexCodeSampleKind.Find:
                builder.AppendLine(CultureInfo.InvariantCulture, $"        var mc = MyRegex().Matches({input});");
                if (options.GenerateLoop)
                {
                    builder.AppendLine("        foreach (var ma in mc)");
                    builder.AppendLine("        {");
                    builder.AppendLine("        }");
                }
                break;
            case RegexCodeSampleKind.Replace:
                builder.AppendLine(CultureInfo.InvariantCulture, $"        var result = MyRegex().Replace({input}, {replacement});");
                break;
            case RegexCodeSampleKind.Split:
                builder.AppendLine(CultureInfo.InvariantCulture, $"        var parts = MyRegex().Split({input});");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), options.SampleKind, "Unknown sample kind.");
        }

        builder.AppendLine("    }");
        builder.Append('}');
    }

    private static void AppendFind(
        StringBuilder builder,
        RegexCodeGenerationOptions options,
        string regexType,
        string pattern,
        string input,
        string regexOptions)
    {
        if (options.UseInstanceMethod)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"var re = new {regexType}({pattern}, {regexOptions});");
            builder.AppendLine(CultureInfo.InvariantCulture, $"var mc = re.Matches({input});");
        }
        else
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"var mc = {regexType}.Matches({input}, {pattern}, {regexOptions});");
        }

        if (options.GenerateLoop)
        {
            builder.AppendLine("foreach (var ma in mc)");
            builder.AppendLine("{");
            builder.AppendLine("}");
        }
    }

    private static void AppendReplace(
        StringBuilder builder,
        RegexCodeGenerationOptions options,
        string regexType,
        string pattern,
        string input,
        string replacement,
        string regexOptions)
    {
        if (options.UseInstanceMethod)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"var re = new {regexType}({pattern}, {regexOptions});");
            builder.Append(CultureInfo.InvariantCulture, $"var result = re.Replace({input}, {replacement});");
        }
        else
        {
            builder.Append(CultureInfo.InvariantCulture, $"var result = {regexType}.Replace({input}, {pattern}, {replacement}, {regexOptions});");
        }
    }

    private static void AppendSplit(
        StringBuilder builder,
        RegexCodeGenerationOptions options,
        string regexType,
        string pattern,
        string input,
        string regexOptions)
    {
        if (options.UseInstanceMethod)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"var re = new {regexType}({pattern}, {regexOptions});");
            builder.Append(CultureInfo.InvariantCulture, $"var parts = re.Split({input});");
        }
        else
        {
            builder.Append(CultureInfo.InvariantCulture, $"var parts = {regexType}.Split({input}, {pattern}, {regexOptions});");
        }
    }

    private static string FormatRegexOptions(RegexOptions options, bool assumeUsingDirectives)
    {
        var type = assumeUsingDirectives ? "RegexOptions" : "System.Text.RegularExpressions.RegexOptions";
        var names = new List<string>();
        AddIfSet(RegexOptions.IgnoreCase, "IgnoreCase");
        AddIfSet(RegexOptions.Multiline, "Multiline");
        AddIfSet(RegexOptions.ExplicitCapture, "ExplicitCapture");
        AddIfSet(RegexOptions.Compiled, "Compiled");
        AddIfSet(RegexOptions.Singleline, "Singleline");
        AddIfSet(RegexOptions.IgnorePatternWhitespace, "IgnorePatternWhitespace");
        AddIfSet(RegexOptions.RightToLeft, "RightToLeft");
        AddIfSet(RegexOptions.CultureInvariant, "CultureInvariant");
        AddIfSet(RegexOptions.NonBacktracking, "NonBacktracking");

        return names.Count == 0
            ? $"{type}.None"
            : string.Join(" | ", names.Select(name => $"{type}.{name}"));

        void AddIfSet(RegexOptions flag, string name)
        {
            if ((options & flag) == flag)
            {
                names.Add(name);
            }
        }
    }

    private static string FormatString(string value, bool verbatim)
    {
        if (verbatim)
        {
            return $"@\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\0' => "\\0",
                '\a' => "\\a",
                '\b' => "\\b",
                '\f' => "\\f",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\v' => "\\v",
                _ when char.IsControl(character) => $"\\u{(int)character:X4}",
                _ => character.ToString(),
            });
        }

        builder.Append('"');
        return builder.ToString();
    }
}
