// DTOs mirroring the public content API (Ardayasa.Application.Content).

export interface PsychologistSummary {
  id: string;
  displayName: string;
  title: string | null;
  slug: string | null;
  specialization: string | null;
  expertise: string[];
  photoUrl: string | null;
}

export interface Testimonial {
  id: string;
  authorName: string;
  roleLabel: string | null;
  content: string;
  rating: number;
}

/** Availability-derived practice schedule; times are wall-clock WIB 'HH:mm:ss'. */
export interface ScheduleDay {
  dayOfWeek: string; // API DayOfWeek enum name, e.g. 'Monday'
  ranges: { startTime: string; endTime: string }[];
}

export interface PsychologistDetail extends PsychologistSummary {
  education: string[];
  bio: string | null;
  /** Static Phase 1 text — fallback while schedule (live availability) is empty. */
  scheduleLines: string[];
  schedule: ScheduleDay[];
  testimonials: Testimonial[];
}

export interface Service {
  id: string;
  name: string;
  description: string | null;
  durationMinutes: number | null;
  offlinePrice: number | null;
  onlinePrice: number | null;
  sessionCount: number;
  notes: string | null;
}

export interface ServiceCategory {
  id: string;
  name: string;
  description: string | null;
  services: Service[];
}

export interface ArticleCategory {
  id: string;
  name: string;
  slug: string;
}

export interface ArticleListItem {
  title: string;
  slug: string;
  excerpt: string | null;
  featuredImageUrl: string | null;
  categoryName: string | null;
  categorySlug: string | null;
  publishedAtUtc: string | null;
}

export interface ArticleDetail extends ArticleListItem {
  contentHtml: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface FaqItem {
  id: string;
  question: string;
  answerHtml: string;
}
