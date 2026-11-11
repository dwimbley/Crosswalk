using System.Text.Json;
using Crosswalk.Core.Jira;

namespace Crosswalk.Tests;

public class AdfConverterTests
{
    [Fact]
    public void Paragraph_ConvertsToHtml()
    {
        var adf = Parse("""
        {
            "type": "doc",
            "content": [
                { "type": "paragraph", "content": [{ "type": "text", "text": "Hello world" }] }
            ]
        }
        """);

        Assert.Equal("<p>Hello world</p>", AdfConverter.ToHtml(adf));
    }

    [Fact]
    public void Bold_And_Italic()
    {
        var adf = Parse("""
        {
            "type": "doc",
            "content": [{
                "type": "paragraph",
                "content": [{
                    "type": "text",
                    "text": "bold italic",
                    "marks": [{ "type": "strong" }, { "type": "em" }]
                }]
            }]
        }
        """);

        Assert.Equal("<p><strong><em>bold italic</em></strong></p>", AdfConverter.ToHtml(adf));
    }

    [Fact]
    public void Link_RendersWithNoopener()
    {
        var adf = Parse("""
        {
            "type": "doc",
            "content": [{
                "type": "paragraph",
                "content": [{
                    "type": "text",
                    "text": "click here",
                    "marks": [{ "type": "link", "attrs": { "href": "https://example.com" } }]
                }]
            }]
        }
        """);

        var html = AdfConverter.ToHtml(adf);
        Assert.Contains("target=\"_blank\"", html);
        Assert.Contains("rel=\"noopener noreferrer\"", html);
        Assert.Contains("https://example.com", html);
    }

    [Fact]
    public void CodeBlock_Renders()
    {
        var adf = Parse("""
        {
            "type": "doc",
            "content": [{
                "type": "codeBlock",
                "attrs": { "language": "csharp" },
                "content": [{ "type": "text", "text": "var x = 1;" }]
            }]
        }
        """);

        var html = AdfConverter.ToHtml(adf);
        Assert.Contains("<pre><code class=\"language-csharp\">", html);
        Assert.Contains("var x = 1;", html);
    }

    [Fact]
    public void BulletList_Renders()
    {
        var adf = Parse("""
        {
            "type": "doc",
            "content": [{
                "type": "bulletList",
                "content": [
                    { "type": "listItem", "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "Item 1" }] }] },
                    { "type": "listItem", "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "Item 2" }] }] }
                ]
            }]
        }
        """);

        var html = AdfConverter.ToHtml(adf);
        Assert.Contains("<ul>", html);
        Assert.Contains("<li><p>Item 1</p></li>", html);
    }

    [Fact]
    public void Mention_RendersAsPlainText()
    {
        var adf = Parse("""
        {
            "type": "doc",
            "content": [{
                "type": "paragraph",
                "content": [
                    { "type": "text", "text": "Assigned to " },
                    { "type": "mention", "attrs": { "id": "acc-123", "text": "@David Wimbley" } }
                ]
            }]
        }
        """);

        var html = AdfConverter.ToHtml(adf);
        Assert.Contains("@David Wimbley", html);
        Assert.Contains("class=\"mention\"", html);
    }

    [Fact]
    public void HtmlEntities_AreEscaped()
    {
        var adf = Parse("""
        {
            "type": "doc",
            "content": [{
                "type": "paragraph",
                "content": [{ "type": "text", "text": "<script>alert('xss')</script>" }]
            }]
        }
        """);

        var html = AdfConverter.ToHtml(adf);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void EmptyDoc_ReturnsEmpty()
    {
        var adf = Parse("""{ "type": "doc", "content": [] }""");
        Assert.Equal("", AdfConverter.ToHtml(adf));
    }

    [Fact]
    public void NonObject_ReturnsEmpty()
    {
        var adf = JsonDocument.Parse("null").RootElement;
        Assert.Equal("", AdfConverter.ToHtml(adf));
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
