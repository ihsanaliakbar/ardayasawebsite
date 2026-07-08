// DTOs mirroring the scheduling/booking API (Ardayasa.Application.Scheduling / .Bookings).
// Enum values mirror the API's enum names; labels live in id.json under enums.*.

export type DayOfWeek =
  | 'Sunday'
  | 'Monday'
  | 'Tuesday'
  | 'Wednesday'
  | 'Thursday'
  | 'Friday'
  | 'Saturday';

/** Monday-first ordering used everywhere availability is displayed. */
export const WEEK_DAYS: DayOfWeek[] = [
  'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday',
];

export type ExceptionKind = 'Block' | 'Extra';

export type BookingMode = 'Offline' | 'Online';

export type BookingStatus =
  | 'PendingPayment'
  | 'AwaitingVerification'
  | 'Confirmed'
  | 'Completed'
  | 'NoShow'
  | 'Cancelled'
  | 'Expired';

export const BOOKING_STATUSES: BookingStatus[] = [
  'PendingPayment', 'AwaitingVerification', 'Confirmed', 'Completed', 'NoShow', 'Cancelled', 'Expired',
];

export interface AvailabilityRule {
  id: string;
  dayOfWeek: DayOfWeek;
  startTime: string; // 'HH:mm:ss', wall-clock WIB
  endTime: string;
}

export interface AvailabilityException {
  id: string;
  date: string; // 'YYYY-MM-DD', WIB
  kind: ExceptionKind;
  startTime: string | null;
  endTime: string | null;
}

export interface AvailabilityView {
  rules: AvailabilityRule[];
  exceptions: AvailabilityException[];
}

export interface AvailabilityRuleInput {
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
}

export interface PsychologistServiceMapRow {
  serviceId: string;
  name: string;
  categoryName: string;
  durationMinutes: number | null;
  offlinePrice: number | null;
  onlinePrice: number | null;
  enabled: boolean;
}

export interface ClinicSettings {
  slotBufferMinutes: number;
}

export interface BookableService {
  id: string;
  name: string;
  categoryName: string;
  durationMinutes: number;
  offlinePrice: number | null;
  onlinePrice: number | null;
  notes: string | null;
}

/** A psychologist offering a given service (wizard "pilih psikolog" step). */
export interface ServicePsychologist {
  psychologistId: string;
  displayName: string;
  title: string | null;
  specialization: string | null;
  slug: string | null;
  photoUrl: string | null;
}

export interface Slot {
  startUtc: string;
  endUtc: string;
  /** Every psychologist free at this time; one entry when the query was per-psychologist. */
  psychologistIds: string[];
}

export interface DaySlots {
  date: string; // 'YYYY-MM-DD', WIB
  slots: Slot[];
}

export interface PatientBooking {
  id: string;
  psychologistId: string;
  psychologistName: string;
  psychologistSlug: string | null;
  serviceName: string;
  mode: BookingMode;
  startUtc: string;
  endUtc: string;
  durationMinutes: number;
  priceIdr: number;
  status: BookingStatus;
  zoomLink: string | null;
  paymentDueAtUtc: string | null;
  createdAtUtc: string;
}

export interface StaffBooking {
  id: string;
  psychologistId: string;
  psychologistName: string;
  serviceName: string;
  mode: BookingMode;
  startUtc: string;
  endUtc: string;
  durationMinutes: number;
  priceIdr: number;
  status: BookingStatus;
  zoomLink: string | null;
  patientName: string;
  patientWhatsApp: string | null;
  createdAtUtc: string;
}

export interface PagedBookings {
  items: StaffBooking[];
  totalCount: number;
  page: number;
  pageSize: number;
}
