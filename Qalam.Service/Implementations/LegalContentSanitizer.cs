using Ganss.Xss;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class LegalContentSanitizer : ILegalContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public LegalContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "br", "strong", "b", "em", "i", "u", "s", "ul", "ol", "li",
                     "h1", "h2", "h3", "h4", "blockquote", "a", "span", "div"
                 })
            _sanitizer.AllowedTags.Add(tag);

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedAttributes.Add("rel");
        _sanitizer.AllowedAttributes.Add("class");

        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");
    }

    public string? Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        var cleaned = _sanitizer.Sanitize(html).Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }
}
