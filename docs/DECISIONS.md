# Decisions log

Newest first. Each entry: date, decision, rationale. Stack-level fixed decisions live in `CLAUDE.md` / `SPEC.md` §12; this file records everything decided mid-session on top of those.

## 2026-07-04 — Phase 1 implementation decisions

- **Seeded psychologist accounts are placeholders**: created without a password (`UserManager.CreateAsync` sans password) with emails `firstname.lastname@ardayasa.local`. They cannot log in until claimed via the invitation/password-reset flow; admin should re-invite with real emails later. Profiles are publicly visible immediately.
- **"Buat Janji" CTAs link to the clinic WhatsApp** until the booking flow ships (Phase 2/3) — matches the current real-world registration process described in the FAQ.
- **Seed photos ship inside the API image** (`server/Ardayasa.Api/SeedAssets/`, copied on publish) and flow through `IFileStorage` at seed time, so seeded and admin-uploaded photos are served identically via `GET /api/files/{key}`.
- **Content seeding is idempotent per table** (each block seeds only when its table is empty) and runs after `DbSeeder` under the same `AUTO_MIGRATE`/Development gate.
- **HTML sanitization**: all admin-authored rich text (TipTap articles, FAQ answers) is sanitized server-side with `HtmlSanitizer` (Ganss) behind `IContentSanitizer` — allowlist of basic tags; scripts/styles/event handlers stripped. Client renders it via `[innerHTML]` + a `.rich-text` style scope.
- **Fonts via Google Fonts CDN** (Playfair Display headings + Inter body). Self-hosting can be revisited at deployment if needed.
- **`anyComponentStyle` budget raised 4→6 kB** (warning) — the landing page's styles legitimately exceed the scaffold default.
- **Article search is portable `ToLower().Contains`**, not Postgres `ILike`, because the test suite runs the real pipeline on SQLite.
- **Open item for Ihsan:** public contact email (footer/kontak). Mockup shows `halo@ardayasa.id` — unverified, so the site currently lists WhatsApp + Instagram + address only.

## 2026-07-04 — Phase 1 kickoff (decisions confirmed by Ihsan)

- **Brand palette provided** (dark teal + gold): `--bg-dark #071C1F`, `--bg-teal #0C2F35`, `--primary #0C424B`, `--primary-light #0B5461`, `--primary-hover #21535A`, `--teal-muted #669298`, `--text-main #F4F6F3`, `--text-muted #C6C6BF`, `--bubble-light #A3A5A4`, `--accent-gold #F4D96F`, `--accent-gold-dark #D1BC64`. UI reference: `screenshots_instagram/UI_Ardayasa.png` (dark theme, serif display headings, gold CTAs). Doesn't have to match pixel-perfect.
- **Content source**: `screenshots_instagram/` — 5 psychologist profiles (name, title, education, specialization badge, expertise, practice schedule), full service pricelist, 8+ FAQs, open hours, milestones, contact/location.
- **Pricing is global per service** (confirmed — closes SPEC §6.1 / open input #1's pricing question). Per-psychologist override dropped; can be added later if ever needed.
- **Service model**: `ServiceCategory` (Konseling, Bundling Konseling, Asesmen, Konsultasi Hasil, Psikoterapi) + `Service` with `DurationMinutes`, nullable `OfflinePrice`/`OnlinePrice` (null = mode unavailable), `SessionCount` for bundles, notes. Display-only in Phase 1; becomes the bookable catalog in Phase 2. Overtime rule from pricelist ("charge overtime per 15 menit") shown as a note on the services page.
- **Homepage stats use real milestones** (135+ klien, 205+ sesi, 3 kolaborasi eksternal — from the Instagram milestone post), not the mockup's placeholder numbers (5.000+ etc.). Confirmed by Ihsan.
- **Seed content**: 5 real psychologists (photos cropped from Instagram screenshots — admin can replace later), full service catalog, real FAQs, ~6 generated example testimonials (plausible but fictional — replaceable via admin), 3 sample articles. Confirmed by Ihsan.
- **Psychologist profile shows a static `scheduleText` in Phase 1** (copy from the Instagram "Jadwal Praktik"); replaced by live availability-derived schedule in Phase 2 — avoids modeling availability early.
- **Testimonial roleLabel** pattern from mockup: "Klien Konseling Individu" etc.; optional FK to a psychologist for profile-page testimonials.

## 2026-07-04 — Phase 0 implementation decisions

- **API error contract:** failures return `{ errors: [{ code, description }] }` where `code` is a stable machine key (e.g. `auth.invalid_credentials`). The Angular app maps codes to Indonesian text in `client/src/i18n/id.json` under `apiErrors.*` (dots → underscores). `description` is English/dev-only, never shown to users.
- **Backend email copy** lives centralized in `Ardayasa.Infrastructure/Email/EmailTemplates.cs` (Indonesian) — the backend counterpart of the frontend translation file; moves to per-locale resources when English is added.
- **Same-origin API access, no CORS in practice:** the Angular dev server proxies `/api` → `localhost:5000` (`proxy.conf.json`); in Docker the SSR Express server proxies `/api` → `API_BASE_URL`. Keeps the refresh cookie first-party (`SameSite=Lax`, `Path=/api/auth`). API CORS config remains as belt-and-braces.
- **Refresh-token reuse detection:** presenting a rotated/revoked refresh token revokes *all* active sessions of that user (defense against cookie theft). Covered by test.
- **ngx-translate loads `id.json` via static import** (works identically in SSR and browser; no HTTP fetch for resources). English later = second import keyed by lang.
- **Angular 22 / Material 22** (current stable at scaffold time); Material theme uses the new `mat.theme` mixin with placeholder `azure` palette — swap in `client/src/styles.scss` when brand colors land (Phase 1).
- **Microsoft.AspNetCore.OpenApi deliberately omitted** — its `Microsoft.OpenApi` dependency has an unpatched security advisory (GHSA-v5pm-xwqc-g5wc) and the project builds with warnings-as-errors. Re-add when patched.
- **Hangfire dashboard access:** Admin role, or local requests in Development. Browser-navigated dashboard has no Bearer token, so in practice it's dev-local only until a cookie-auth path is added (revisit by Phase 3 when the dashboard matters operationally).
- **Auth rate limit:** fixed window 10 requests/min per IP on `/api/auth/*` (configurable via `RateLimiting:AuthPermitLimit`).
- **Angular SSR host allowlist:** Angular 22 SSRF protection requires `security.allowedHosts` in `angular.json` (hostnames **without** ports). Currently `localhost`, `127.0.0.1` — **the production domain must be added there at deployment** (noted in README).
- **SSR `/api` proxy strips hop-by-hop headers** (`expect`, `connection`, etc.) — undici's fetch rejects them; observed with clients sending `Expect: 100-continue`.

## 2026-07-04 — Phase 0 kickoff

- **Phase 0 plan approved by Ihsan** (data model + API contracts as presented): Identity `ApplicationUser` + roles (`Admin`, `Psychologist`, `Patient`), `RefreshToken` table (hashed, rotated, revocable), minimal `Psychologist` record (full profile fields deferred to Phase 1), `AuditLog` table wired now and used from Phase 3.
- **Psychologist invitation tokens use ASP.NET Core Identity's built-in one-time token provider** — no custom invitation table. Rationale: hashed, expiring, single-use out of the box; less code to secure. Email verification and password reset use the same mechanism.
- **Repo moved out of OneDrive** to `C:\dev\ardayasawebsite` (was `C:\Users\HP\OneDrive\Desktop\ardayasaproj\ardayasawebsite`). Rationale: OneDrive sync churn and file locks break `node_modules`/build output. Old copy left in place temporarily — delete after reopening the editor at the new path.
- **Local tooling installed via winget**: .NET SDK (latest LTS) + Node.js (latest LTS). Docker 29.6.1 already present. Dev loop runs on host; Postgres and full-stack verification via docker compose.
- **Repo layout:** `/server` (Ardayasa.Api / Application / Domain / Infrastructure / Tests) + `/client` (Angular SSR) in this single repo.
- **Google OAuth**: wired in Phase 0 behind config; needs client ID/secret from Ihsan — until provided, the endpoint returns a clear "not configured" error and email/password flow is the verified path.
