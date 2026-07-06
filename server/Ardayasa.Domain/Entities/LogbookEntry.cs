namespace Ardayasa.Domain.Entities;

/// <summary>
/// Psychologist-authored log of one counseling session, attached to a patient
/// account. Readable by every psychologist currently assigned to the patient
/// (via <see cref="PatientAssignment"/>); editable only by its author; never
/// deletable through the API. Admin and the patient have NO access — not even
/// to the fact that entries exist — so logbook actions are never audit-logged
/// and entry content never goes into logs.
/// </summary>
public class LogbookEntry
{
    public Guid Id { get; set; }

    /// <summary>Identity user id of the patient the entry belongs to. The patient's name is resolved from the account, never stored here.</summary>
    public Guid PatientUserId { get; set; }

    /// <summary>The psychologist who wrote the entry — the only one allowed to edit it.</summary>
    public Guid AuthorPsychologistId { get; set; }

    public Psychologist? AuthorPsychologist { get; set; }

    /// <summary>Tanggal konseling. A calendar date — no time-of-day/timezone component.</summary>
    public DateOnly SessionDate { get; set; }

    /// <summary>Sesi keberapa — entered manually; sessions may predate the system.</summary>
    public int SessionNumber { get; set; }

    /// <summary>Ringkasan kasus.</summary>
    public required string CaseSummary { get; set; }

    /// <summary>Aktivitas sesi.</summary>
    public required string SessionActivities { get; set; }

    /// <summary>PR / tugas rumah.</summary>
    public string? Homework { get; set; }

    /// <summary>Rencana sesi selanjutnya.</summary>
    public string? NextSessionPlan { get; set; }

    /// <summary>Apakah perlu sesi lanjutan.</summary>
    public bool FollowUpNeeded { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Null until the author edits the entry — drives the "diedit" marker in the UI.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
}
