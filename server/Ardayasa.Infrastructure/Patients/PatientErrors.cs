using Ardayasa.Application.Common;

namespace Ardayasa.Infrastructure.Patients;

/// <summary>Stable error codes for patient intake & assignment, mapped to Indonesian in the client.</summary>
public static class PatientErrors
{
    public static readonly Error NameRequired = new("patients.name_required", "Full name is required.");
    public static readonly Error DiagnosisRequired = new("patients.diagnosis_required", "Diagnosis description is required when a prior diagnosis is reported.");
    public static readonly Error PatientNotFound = new("patients.patient_not_found", "The referenced patient does not exist.");
    public static readonly Error PsychologistNotFound = new("patients.psychologist_not_found", "The referenced psychologist does not exist.");
    public static readonly Error AlreadyAssigned = new("patients.already_assigned", "The patient is already assigned to this psychologist.");
    public static readonly Error AssignmentNotFound = new("patients.assignment_not_found", "The assignment does not exist.");
}
