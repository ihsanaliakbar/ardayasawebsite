import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { PsychologistPatientListItem } from '../../core/patients/patient.models';
import {
  ProfileSavePayload,
  PsychologistProfile,
  PsychologistProfileForm,
} from '../../shared/psychologist-profile-form';

@Component({
  selector: 'app-psych-home',
  imports: [RouterLink, MatButtonModule, MatCardModule, PsychologistProfileForm, TranslatePipe],
  template: `
    <div class="page-container page">
      <h1 class="section-title">{{ 'psych.title' | translate }}</h1>

      <mat-card class="panel">
        <h2>{{ 'psych.profileTitle' | translate }}</h2>
        <p class="hint">{{ 'psych.profileHint' | translate }}</p>
        @if (errorKey(); as key) {
          <p class="error">{{ key | translate }}</p>
        }
        @if (saved()) {
          <p class="success">{{ 'admin.saved' | translate }}</p>
        }
        @if (profile(); as p) {
          <app-psychologist-profile-form
            [profile]="p"
            [busy]="busy()"
            (save)="save($event)"
            (photoSelected)="uploadPhoto($event)"
          />
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
        <p class="hint">{{ 'psych.comingSoon' | translate }}</p>
      </mat-card>
    </div>
  `,
  styles: `
    .page { padding-top: 32px; }
    .section-title { margin-bottom: 24px; }
    h2 { margin-top: 0; }
    .panel { padding: 24px; margin: 0 auto 24px; max-width: 720px; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .error { color: var(--mat-sys-error); }
    .success { color: var(--accent-gold); }
    .row {
      display: flex; justify-content: space-between; align-items: center; gap: 16px;
      padding: 12px 0; border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .row:last-child { border-bottom: none; }
    .badge {
      font: var(--mat-sys-label-medium); padding: 2px 10px; border-radius: 12px; margin-left: 8px;
      background: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class PsychHome implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly profile = signal<PsychologistProfile | null>(null);
  protected readonly patients = signal<PsychologistPatientListItem[]>([]);
  protected readonly busy = signal(false);
  protected readonly saved = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  ngOnInit(): void {
    this.http
      .get<PsychologistProfile>('/api/psychologist/profile')
      .subscribe((p) => this.profile.set(p));
    this.http
      .get<PsychologistPatientListItem[]>('/api/psychologist/patients')
      .subscribe((rows) => this.patients.set(rows));
  }

  protected save(payload: ProfileSavePayload): void {
    this.busy.set(true);
    this.saved.set(false);
    this.errorKey.set(null);
    this.http.put<PsychologistProfile>('/api/psychologist/profile', payload).subscribe({
      next: (updated) => {
        this.busy.set(false);
        this.saved.set(true);
        this.profile.set(updated);
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.errorKey.set(errorKeyFromResponse(err));
      },
    });
  }

  protected uploadPhoto(file: File): void {
    const form = new FormData();
    form.append('file', file);
    this.http.post<PsychologistProfile>('/api/psychologist/profile/photo', form).subscribe({
      next: (updated) => this.profile.set(updated),
      error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
    });
  }
}
