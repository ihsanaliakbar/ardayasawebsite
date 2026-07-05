namespace Ardayasa.Domain.Entities;

/// <summary>
/// Admin-managed link between a patient and a psychologist. An assignment is the
/// sole thing that grants a psychologist read access to the patient's
/// <see cref="PatientProfile"/>. Many-to-many: a patient can have several
/// psychologists and vice versa; duplicates are blocked by a unique DB index.
/// </summary>
public class PatientAssignment
{
    public Guid Id { get; set; }

    /// <summary>Identity user id of the patient.</summary>
    public Guid PatientUserId { get; set; }

    public Guid PsychologistId { get; set; }

    public Psychologist? Psychologist { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    /// <summary>Admin who created the assignment (audit trail; no FK on purpose, like AuditLog.ActorUserId).</summary>
    public Guid AssignedByUserId { get; set; }
}
