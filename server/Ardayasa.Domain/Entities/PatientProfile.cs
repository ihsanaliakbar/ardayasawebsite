namespace Ardayasa.Domain.Entities;

public enum Gender
{
    Male,
    Female,
}

public enum MaritalStatus
{
    Single,
    Married,
    Divorced,
    Widowed,
}

public enum EducationLevel
{
    ElementarySchool,
    JuniorHighSchool,
    SeniorHighSchool,
    Diploma,
    Bachelor,
    Master,
    Doctorate,
}

/// <summary>
/// Patient-supplied intake form (one per patient account). Contains sensitive
/// personal data under UU 27/2022 — including self-reported diagnosis history —
/// so it is readable ONLY by the patient themselves and psychologists assigned
/// via <see cref="PatientAssignment"/>. Never expose through admin endpoints,
/// never write field values into logs or audit entries.
/// </summary>
public class PatientProfile
{
    /// <summary>Identity user id of the patient. PK — one profile per account.</summary>
    public Guid UserId { get; set; }

    /// <summary>Full legal name (Nama) — may differ from the account display name.</summary>
    public required string FullName { get; set; }

    /// <summary>Tempat lahir.</summary>
    public string? BirthPlace { get; set; }

    /// <summary>Tanggal lahir. A calendar date — no time-of-day/timezone component.</summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>Jenis kelamin.</summary>
    public Gender? Gender { get; set; }

    /// <summary>Alamat domisili.</summary>
    public string? DomicileAddress { get; set; }

    /// <summary>Status pernikahan.</summary>
    public MaritalStatus? MaritalStatus { get; set; }

    /// <summary>Pendidikan terakhir.</summary>
    public EducationLevel? LastEducation { get; set; }

    /// <summary>Pekerjaan.</summary>
    public string? Occupation { get; set; }

    /// <summary>Pernah mengakses layanan psikologi sebelumnya. Null = not answered yet.</summary>
    public bool? HasAccessedPsychologyServices { get; set; }

    /// <summary>Pernah mendapatkan diagnosis dari psikolog atau psikiater. Null = not answered yet.</summary>
    public bool? HasPriorDiagnosis { get; set; }

    /// <summary>Diagnosis yang didapatkan — only relevant when <see cref="HasPriorDiagnosis"/> is true.</summary>
    public string? PriorDiagnosis { get; set; }

    /// <summary>Gambaran khusus yang ingin dikonsultasikan dengan psikolog.</summary>
    public string? ConsultationConcerns { get; set; }

    /// <summary>Harapan menjalani konseling di Ardayasa.</summary>
    public string? CounselingExpectations { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// True when every intake question is answered (the diagnosis description only
    /// counts when a prior diagnosis is reported). Drives the dashboard reminder.
    /// </summary>
    public bool IsComplete()
        => !string.IsNullOrWhiteSpace(FullName)
           && !string.IsNullOrWhiteSpace(BirthPlace)
           && BirthDate is not null
           && Gender is not null
           && !string.IsNullOrWhiteSpace(DomicileAddress)
           && MaritalStatus is not null
           && LastEducation is not null
           && !string.IsNullOrWhiteSpace(Occupation)
           && HasAccessedPsychologyServices is not null
           && HasPriorDiagnosis is not null
           && (HasPriorDiagnosis == false || !string.IsNullOrWhiteSpace(PriorDiagnosis))
           && !string.IsNullOrWhiteSpace(ConsultationConcerns)
           && !string.IsNullOrWhiteSpace(CounselingExpectations);
}
