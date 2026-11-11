using System.Net;
using System.Text;
using System.Text.Json;

namespace Crosswalk.Core.Jira;

/// <summary>
/// Converts Atlassian Document Format (ADF) JSON to safe, readable HTML.
/// Handles paragraphs, headings, lists, code blocks, links, and mentions.
/// Never passes raw HTML through — all text is entity-encoded.
/// </summary>
public static class AdfConverter
{
    public static string ToHtml(JsonElement adf)
    {
        if (adf.ValueKind != JsonValueKind.Object)
            return "";

        var sb = new StringBuilder();
        if (adf.TryGetProperty("content", out var content))
        {
            foreach (var node in content.EnumerateArray())
            {
                RenderNode(node, sb);
            }
        }
        return sb.ToString();
    }

    private static void RenderNode(JsonElement node, StringBuilder sb)
    {
        var type = node.TryGetProperty("type", out var t) ? t.GetString() : null;

        switch (type)
        {
            case "paragraph":
                sb.Append("<p>");
                RenderChildren(node, sb);
                sb.Append("</p>");
                break;

            case "heading":
                var level = node.TryGetProperty("attrs", out var ha) &&
                            ha.TryGetProperty("level", out var hl) ? hl.GetInt32() : 3;
                level = Math.Clamp(level, 1, 6);
                sb.Append($"<h{level}>");
                RenderChildren(node, sb);
                sb.Append($"</h{level}>");
                break;

            case "bulletList":
                sb.Append("<ul>");
                RenderChildren(node, sb);
                sb.Append("</ul>");
                break;

            case "orderedList":
                sb.Append("<ol>");
                RenderChildren(node, sb);
                sb.Append("</ol>");
                break;

            case "listItem":
                sb.Append("<li>");
                RenderChildren(node, sb);
                sb.Append("</li>");
                break;

            case "codeBlock":
                var lang = node.TryGetProperty("attrs", out var ca) &&
                           ca.TryGetProperty("language", out var cl) ? cl.GetString() : null;
                sb.Append(lang != null ? $"<pre><code class=\"language-{Encode(lang)}\">" : "<pre><code>");
                RenderChildren(node, sb);
                sb.Append("</code></pre>");
                break;

            case "blockquote":
                sb.Append("<blockquote>");
                RenderChildren(node, sb);
                sb.Append("</blockquote>");
                break;

            case "rule":
                sb.Append("<hr/>");
                break;

            case "table":
                sb.Append("<table>");
                RenderChildren(node, sb);
                sb.Append("</table>");
                break;

            case "tableRow":
                sb.Append("<tr>");
                RenderChildren(node, sb);
                sb.Append("</tr>");
                break;

            case "tableHeader":
                sb.Append("<th>");
                RenderChildren(node, sb);
                sb.Append("</th>");
                break;

            case "tableCell":
                sb.Append("<td>");
                RenderChildren(node, sb);
                sb.Append("</td>");
                break;

            case "text":
                RenderTextNode(node, sb);
                break;

            case "hardBreak":
                sb.Append("<br/>");
                break;

            case "mention":
                // Render mentions as plain text names
                var mentionText = node.TryGetProperty("attrs", out var ma) &&
                                  ma.TryGetProperty("text", out var mt) ? mt.GetString() : "@someone";
                sb.Append($"<span class=\"mention\">{Encode(mentionText ?? "@someone")}</span>");
                break;

            case "inlineCard":
                // Render inline cards (links) as plain links
                if (node.TryGetProperty("attrs", out var ica) &&
                    ica.TryGetProperty("url", out var icu))
                {
                    var url = icu.GetString() ?? "";
                    sb.Append($"<a href=\"{Encode(url)}\" target=\"_blank\" rel=\"noopener noreferrer\">{Encode(url)}</a>");
                }
                break;

            case "mediaSingle":
            case "media":
                sb.Append("<p><em>[media attachment]</em></p>");
                break;

            default:
                // Unknown node types: render children if any
                RenderChildren(node, sb);
                break;
        }
    }

    private static void RenderTextNode(JsonElement node, StringBuilder sb)
    {
        var text = node.TryGetProperty("text", out var tv) ? tv.GetString() ?? "" : "";

        if (!node.TryGetProperty("marks", out var marks))
        {
            sb.Append(Encode(text));
            return;
        }

        // Apply marks (bold, italic, code, link, etc.)
        var openTags = new List<string>();
        var closeTags = new List<string>();

        foreach (var mark in marks.EnumerateArray())
        {
            var markType = mark.TryGetProperty("type", out var mt) ? mt.GetString() : null;
            switch (markType)
            {
                case "strong":
                    openTags.Add("<strong>");
                    closeTags.Insert(0, "</strong>");
                    break;
                case "em":
                    openTags.Add("<em>");
                    closeTags.Insert(0, "</em>");
                    break;
                case "code":
                    openTags.Add("<code>");
                    closeTags.Insert(0, "</code>");
                    break;
                case "strike":
                    openTags.Add("<s>");
                    closeTags.Insert(0, "</s>");
                    break;
                case "underline":
                    openTags.Add("<u>");
                    closeTags.Insert(0, "</u>");
                    break;
                case "link":
                    var href = mark.TryGetProperty("attrs", out var la) &&
                               la.TryGetProperty("href", out var lh) ? lh.GetString() ?? "" : "";
                    openTags.Add($"<a href=\"{Encode(href)}\" target=\"_blank\" rel=\"noopener noreferrer\">");
                    closeTags.Insert(0, "</a>");
                    break;
            }
        }

        foreach (var tag in openTags) sb.Append(tag);
        sb.Append(Encode(text));
        foreach (var tag in closeTags) sb.Append(tag);
    }

    private static void RenderChildren(JsonElement node, StringBuilder sb)
    {
        if (node.TryGetProperty("content", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                RenderNode(child, sb);
            }
        }
    }

    private static string Encode(string? text) => WebUtility.HtmlEncode(text ?? "");
}
