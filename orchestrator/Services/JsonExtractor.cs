using System.Text.RegularExpressions;

namespace SddOrchestrator.Services;

public static class JsonExtractor
{
    private static readonly Regex FencedJson = new(
        @"```json\s*(?<body>\{[\s\S]*?\})\s*```",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string? ExtractFencedJson(string output)
    {
        var match = FencedJson.Match(output);
        if (match.Success) return match.Groups["body"].Value;

        var first = output.IndexOf('{');
        var last = output.LastIndexOf('}');
        return first >= 0 && last > first ? output[first..(last + 1)] : null;
    }
}
