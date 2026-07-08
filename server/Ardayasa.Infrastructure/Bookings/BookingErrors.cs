using Ardayasa.Application.Common;

namespace Ardayasa.Infrastructure.Bookings;

public static class BookingErrors
{
    public static readonly Error PsychologistNotFound = new("booking.psychologist_not_found", "No active psychologist matches.");
    public static readonly Error ServiceNotBookable = new("booking.service_not_bookable", "The service is not directly bookable online.");
    public static readonly Error ServiceNotOffered = new("booking.service_not_offered", "The psychologist does not offer this service.");
    public static readonly Error ModeUnavailable = new("booking.mode_unavailable", "The service is not offered in the requested mode.");
    public static readonly Error IntakeIncomplete = new("booking.intake_incomplete", "The intake form (Data Pribadi) must be completed before booking.");
    public static readonly Error SlotUnavailable = new("booking.slot_unavailable", "The requested time is not an available slot.");
    public static readonly Error SlotTaken = new("booking.slot_taken", "The slot was just taken by another booking.");
    public static readonly Error InvalidDateRange = new("booking.invalid_date_range", "Invalid or too large date range.");
    public static readonly Error BookingNotFound = new("booking.not_found", "No such booking.");
    public static readonly Error ZoomLinkInvalid = new("booking.zoom_link_invalid", "The meeting link must be a valid https URL.");
    public static readonly Error ZoomLinkOfflineBooking = new("booking.zoom_link_offline", "Meeting links only apply to online bookings.");
}
