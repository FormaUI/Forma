namespace Forma.UI.Features.Composer.Sanitization;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>Allowlist sanitizer covering the markup the built-in plugins produce, with no external dependency.</summary>
public sealed partial class DefaultHtmlSanitizer : IComposerHtmlSanitizer
{
    /// <inheritdoc />
    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return SanitizeTags(StripDangerousBlocks(html));
    }

    // Removes the elements whose text content is itself dangerous, which dropping the tags alone would leave behind.
    private static string StripDangerousBlocks(string input)
    {
        var cleaned = ScriptBlockRegex().Replace(input, string.Empty);
        cleaned = StyleBlockRegex().Replace(cleaned, string.Empty);
        cleaned = IframeBlockRegex().Replace(cleaned, string.Empty);
        cleaned = ObjectBlockRegex().Replace(cleaned, string.Empty);
        cleaned = EmbedTagRegex().Replace(cleaned, string.Empty);
        cleaned = SvgBlockRegex().Replace(cleaned, string.Empty);
        return HtmlCommentRegex().Replace(cleaned, string.Empty);
    }

    private static string SanitizeTags(string html)
    {
        var output = new StringBuilder(html.Length);
        var index = 0;

        while (index < html.Length)
        {
            var open = html.IndexOf('<', index, StringComparison.Ordinal);

            if (open < 0)
            {
                output.Append(html, index, html.Length - index);
                break;
            }

            output.Append(html, index, open - index);

            if (TryReadTag(html, open, out var tag, out var afterTag))
            {
                AppendTag(output, tag);
                index = afterTag;
            }
            else
            {
                output.Append('<');
                index = open + 1;
            }
        }

        return output.ToString();
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    private static bool TryReadTag(string html, int start, out HtmlTag tag, out int afterTag)
    {
        tag = default;
        afterTag = start;

        var position = start + 1;

        if (position >= html.Length)
        {
            return false;
        }

        var isClosing = html[position] == '/';

        if (isClosing)
        {
            position++;
        }

        var nameStart = position;

        while (position < html.Length && (char.IsAsciiLetterOrDigit(html[position]) || html[position] == '-'))
        {
            position++;
        }

        if (position == nameStart)
        {
            return false;
        }

        var name = html[nameStart..position].ToLowerInvariant();
        var attributesStart = position;
        var quote = '\0';

        // A '>' inside a quoted value does not end the tag, so the scan tracks the quote it is in.
        while (position < html.Length)
        {
            var current = html[position];

            if (quote != '\0')
            {
                if (current == quote)
                {
                    quote = '\0';
                }
            }
            else if (current is '"' or '\'')
            {
                quote = current;
            }
            else if (current == '>')
            {
                break;
            }

            position++;
        }

        if (position >= html.Length)
        {
            return false;
        }

        var attributes = html[attributesStart..position].Trim();
        var isSelfClosing = attributes.Length > 0 && attributes[^1] == '/';

        if (isSelfClosing)
        {
            attributes = attributes[..^1].TrimEnd();
        }

        tag = new HtmlTag(isClosing, name, attributes, isSelfClosing);
        afterTag = position + 1;
        return true;
    }

    private static void AppendTag(StringBuilder output, HtmlTag tag)
    {
        if (!HtmlAllowList.Tags.Contains(tag.Name))
        {
            return;
        }

        var isVoid = HtmlAllowList.VoidTags.Contains(tag.Name);

        if (tag.IsClosing)
        {
            if (!isVoid)
            {
                output.Append("</").Append(tag.Name).Append('>');
            }

            return;
        }

        var attributes = SanitizeAttributes(tag.Name, tag.Attributes);

        output.Append('<').Append(tag.Name);

        if (attributes.Length > 0)
        {
            output.Append(' ').Append(attributes);
        }

        output.Append(tag.IsSelfClosing || isVoid ? " />" : ">");
    }

    private static string SanitizeAttributes(string tagName, string rawAttributes)
    {
        if (string.IsNullOrWhiteSpace(rawAttributes))
        {
            return string.Empty;
        }

        var tagSpecific = HtmlAllowList.TagAttributes.GetValueOrDefault(tagName);
        var result = new StringBuilder();

        foreach (Match match in AttributeRegex().Matches(rawAttributes))
        {
            var name = match.Groups[1].Value;

            // An event handler is script by definition, whatever the element.
            if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isAllowed = name.StartsWith("data-", StringComparison.OrdinalIgnoreCase)
                            || HtmlAllowList.GlobalAttributes.Contains(name)
                            || (tagSpecific is not null && tagSpecific.Contains(name));

            if (!isAllowed)
            {
                continue;
            }

            var value = match.Groups[2].Success ? match.Groups[2].Value
                : match.Groups[3].Success ? match.Groups[3].Value
                : match.Groups[4].Value;

            if (HtmlAllowList.UrlAttributes.Contains(name))
            {
                if (!IsSafeUrl(value))
                {
                    continue;
                }
            }
            else if (name.Equals("style", StringComparison.OrdinalIgnoreCase))
            {
                value = SanitizeStyle(value);

                if (value.Length == 0)
                {
                    continue;
                }
            }

            if (result.Length > 0)
            {
                result.Append(' ');
            }

            result.Append(name).Append("=\"").Append(EscapeAttributeValue(value)).Append('"');
        }

        return result.ToString();
    }

    private static bool IsSafeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();

        if (trimmed[0] is '#' or '/' || trimmed.StartsWith("./", StringComparison.Ordinal) || trimmed.StartsWith("../", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var scheme in HtmlAllowList.UrlSchemes)
        {
            if (trimmed.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var prefix in HtmlAllowList.DataUrlPrefixes)
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string SanitizeStyle(string style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return string.Empty;
        }

        var declarations = style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var safe = new List<string>(declarations.Length);

        foreach (var declaration in declarations)
        {
            if (IsDeniedDeclaration(declaration))
            {
                continue;
            }

            var colon = declaration.IndexOf(':', StringComparison.Ordinal);

            if (colon <= 0)
            {
                continue;
            }

            var property = declaration[..colon].Trim();
            var value = declaration[(colon + 1)..].Trim();

            if (HtmlAllowList.StyleProperties.Contains(property) || property.StartsWith("--forma-", StringComparison.OrdinalIgnoreCase))
            {
                safe.Add($"{property}: {value}");
            }
        }

        return string.Join("; ", safe);
    }

    private static bool IsDeniedDeclaration(string declaration)
    {
        foreach (var denied in HtmlAllowList.StyleDenyList)
        {
            if (declaration.Contains(denied, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string EscapeAttributeValue(string value)
        => value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    [GeneratedRegex(@"<script\b[^>]*>[\s\S]*?<\/script\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptBlockRegex();

    [GeneratedRegex(@"<style\b[^>]*>[\s\S]*?<\/style\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleBlockRegex();

    [GeneratedRegex(@"<iframe\b[^>]*>[\s\S]*?<\/iframe\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex IframeBlockRegex();

    [GeneratedRegex(@"<object\b[^>]*>[\s\S]*?<\/object\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex ObjectBlockRegex();

    [GeneratedRegex(@"<embed\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex EmbedTagRegex();

    [GeneratedRegex(@"<svg\b[^>]*>[\s\S]*?<\/svg\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex SvgBlockRegex();

    [GeneratedRegex(@"<!--[\s\S]*?-->")]
    private static partial Regex HtmlCommentRegex();

    [GeneratedRegex("""([a-zA-Z0-9\-_:]+)(?:\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+)))?""")]
    private static partial Regex AttributeRegex();

    private readonly record struct HtmlTag(bool IsClosing, string Name, string Attributes, bool IsSelfClosing);
}
