# CLAUDE.md — Working conventions for this project

## What this project is

Booking + marketing website for a multi-psychologist counseling clinic in Indonesia. Full spec lives in **`docs/SPEC.md`** — read it at the start of every session. This file is the short operational memory: how to work, what's decided, where we are.

## Current status

- **Current phase:** Phase 1 — implemented & verified 2026-07-04 (public site + content admin: brand theme, home/layanan/psikolog/artikel/FAQ/tentang pages, TipTap CMS, real content seeded from Instagram screenshots; 15/15 tests green; `docker compose up` from clean with all public endpoints serving seeded content). Awaiting Ihsan's review before Phase 2.
- **Last completed:** Phase 0 (committed); Phase 1 build (not yet reviewed/committed)
- **Blocked on:** Google OAuth client ID/secret (non-blocking), availability UI layout input (Phase 2). Brand colors + copy + service catalog were received 2026-07-04 (`screenshots_instagram/`) and are seeded.
- **Confirm with Ihsan:** public contact email for footer/kontak (mockup's halo@ardayasa.id unverified — site currently lists WhatsApp + Instagram + address only)
- **Repo location:** `C:\dev\ardayasawebsite` (moved out of OneDrive 2026-07-04; delete the old OneDrive copy after switching the editor here)

> Update this section and `docs/DECISIONS.md` at the end of every phase (and after any significant mid-phase decision).

## How to work

1. **This is production-bound.** Correctness, security, and maintainability over speed.
2. **Never assume on product decisions.** Anything ambiguous, or marked [AWAITING INPUT] / [CONFIRM] in SPEC.md → ask Ihsan before implementing.
3. **Phase discipline.** Build in the order of SPEC.md §11. Before starting a phase, present the data model / API contracts for that phase and wait for OK. Don't jump ahead.
4. **Verify before review.** End of each phase: run the full test suite and `docker compose up` from clean; confirm the phase's happy path works before asking for review.
5. **Log decisions.** Any decision made mid-session (with rationale) goes into `docs/DECISIONS.md` so the next session inherits it.

## Stack & fixed decisions (do not re-litigate)

- **Frontend:** Angular latest LTS + **SSR** (public routes only), Angular Material, reactive forms, **ngx-translate** (all UI strings in Bahasa Indonesia via resource files — never hardcoded)
- **Backend:** ASP.NET Core Web API (latest LTS), EF Core, PostgreSQL
- **Auth:** ASP.NET Core Identity + JWT — access token ~15 min in memory; refresh token in httpOnly cookie, rotated, revocable. Google OAuth as external provider. Psychologists are admin-invited, never self-registered.
- **Jobs:** Hangfire (Postgres storage)
- **Editor:** TipTap
- **Email:** SMTP via MailKit behind `IEmailSender`
- **WhatsApp:** Fonnte behind `IWhatsAppSender` (failures never break the booking flow)
- **Files:** local Docker volume behind `IFileStorage`
- **Layering:** API / Application / Domain / Infrastructure; all external integrations behind interfaces

## Non-negotiable invariants

- **Timezone:** UTC in DB, WIB (Asia/Jakarta) in UI. Proper tz conversion — never a hardcoded +7.
- **Booking state machine** (SPEC.md §6.3) is explicit and guarded; invalid transitions impossible. Slot double-booking prevented at the **DB level** (unique constraint), not just app logic.
- **Authorization on every endpoint** — role policies, no anonymous mutation endpoints.
- **No clinical/session notes anywhere in v1.** Minimal PII. Audit log for admin actions on bookings/payments/settings.
- **Code, comments, identifiers in English; all user-facing text in Indonesian via translation files.**

## Repo conventions

- Spec: `docs/SPEC.md` · Decisions log: `docs/DECISIONS.md` (create on first decision)
- Secrets only via env vars; keep `.env.example` current when adding config
- EF migrations checked in; seed = default admin (env credentials) + reference data
- Tests required for: booking state machine, slot generation, availability, authorization policies

## Session start checklist

1. Read this file → note current phase and blockers
2. Read `docs/SPEC.md` (at minimum the sections for the current phase) and `docs/DECISIONS.md` if it exists
3. State your plan for the session and confirm before writing code
