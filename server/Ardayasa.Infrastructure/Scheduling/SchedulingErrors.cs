using Ardayasa.Application.Common;

namespace Ardayasa.Infrastructure.Scheduling;

public static class SchedulingErrors
{
    public static readonly Error PsychologistNotFound = new("scheduling.psychologist_not_found", "No psychologist with that id.");
    public static readonly Error InvalidTimeRange = new("scheduling.invalid_time_range", "Start time must be before end time.");
    public static readonly Error OverlappingRules = new("scheduling.overlapping_rules", "Availability windows on the same day must not overlap.");
    public static readonly Error ExceptionTimesRequired = new("scheduling.exception_times_required", "Extra availability requires start and end times.");
    public static readonly Error ExceptionDateInPast = new("scheduling.exception_date_in_past", "Exceptions can only be added for today or later (WIB).");
    public static readonly Error ExceptionNotFound = new("scheduling.exception_not_found", "No such exception for this psychologist.");
    public static readonly Error UnknownServiceIds = new("scheduling.unknown_service_ids", "One or more service ids are not bookable catalog services.");
    public static readonly Error InvalidBuffer = new("settings.invalid_buffer", "Slot buffer must be between 0 and 120 minutes.");
}
