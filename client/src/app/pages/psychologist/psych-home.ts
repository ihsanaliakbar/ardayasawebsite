import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { PsychologistPatientListItem } from '../../core/patients/patient.models';
import {
  AvailabilityRule,
  AvailabilityView,
  DayOfWeek,
  StaffBooking,
  WEEK_DAYS,
} from '../../core/scheduling/scheduling.models';
import { WibCalendarDatePipe, WibDatePipe, WibTimePipe, formatWibTimeOnly } from '../../core/scheduling/wib';
import { PsychologistProfile } from '../../shared/psychologist-profile-form';

@Component({
  selector: 'app-psych-home',
  imports: [
    RouterLink, FormsModule, MatButtonModule, MatCardModule, MatIconModule,
    TranslatePipe, WibCalendarDatePipe, WibDatePipe, WibTimePipe,
  ],
  template: `
    <div class="page-container page">
      <h1 class="section-title">{{ 'psych.title' | translate }}</h1>

      <mat-card class="panel">
        <h2>{{ 'psych.profileTitle' | translate }}</h2>
        <p class="hint">{{ 'psych.profileHint' | translate }}</p>
        @if (profile(); as p) {
          <div class="profile-header">
            @if (p.photoUrl; as url) {
              <img [src]="url" alt="" class="photo" />
            } @else {
              <div class="photo placeholder"><mat-icon>person</mat-icon></div>
            }
            <div>
              <strong class="name">{{ p.displayName }}</strong>
              @if (p.title) {
                <br /><span class="hint">{{ p.title }}</span>
              }
            </div>
          </div>

          <dl class="fields">
            <dt>{{ 'profileForm.specialization' | translate }}</dt>
            <dd>{{ p.specialization ?? '—' }}</dd>

            <dt>{{ 'profileForm.education' | translate }}</dt>
            <dd>
              @if (p.education.length === 0) { — } @else {
                <ul>
                  @for (line of p.education; track line) {
                    <li>{{ line }}</li>
                  }
                </ul>
              }
            </dd>

            <dt>{{ 'profileForm.expertise' | translate }}</dt>
            <dd>
              @if (p.expertise.length === 0) { — } @else {
                <ul>
                  @for (line of p.expertise; track line) {
                    <li>{{ line }}</li>
                  }
                </ul>
              }
            </dd>

            <dt>{{ 'profileForm.bio' | translate }}</dt>
            <dd>{{ p.bio ?? '—' }}</dd>

            <dt>{{ 'profileForm.schedule' | translate }}</dt>
            <dd>
              @if (p.scheduleLines.length === 0) { — } @else {
                <ul>
                  @for (line of p.scheduleLines; track line) {
                    <li>{{ line }}</li>
                  }
                </ul>
              }
            </dd>
          </dl>
        }
      </mat-card>

      <mat-card class="panel">
        <h2>{{ 'psych.patients.title' | translate }}</h2>
        <p class="hint">{{ 'psych.patients.hint' | translate }}</p>
        @if (patients().length === 0) {
          <p class="hint">{{ 'psych.patients.empty' | translate }}</p>
        }
        @for (patient of patients(); track patient.patientUserId) {
          <div class="row">
            <div>
              <strong>{{ patient.fullName }}</strong>
              @if (!patient.profileCompleted) {
                <span class="badge">{{ 'psych.patients.intakeIncomplete' | translate }}</span>
              }
              <br />
              <span class="hint">{{ patient.whatsAppNumber ?? '—' }}</span>
            </div>
            <a mat-stroked-button [routerLink]="['/psikolog/pasien', patient.patientUserId]">
              {{ 'psych.patients.view' | translate }}
            </a>
          </div>
        }
      </mat-card>

      <mat-card class="panel">
        <h2>{{ 'psych.schedule.title' | translate }}</h2>
        <p class="hint">{{ 'psych.schedule.hint' | translate }}</p>
        @if (availability(); as view) {
          @if (view.rules.length === 0) {
            <p class="hint">{{ 'psych.schedule.empty' | translate }}</p>
          }
          <dl class="fields">
            @for (day of weekDays; track day) {
              @if (rulesFor(view, day).length > 0) {
                <dt>{{ 'enums.day.' + day | translate }}</dt>
                <dd>
                  @for (rule of rulesFor(view, day); track rule.id; let last = $last) {
                    {{ formatTime(rule.startTime) }}–{{ formatTime(rule.endTime) }} WIB@if (!last) {, }
                  }
                </dd>
              }
            }
          </dl>
          @if (view.exceptions.length > 0) {
            <h3>{{ 'psych.schedule.exceptions' | translate }}</h3>
            <ul>
              @for (x of view.exceptions; track x.id) {
                <li>
                  {{ x.date | wibCalendarDate }} —
                  {{ (x.kind === 'Block' ? 'adminSchedule.exceptions.block' : 'adminSchedule.exceptions.extra') | translate }}
                  @if (x.startTime && x.endTime) {
                    ({{ formatTime(x.startTime) }}–{{ formatTime(x.endTime) }} WIB)
                  }
                </li>
              }
            </ul>
          }
        }
      </mat-card>

      <mat-card class="panel">
        <h2>{{ 'psych.bookings.title' | translate }}</h2>
        <p class="hint">{{ 'psych.bookings.hint' | translate }}</p>
        @if (zoomErrorKey(); as key) {
          <p class="error">{{ key | translate }}</p>
        }
        @for (b of bookings(); track b.id) {
          <div class="booking-row">
            <div>
              <strong>{{ b.startUtc | wibDate }} · {{ b.startUtc | wibTime }}–{{ b.endUtc | wibTime }} WIB</strong>
              <span class="badge">{{ 'enums.bookingStatus.' + b.status | translate }}</span>
              <br />
              <span class="hint">{{ b.patientName }} · {{ b.serviceName }} · {{ 'enums.bookingMode.' + b.mode | translate }}</span>
            </div>
            @if (b.mode === 'Online') {
              <div class="zoom">
                <input type="url" [placeholder]="'psych.bookings.zoomPlaceholder' | translate" [(ngModel)]="zoomDrafts[b.id]" />
                <button mat-stroked-button (click)="saveZoom(b)" [disabled]="!zoomDrafts[b.id]">
                  {{ 'psych.bookings.saveZoom' | translate }}
                </button>
              </div>
            }
          </div>
        } @empty {
          <p class="hint">{{ 'psych.bookings.empty' | translate }}</p>
        }
      </mat-card>
    </div>
  `,
  styles: `
    .page { padding-top: 32px; }
    .section-title { margin-bottom: 24px; }
    h2 { margin-top: 0; }
    .panel { padding: 24px; margin: 0 auto 24px; max-width: 720px; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .profile-header { display: flex; align-items: center; gap: 20px; margin: 16px 0; }
    .photo { width: 96px; height: 114px; object-fit: cover; border-radius: 12px; }
    .photo.placeholder { display: grid; place-items: center; background: var(--mat-sys-surface-container-high); }
    .name { font-size: 1.1rem; }
    .fields { display: grid; grid-template-columns: auto 1fr; gap: 8px 24px; margin: 0; }
    .fields dt { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; padding-top: 1px; }
    .fields dd { margin: 0; }
    .fields ul { margin: 0; padding-left: 18px; }
    .row {
      display: flex; justify-content: space-between; align-items: center; gap: 16px;
      padding: 12px 0; border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .row:last-child { border-bottom: none; }
    .badge {
      font: var(--mat-sys-label-medium); padding: 2px 10px; border-radius: 12px; margin-left: 8px;
      background: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface-variant);
    }
    h3 { font: var(--mat-sys-title-small); margin: 16px 0 4px; }
    .error { color: var(--mat-sys-error); font-size: 0.9rem; }
    .booking-row {
      display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 12px;
      padding: 12px 0; border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .booking-row:last-of-type { border-bottom: none; }
    .zoom { display: flex; gap: 8px; align-items: center; }
    .zoom input {
      background: var(--mat-sys-surface-container-high); color: inherit; width: 200px;
      border: 1px solid var(--mat-sys-outline-variant); border-radius: 8px; padding: 8px 10px;
      font: inherit;
    }
  `,
})
export class PsychHome implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly weekDays = WEEK_DAYS;
  protected readonly profile = signal<PsychologistProfile | null>(null);
  protected readonly patients = signal<PsychologistPatientListItem[]>([]);
  protected readonly availability = signal<AvailabilityView | null>(null);
  protected readonly bookings = signal<StaffBooking[]>([]);
  protected readonly zoomErrorKey = signal<string | null>(null);
  protected zoomDrafts: Record<string, string> = {};

  ngOnInit(): void {
    this.http
      .get<PsychologistProfile>('/api/psychologist/profile')
      .subscribe((p) => this.profile.set(p));
    this.http
      .get<PsychologistPatientListItem[]>('/api/psychologist/patients')
      .subscribe((rows) => this.patients.set(rows));
    this.http
      .get<AvailabilityView>('/api/psychologist/availability')
      .subscribe((view) => this.availability.set(view));
    this.reloadBookings();
  }

  protected rulesFor(view: AvailabilityView, day: DayOfWeek): AvailabilityRule[] {
    return view.rules.filter((r) => r.dayOfWeek === day);
  }

  protected formatTime = formatWibTimeOnly;

  protected saveZoom(booking: StaffBooking): void {
    this.zoomErrorKey.set(null);
    this.http
      .put<StaffBooking>(`/api/psychologist/bookings/${booking.id}/zoom-link`, { zoomLink: this.zoomDrafts[booking.id] })
      .subscribe({
        next: () => this.reloadBookings(),
        error: (err: unknown) => this.zoomErrorKey.set(errorKeyFromResponse(err)),
      });
  }

  private reloadBookings(): void {
    this.http.get<StaffBooking[]>('/api/psychologist/bookings').subscribe((rows) => {
      this.bookings.set(rows);
      this.zoomDrafts = Object.fromEntries(rows.map((b) => [b.id, b.zoomLink ?? '']));
    });
  }
}
