using Ardayasa.Application.Common.Interfaces;
using Ganss.Xss;

namespace Ardayasa.Infrastructure.Content;

/// <summary>
/// Allowlist HTML sanitizer for TipTap output. Permits basic formatting, headings,
/// lists, links and images; strips scripts, event handlers, styles and unknown tags.
/// </summary>
public class HtmlContentSanitizer : IContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "br", "strong", "b", "em", "i", "u", "s", "a", "ul", "ol", "li",
                     "h2", "h3", "h4", "blockquote", "img", "figure", "figcaption", "code", "pre", "hr",
                 })
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        _sanitizer.AllowedAttributes.Clear();
        foreach (var attr in new[] { "href", "src", "alt", "title", "target", "rel" })
        {
            _sanitizer.AllowedAttributes.Add(attr);
        }

        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");

        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.KeepChildNodes = true;

        // Force safe link behavior on anything that survives sanitization.
        _sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is AngleSharp.Dom.IElement { TagName: "A" } a)
            {
                a.SetAttribute("rel", "noopener noreferrer");
            }
        };
    }

    public string Sanitize(string html) => _sanitizer.Sanitize(html);
}
