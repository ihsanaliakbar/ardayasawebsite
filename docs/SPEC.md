# SPEC — Psychology Clinic & Counseling Center Website

> This is the authoritative project specification. Claude Code: read this together with `CLAUDE.md` at the start of every session. `CLAUDE.md` tells you how to work and which phase we're in; this file tells you what to build. logo ist in screenshots_instagram/ardayasa_logo.jpg

---

## 1. Project overview

A website for a psychology clinic / counseling center in Indonesia. The clinic has **multiple psychologists**. The site is both a public marketing site and a booking/account platform.

**Three user roles:**

- **Patient** — registers, browses, books and pays for sessions, manages their own bookings.
- **Psychologist** — manages their own profile, availability, and bookings; provides the Zoom link for online sessions.
- **Admin** — full control: manages psychologists, patients, services & pricing, articles, FAQ, testimonials, verifies payments, sets cancellation/reschedule policy, and can set availability on any psychologist's behalf.

**Language:** All user-facing content is in **Bahasa Indonesia**. Keep user-facing strings in a translation/resource file (not hardcoded inline) so an English version can be added later without a rewrite. Code, comments, and identifiers in English.

**Timezone:** Store all timestamps in **UTC** in the database; display and accept input in **WIB (Asia/Jakarta)**. Availability is defined in WIB.

---

## 2. Tech stack

- **Frontend:** Angular (latest LTS) with **SSR (Angular Universal / built-in `@angular/ssr`)** — required for the public site's SEO (see Section 4 and NFRs). TypeScript, Angular Router, reactive forms, **Angular Material** themed to the clinic's brand colors (colors pending — see [AWAITING INPUT] items).
- **Backend:** ASP.NET Core Web API (C#, latest LTS), Entity Framework Core.
- **Database:** PostgreSQL.
- **Auth:** ASP.NET Core Identity + JWT for the SPA, plus **Google login** (OAuth external provider).
  - **Access token:** short-lived (~15 min), held in memory on the client (never localStorage).
  - **Refresh token:** httpOnly, Secure, SameSite cookie; rotated on use; revocable server-side.
- **Background jobs:** **Hangfire** (PostgreSQL storage) for slot auto-expiry and scheduled reminders. Chosen over a bare `BackgroundService` because we need persistent, observable, retryable scheduled jobs (reminders must survive restarts) and the dashboard helps debugging.
- **File/image storage:** psychologist photos and article featured images stored via an `IFileStorage` interface. v1 implementation: local disk (Docker volume) with size/type validation and randomized filenames. Swappable to S3-compatible object storage later.
- **Containerization:** Dockerized from the start (see Section 10).

Structure the solution cleanly (API / Application / Domain / Infrastructure layering, or a pragmatic equivalent). Keep all external integrations (payment later, WhatsApp, email, Google OAuth, file storage) behind interfaces so providers can be swapped.

---

## 3. Roles & permissions

| Capability | Patient | Psychologist | Admin |
|---|---|---|---|
| Register / login (incl. Google) | ✅ | ✅ | ✅ |
| Browse public site | ✅ | ✅ | ✅ |
| Book & pay for a session | ✅ | — | — |
| View/manage own bookings | ✅ | own sessions | all |
| Reschedule/cancel own booking (per policy) | ✅ | — | ✅ (any) |
| Set own availability | — | ✅ | ✅ (for anyone) |
| Provide Zoom link for a session | — | ✅ (own) | ✅ (any) |
| Edit own psychologist profile | — | ✅ | ✅ (any) |
| Verify payments | — | — | ✅ |
| Manage services & pricing | — | — | ✅ |
| Manage articles / FAQ / testimonials | — | — | ✅ |
| Manage psychologists & patients | — | — | ✅ |
| Set cancellation/reschedule policy | — | — | ✅ |

**Account provisioning:**
- Patients self-register (email/password with verification, or Google).
- **Psychologists are created by admin**: admin creates the psychologist record and sends an **invitation email** with a one-time link where the psychologist sets their password (or links Google). Psychologists cannot self-register.
- A default admin account is seeded on first run (credentials via environment variables).

---

## 4. Public marketing site (no login required)

- **Home** — hero, brief intro, featured services, featured psychologists, CTA to book. brand color and copy see the screenshots.
- **Psychologist profiles** — list + detail page: photo, name, title/credentials, areas of expertise, short bio, and their testimonials. you can see the info in screenshots_instagram/psychologist*.png. for testimonial generate example testimonial
- **Services** — general description of what the clinic offers. The bookable catalog with prices comes from Section 6. this part you can also see in the screenshots_instagram/open_hours.png and screenshots_instagram/service_pricelist*.png
- **Articles / blog** — public list + detail, with categories and search. Content is authored by admins via an in-site rich-text editor (Section 7).
- **Testimonials** — managed by admin, shown on home and/or psychologist pages.
- **FAQ** — managed by admin (question/answer, ordered).
- **About / Contact** — clinic info, address, map, WhatsApp/contact. clinic is in Bukit Cimanggu City, Bogor https://www.google.com/maps/place/Ardayasa+Wellbeing+and+Growth+Center/@-6.5458236,106.7802654,17z/data=!3m1!4b1!4m6!3m5!1s0x2e69c31195bf3d57:0xe37542b5af907a50!8m2!3d-6.5458236!4d106.7802654!16s%2Fg%2F11ynvqp6rf?entry=ttu&g_ep=EgoyMDI2MDYyOS4wIKXMDSoASAFQAw%3D%3D 
whatsapp/contact : https://api.whatsapp.com/send/?phone=6285121305115&text&type=phone_number&app_absent=0
+6285121305115

The whole public site must be responsive and SEO-friendly: proper titles/meta per route, semantic markup, Open Graph tags for articles, and **server-rendered article URLs via Angular SSR** so crawlers get real HTML. Admin and patient dashboards do not need SSR.

---

## 5. Accounts & authentication

- Email/password registration with verification email.
- **Google login.**
- JWT access + refresh token flow as specified in Section 2 (httpOnly cookie refresh, rotation, revocation).
- Password reset via email.
- Role-based route guards (Angular) and authorization policies (API) — every endpoint explicitly authorized.
- Patient profile: name, email, phone (**WhatsApp number — required**, since we notify via WhatsApp), basic details only.
- **No clinical or session notes anywhere in v1.** Store only booking data + basic contact/profile info.
- Account/data-deletion path for patients (see Section 9).

---

## 6. Booking & scheduling

### 6.1 Services

- A catalog of services, each with: name, description, **duration**, **price**, and session type support (in-person / online / both).
- Model: a service has a base price/duration; a psychologist *may* override the price for that service (nullable per-psychologist price). **[CONFIRM once catalog is provided]** whether pricing is actually global or per-psychologist.
- service catalog and price see in screenshots_instagram/service_pricelist*.png

### 6.2 Availability

- Each psychologist has **recurring weekly availability** (e.g. Mon–Fri 09:00–17:00 WIB) plus **exceptions** (block a date/time, or add extra one-off availability).
- Settable by the psychologist for themselves, and by the admin for anyone.
- From availability + service duration, generate bookable **time slots**, with a configurable buffer between sessions. **Proposed default: 15 minutes** — [CONFIRM].

### 6.3 Booking flow (patient)

1. Patient picks a psychologist and a service (or service then psychologist), and **chooses in-person or online**.
2. Patient sees available slots (WIB) and selects one.
3. On booking, the slot is held with status **`PendingPayment`** and a **30-minute countdown**. Show bank-transfer instructions (manual payment — no gateway in v1).
4. Patient clicks **"Saya sudah bayar"** → status becomes **`AwaitingVerification`** and the auto-expiry timer stops. (No proof-of-transfer upload in v1.)
5. If the 30-minute window lapses while still `PendingPayment`, a **Hangfire job auto-releases** the slot → status **`Expired`**.
6. **Admin verifies** payment → status **`Confirmed`**. On confirmation, send confirmation notifications (Section 8).
7. For **online** sessions, the psychologist (or admin) attaches a **Zoom link** to the booking; it becomes visible to the patient once confirmed.
8. After the session time passes, allow marking **`Completed`** / **`NoShow`**.

**Booking status machine:** `PendingPayment → AwaitingVerification → Confirmed → Completed` with side branches `→ Expired` (timeout from PendingPayment only), `→ Cancelled` (per policy), `→ NoShow` (from Confirmed, after session time). Implement as an explicit, guarded state machine — invalid transitions must be impossible, and slot holds must be race-safe with a **DB-level uniqueness constraint** preventing double-booking of the same slot.

### 6.4 Cancellation & reschedule

- Policy is **admin-configurable** (reschedule/cancel allowed up to X hours before the session). Stored as settings. **Proposed default: 24 hours** — [CONFIRM].
- Patients can reschedule/cancel their own bookings within policy; admins can do it for anyone (bypassing the window).
- Since payment is manual, refunds are handled offline — just reflect status and notify; no refund automation.

### 6.5 Admin payment-verification dashboard

- A queue of bookings in `AwaitingVerification` with patient, psychologist, service, amount, time — one click to confirm or reject. Every confirm/reject is written to the audit log.

---

## 7. Content management (admin)

- **Article editor:** **TipTap** (Angular-compatible via its headless core; clean HTML output, actively maintained). Support draft/published, categories, featured image (via `IFileStorage`), slug, publish date.
- **FAQ**, **testimonials**, **psychologist profiles**, **services & pricing**, and **policy/settings** all editable from an admin area.

---

## 8. Integrations & notifications

- **Email:** transactional email behind an `IEmailSender` interface. v1 implementation: **SMTP via MailKit**, config from environment variables. Swappable to a provider (Resend, SES, etc.) later.
- **WhatsApp:** notifications via **Fonnte** (unofficial Indonesian WhatsApp gateway), isolated behind an **`IWhatsAppSender` interface** so it can be swapped (Wablas, official WA Business API) later. Failures must not break the booking flow — log and continue.
- **Notification events** (email + WhatsApp where sensible): account verification (email only), booking placed / pending payment, payment confirmed (with Zoom link if online), session reminders (**default 24h and 1h before** — configurable), cancellation/reschedule.
- **Reminders** run via Hangfire recurring/delayed jobs.
- Keep a **notification log** table (event, channel, recipient, status, timestamp, error) for debugging/audit.

---

## 9. Non-functional requirements

- **Security / privacy (Indonesia PDP Law – UU 27/2022 awareness):** HTTPS everywhere, Identity password hashing, JWT best practices per Section 2, role-based authorization on every endpoint, input validation, rate-limiting on auth endpoints, minimal PII, and an **audit log** for admin actions on bookings/payments/settings. Account/data-deletion path for patients (soft-delete + PII anonymization of past bookings; hard-delete of profile). No health/clinical data stored in v1.
- **Reliability:** guarded booking state machine; race-safe slot holds enforced at the DB level (unique constraint / transactional hold).
- **i18n-ready:** Indonesian UI via a translation file structure (`@angular/localize` or ngx-translate — pick one and be consistent; ngx-translate preferred for runtime simplicity).
- **Timezone:** UTC storage, WIB display, as above. Slot generation logic must be tested around DST-free WIB but still use proper timezone conversion (no hardcoded +7 offsets).
- **Testing:** unit tests for booking/availability/slot-generation/payment-state logic and API authorization; a few end-to-end happy-path tests for the booking flow.

---

## 10. Deployment (plan now, host later)

- A **`Dockerfile`** for the API and one for the Angular SSR app, plus a **`docker-compose.yml`** for local dev (api + web + postgres + a volume for file storage).
- All secrets/config via environment variables; provide a `.env.example`.
- EF Core migrations applied automatically in dev, documented step for prod; seed step for the admin + reference data.
- A clear `README` with local run instructions.
- Host is undecided; design host-agnostic. (Likely targets: Singapore-region VPS or Azure Indonesia region — do not optimize for a specific one.)

---

## 11. Build order (phases)

Confirm the plan (data model / API contracts for the phase) before starting each phase. Do not jump ahead. Update `CLAUDE.md` phase status and `docs/DECISIONS.md` when a phase completes.

- **Phase 0 — Foundation:** solution scaffold, Docker + compose + Postgres, EF setup, Identity + JWT (per Section 2) + Google login, roles, seeded admin, psychologist invitation flow, Hangfire wiring, Angular SSR app shell + routing + auth guards + theming hook (colors pending), `IFileStorage`/`IEmailSender`/`IWhatsAppSender` interfaces stubbed.
- **Phase 1 — Public site + content admin:** psychologist profiles, services (display), articles (public + TipTap admin editor), FAQ, testimonials, about/contact, SEO meta/SSR verification. *(Needs branding + content inputs.)*
- **Phase 2 — Availability & booking core:** service catalog, availability (psychologist + admin), slot generation with buffer, booking flow with in-person/online choice and Zoom-link handling. *(Needs services/pricing table + availability layout input.)*
- **Phase 3 — Manual payment:** booking state machine, 30-min pending hold, "Saya sudah bayar," Hangfire auto-expiry, admin verification dashboard, audit log entries.
- **Phase 4 — Notifications:** MailKit email + Fonnte WhatsApp implementations, reminders scheduler, cancellation/reschedule with admin-set policy, notification log.
- **Phase 5 — Hardening & ship:** PDP hardening, data-deletion path, full audit-log coverage, tests, deployment configs, README.

**End-of-phase verification (every phase):** run the full test suite, run `docker compose up` from clean, and confirm the app builds and the phase's happy path works before asking for review.

---

## 12. Open inputs & decisions

Ask for these when reached 

| # | Item | Status |
|---|---|---|
| 1 | Service catalog, durations, prices (**as a table**); global vs. per-psychologist pricing | ⏳ awaiting |
| 2 | Brand color palette + site copy | see screenshots_instagram for color palette and site copy |
| 3 | Availability UI layout | |
| 4 | Buffer between sessions | proposed 15 min — [CONFIRM] |
| 5 | Cancellation/reschedule window | proposed 24 h — [CONFIRM] |
| 6 | Rich-text editor | **decided: TipTap** |
| 7 | Email provider | **decided: SMTP via MailKit behind `IEmailSender`** |
| 8 | Component library | **decided: Angular Material** |
| 9 | Background jobs | **decided: Hangfire (Postgres storage)** |
| 10 | SEO approach | **decided: Angular SSR for public routes** |
| 11 | Refresh token storage | **decided: httpOnly cookie, rotated** |
| 12 | File storage | **decided: local volume behind `IFileStorage`** |
