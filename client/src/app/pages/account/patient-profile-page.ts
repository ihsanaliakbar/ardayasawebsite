import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import {
  EDUCATION_LEVELS, GENDERS, MARITAL_STATUSES, PatientProfile,
} from '../../core/patients/patient.models';

/**
 * "Data Pribadi" — the patient intake form. Answers are visible only to the
 * patient and the psychologists assigned to them.
 */
@Component({
  selector: 'app-patient-profile-page',
  imports: [
    ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatRadioModule, MatButtonModule, TranslatePipe,
  ],
  template: `
    <div class="page-container page">
      <a routerLink="/akun" class="back">← {{ 'account.title' | translate }}</a>
      <h1>{{ 'patientProfile.title' | translate }}</h1>
      <p class="hint">{{ 'patientProfile.privacyNote' | translate }}</p>

      <mat-card class="panel">
        <form [formGroup]="form" (ngSubmit)="save()">
          <mat-form-field appearance="outline">
            <mat-label>{{ 'patientProfile.fullName' | translate }}</mat-label>
            <input matInput formControlName="fullName" />
          </mat-form-field>

          <div class="pair">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'patientProfile.birthPlace' | translate }}</mat-label>
              <input matInput formControlName="birthPlace" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>{{ 'patientProfile.birthDate' | translate }}</mat-label>
              <input matInput type="date" formControlName="birthDate" />
            </mat-form-field>
          </div>

          <mat-form-field appearance="outline">
            <mat-label>{{ 'patientProfile.gender' | translate }}</mat-label>
            <mat-select formControlName="gender">
              @for (g of genders; track g) {
                <mat-option [value]="g">{{ 'enums.gender.' + g | translate }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>{{ 'patientProfile.domicileAddress' | translate }}</mat-label>
            <textarea matInput rows="2" formControlName="domicileAddress"></textarea>
          </mat-form-field>

          <div class="pair">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'patientProfile.maritalStatus' | translate }}</mat-label>
              <mat-select formControlName="maritalStatus">
                @for (s of maritalStatuses; track s) {
                  <mat-option [value]="s">{{ 'enums.maritalStatus.' + s | translate }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>{{ 'patientProfile.lastEducation' | translate }}</mat-label>
              <mat-select formControlName="lastEducation">
                @for (level of educationLevels; track level) {
                  <mat-option [value]="level">{{ 'enums.education.' + level | translate }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
          </div>

          <mat-form-field appearance="outline">
            <mat-label>{{ 'patientProfile.occupation' | translate }}</mat-label>
            <input matInput formControlName="occupation" />
          </mat-form-field>

          <p class="question">{{ 'patientProfile.hasAccessedServices' | translate }}</p>
          <mat-radio-group formControlName="hasAccessedPsychologyServices">
            <mat-radio-button [value]="true">{{ 'common.yes' | translate }}</mat-radio-button>
            <mat-radio-button [value]="false">{{ 'common.no' | translate }}</mat-radio-button>
          </mat-radio-group>

          <p class="question">{{ 'patientProfile.hasPriorDiagnosis' | translate }}</p>
          <mat-radio-group formControlName="hasPriorDiagnosis">
            <mat-radio-button [value]="true">{{ 'common.yes' | translate }}</mat-radio-button>
            <mat-radio-button [value]="false">{{ 'common.no' | translate }}</mat-radio-button>
          </mat-radio-group>

          @if (form.controls.hasPriorDiagnosis.value === true) {
            <mat-form-field appearance="outline">
              <mat-label>{{ 'patientProfile.priorDiagnosis' | translate }}</mat-label>
              <textarea matInput rows="2" formControlName="priorDiagnosis"></textarea>
            </mat-form-field>
          }

          <mat-form-field appearance="outline">
            <mat-label>{{ 'patientProfile.consultationConcerns' | translate }}</mat-label>
            <textarea matInput rows="4" formControlName="consultationConcerns"></textarea>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>{{ 'patientProfile.counselingExpectations' | translate }}</mat-label>
            <textarea matInput rows="4" formControlName="counselingExpectations"></textarea>
          </mat-form-field>

          @if (errorKey(); as key) {
            <p class="error">{{ key | translate }}</p>
          }
          @if (saved()) {
            <p class="success">{{ 'patientProfile.saved' | translate }}</p>
          }
          <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">
            {{ 'patientProfile.submit' | translate }}
          </button>
        </form>
      </mat-card>
    </div>
  `,
  styles: `
    .page { padding-top: 32px; max-width: 720px; }
    h1 { font: var(--mat-sys-headline-medium); font-family: var(--font-display); }
    .back { color: var(--mat-sys-on-surface-variant); text-decoration: none; font-size: 0.9rem; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .panel { padding: 24px; margin-bottom: 32px; }
    form { display: flex; flex-direction: column; gap: 4px; }
    .pair { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    @media (max-width: 600px) { .pair { grid-template-columns: 1fr; } }
    .question { margin: 4px 0 4px; font: var(--mat-sys-body-medium); }
    mat-radio-group { display: flex; gap: 24px; margin-bottom: 16px; }
    .error { color: var(--mat-sys-error); font: var(--mat-sys-body-small); margin: 0 0 8px; }
    .success { color: var(--accent-gold); font: var(--mat-sys-body-small); margin: 0 0 8px; }
    button[type='submit'] { align-self: flex-start; }
  `,
})
export class PatientProfilePage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);

  protected readonly genders = GENDERS;
  protected readonly maritalStatuses = MARITAL_STATUSES;
  protected readonly educationLevels = EDUCATION_LEVELS;

  protected readonly busy = signal(false);
  protected readonly saved = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly form = this.fb.group({
    fullName: this.fb.nonNullable.control('', Validators.required),
    birthPlace: this.fb.control<string | null>(null),
    birthDate: this.fb.control<string | null>(null),
    gender: this.fb.control<string | null>(null),
    domicileAddress: this.fb.control<string | null>(null),
    maritalStatus: this.fb.control<string | null>(null),
    lastEducation: this.fb.control<string | null>(null),
    occupation: this.fb.control<string | null>(null),
    hasAccessedPsychologyServices: this.fb.control<boolean | null>(null),
    hasPriorDiagnosis: this.fb.control<boolean | null>(null),
    priorDiagnosis: this.fb.control<string | null>(null),
    consultationConcerns: this.fb.control<string | null>(null),
    counselingExpectations: this.fb.control<string | null>(null),
  });

  ngOnInit(): void {
    this.http.get<PatientProfile>('/api/me/patient-profile').subscribe({
      next: (p) => this.form.patchValue(p),
      error: () => undefined, // 404 = not filled in yet; start with an empty form
    });
  }

  protected save(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    this.saved.set(false);
    this.errorKey.set(null);
    const value = this.form.getRawValue();
    this.http
      .put<PatientProfile>('/api/me/patient-profile', {
        ...value,
        birthDate: value.birthDate || null, // empty date input -> ''
      })
      .subscribe({
        next: (p) => {
          this.busy.set(false);
          this.saved.set(true);
          this.form.patchValue(p);
        },
        error: (err: unknown) => {
          this.busy.set(false);
          this.errorKey.set(errorKeyFromResponse(err));
        },
      });
  }
}
