namespace Ardayasa.Application.Common.Interfaces;

/// <summary>
/// Sanitizes admin-authored rich-text HTML (TipTap output, FAQ answers) before it is
/// stored. Public pages render this HTML verbatim, so everything persisted must have
/// gone through here.
/// </summary>
public interface IContentSanitizer
{
    string Sanitize(string html);
}
