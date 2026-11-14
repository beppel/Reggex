using System.Collections.Immutable;

namespace Reggex.Desktop.Snippets;

public sealed record RegexSnippetCategory(
    string Name,
    ImmutableArray<RegexSnippet> Snippets);

public sealed record RegexSnippet(string Label, string Text, string ToolTip = "");

public static class RegexSnippetCatalog
{
    public static ImmutableArray<RegexSnippetCategory> PatternCategories { get; } =
    [
        new("Character escapes",
        [
            new("Tab (\\t)", @"\t", "The tab character."),
            new("Carriage return (\\r)", @"\r", "The carriage return character."),
            new("Newline (\\n)", @"\n", "The newline character."),
            new("Space (\\x20)", @"\x20", "A literal space character."),
            new("Open parenthesis (\\()", @"\(", "A literal opening parenthesis.")
        ]),
        new("Character classes",
        [
            new("Any character (.)", ".", "Any character except a newline, unless Singleline is enabled."),
            new("Character in a list ([...])", "[§characters§]", "Any one character from the list."),
            new("Character not in a list ([^...])", "[^§characters§]", "Any character except those in the list."),
            new("Word character (\\w)", @"\w", "A letter, digit, or underscore."),
            new("Whitespace character (\\s)", @"\s", "A whitespace character."),
            new("Decimal digit (\\d)", @"\d", "A decimal digit."),
            new("Unicode character class (\\p{...})", @"\p{§name§}", "A character in the specified Unicode category.")
        ]),
        new("Zero-width assertions",
        [
            new("Start of line (^)", "^", "The start of the input, or of a line when Multiline is enabled."),
            new("End of line ($)", "$", "The end of the input, or of a line when Multiline is enabled."),
            new("Start of string (\\A)", @"\A", "The absolute start of the input."),
            new("End of string (\\z)", @"\z", "The absolute end of the input."),
            new("Word boundary (\\b)", @"\b", "A position at a word boundary."),
            new("Non-word boundary (\\B)", @"\B", "A position that is not a word boundary.")
        ]),
        new("Quantifiers",
        [
            new("Zero or one (?)", "?", "The preceding expression is optional."),
            new("Zero or more (*)", "*", "The preceding expression can repeat zero or more times."),
            new("One or more (+)", "+", "The preceding expression repeats at least once."),
            new("Exactly N ({N})", "{§N§}", "The preceding expression repeats exactly N times."),
            new("Between N and M ({N,M})", "{§N§,M}", "The preceding expression repeats between N and M times."),
            new("Zero or more, lazy (*?)", "*?", "The preceding expression repeats as few times as possible."),
            new("One or more, lazy (+?)", "+?", "The preceding expression repeats as few times as possible, at least once.")
        ]),
        new("Groups",
        [
            new("Capture ((...))", "(§expression§)", "Capture the matched substring."),
            new("Named capture", "(?<§name§>§expression§)", "Capture the expression under a group name."),
            new("Non-capturing group", "(?:§expression§)", "Group an expression without capturing it."),
            new("Positive lookahead", "(?=§expression§)", "Continue only when the expression matches ahead."),
            new("Negative lookahead", "(?!§expression§)", "Continue only when the expression does not match ahead."),
            new("Positive lookbehind", "(?<=§expression§)", "Continue only when the expression matches behind."),
            new("Negative lookbehind", "(?<!§expression§)", "Continue only when the expression does not match behind."),
            new("Atomic group", "(?>§expression§)", "Match the expression without backtracking into it."),
            new("Backreference to named group", @"\k<§name§>", "Match the text captured by the named group.")
        ]),
        new("Alternating constructs",
        [
            new("Either/or", "(?:§first§|§second§)", "Match either the first or the second expression."),
            new("Condition on named group", "(?(§name§)yes|no)", "Choose an expression depending on whether the named group matched.")
        ]),
        new("Numbers",
        [
            new("Positive integer", @"\d+", "A sequence of decimal digits."),
            new("Signed integer", @"[+-]?\d+", "An integer with an optional sign."),
            new("Floating-point number", @"[+-]?\d+(?:\.\d+)?", "A signed number with an optional decimal portion."),
            new("Exponential number", @"[+-]?\d+(?:\.\d+)?(?:E[+-]?\d+)?", "A number with an optional exponent."),
            new("Hexadecimal number", @"[0-9A-Fa-f]+", "A sequence of hexadecimal digits.")
        ]),
        new("Strings",
        [
            new("Alphabetic word", @"\b[A-Za-z]+\b", "A word containing only alphabetic characters."),
            new("Alphanumeric word", @"\b\w+\b", "A word containing letters, digits, or underscores."),
            new("Identifier", @"\b[A-Za-z_]\w*\b", "An identifier that starts with a letter or underscore."),
            new("Quoted string", """(?<quote>["']).*?\k<quote>""", "Text enclosed in matching single or double quotes.")
        ]),
        new("Dates and times",
        [
            new("U.S. date", @"(?:1[0-2]|0?[1-9])(?<sep>[-/])(?:3[01]|[12]\d|0?[1-9])\k<sep>(?:\d{4}|\d{2})", "A month/day/year date with matching separators; day and month ranges are checked."),
            new("European date", @"(?:3[01]|[12]\d|0?[1-9])(?<sep>[-/])(?:1[0-2]|0?[1-9])\k<sep>(?:\d{4}|\d{2})", "A day/month/year date with matching separators; day and month ranges are checked."),
            new("24-hour time", @"(?<!\d)(?:2[0-3]|[01]\d|\d):[0-5]\d(?!\d)", "A time in 24-hour HH:mm format.")
        ]),
        new("Miscellaneous",
        [
            new("Email address", @"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", "A common email address format."),
            new("IPv4 address", @"(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)", "An IPv4 address with each number constrained to 0 through 255."),
            new("GUID", @"\b(?:[0-9A-Fa-f]{32}|[0-9A-Fa-f]{8}-(?:[0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12})\b", "A GUID with or without dashes.")
        ])
    ];

    public static ImmutableArray<RegexSnippetCategory> ReplacementCategories { get; } =
    [
        new("Character escapes",
        [
            new("Tab", @"\t", "Insert the literal \\t sequence; .NET replacement strings do not interpret backslash escapes."),
            new("Carriage return", @"\r", "Insert the literal \\r sequence; .NET replacement strings do not interpret backslash escapes."),
            new("Newline", @"\n", "Insert the literal \\n sequence; .NET replacement strings do not interpret backslash escapes."),
            new("Escape", @"\e", "Insert the literal \\e sequence; .NET replacement strings do not interpret backslash escapes."),
            new("Vertical tab", @"\v", "Insert the literal \\v sequence; .NET replacement strings do not interpret backslash escapes."),
            new("Form-Feed", @"\f", "Insert the literal \\f sequence; .NET replacement strings do not interpret backslash escapes."),
            new("Bell alarm", @"\a", "Insert the literal \\a sequence; .NET replacement strings do not interpret backslash escapes."),
            new("ASCII character NN (octal)", @"\0§nn§", "Insert a literal octal escape sequence."),
            new("ASCII character NN (hex)", @"\x§nn§", "Insert a literal hexadecimal escape sequence."),
            new("Unicode character NNNN", @"\u§nnnn§", "Insert a literal Unicode escape sequence."),
            new("ASCII control character Ctrl-C", @"\cC", "Insert the literal control-character escape sequence."),
            new("Space", @"\x20", "Insert the literal hexadecimal space escape."),
            new("Open parenthesis", @"\(", "Insert the literal \\( sequence."),
            new("Close parenthesis", @"\)", "Insert the literal \\) sequence."),
            new("Open bracket", @"\[", "Insert the literal \\[ sequence."),
            new("Close bracket", @"\]", "Insert the literal \\] sequence.")
        ]),
        new("Substitutions",
        [
            new("Group N ($N)", "$§N§", "Substitute the substring captured by group N."),
            new("Named group (${name})", "${§name§}", "Substitute the last substring matched by a named group."),
            new("Entire match ($0)", "$0", "Substitute the entire match."),
            new("Last captured group ($+)", "$+", "Substitute the highest-numbered group that participated in the match."),
            new("Entire source string ($_)", "$_", "Substitute the entire source string."),
            new("Source string before match ($`)", "$`", "Substitute the portion of the source string before the match."),
            new("Source string after match ($')", "$'", "Substitute the portion of the source string after the match.")
        ])
    ];
}
