using Ardayasa.Application.Common;

namespace Ardayasa.Infrastructure.Patients;

/// <summary>Stable error codes for the counseling logbook, mapped to Indonesian in the client.</summary>
public static class LogbookErrors
{
    public static readonly Error NotAssigned = new("logbook.not_assigned", "The patient does not exist or is not assigned to the caller.");
    public static readonly Error EntryNotFound = new("logbook.entry_not_found", "The logbook entry does not exist for this patient.");
    public static readonly Error NotAuthor = new("logbook.not_author", "Only the author of a logbook entry may edit it.");
    public static readonly Error SessionDateRequired = new("logbook.session_date_required", "Session date is required.");
    public static readonly Error SessionNumberInvalid = new("logbook.session_number_invalid", "Session number must be 1 or greater.");
    public static readonly Error SummaryRequired = new("logbook.summary_required", "Case summary is required.");
    public static readonly Error ActivitiesRequired = new("logbook.activities_required", "Session activities are required.");
}
