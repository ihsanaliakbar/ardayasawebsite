namespace Ardayasa.Domain.Entities;

public class FaqItem
{
    public Guid Id { get; set; }

    public required string Question { get; set; }

    /// <summary>Answer as sanitized HTML (simple markup: paragraphs, bold, lists).</summary>
    public required string AnswerHtml { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
