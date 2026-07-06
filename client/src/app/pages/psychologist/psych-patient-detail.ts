import { HttpClient } from '@angular/common/http';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import {
  LogbookEntry, PsychologistPatientDetail, SaveLogbookEntryRequest,
} from '../../core/patients/patient.models';

/**
 * Detail view for one assigned patient: read-only intake plus the counseling
 * logbook. The API returns 404 unless the logged-in psychologist has an
 * assignment for this patient. Logbook entries are shared reading among the
 * patient's assigned psychologists; only the author can edit; nobody deletes.
 */
@Component({
  selector: 'app-psych-patient-detail',
  imports: [
    ReactiveFormsModule, RouterLink, NgTemplateOutlet, MatCardModule, MatFormFieldModule,
    MatInputModule, MatRadioModule, MatButtonModule, TranslatePipe, DatePipe,
  ],
  template: `
    <div class="page-container page">
      <a routerLink="/psikolog" class="back">← {{ 'psych.title' | translate }}</a>

      @if (notFound()) {
        <h1>{{ 'psych.patientDetail.notFoundTitle' | translate }}</h1>
        <p class="hint">{{ 'psych.patientDetail.notFoundText' | translate }}</p>
      }

      @if (detail(); as d) {
        <h1>{{ d.profile?.fullName ?? d.accountName }}</h1>
        <p class="hint">
          {{ 'psych.patientDetail.assignedSince' | translate }}
          {{ d.assignedAtUtc | date: 'd MMMM y' }}
        </p>

        <mat-card class="panel">
          <h2>{{ 'psych.patientDetail.contactTitle' | translate }}</h2>
          <dl>
            <dt>{{ 'account.email' | translate }}</dt>
            <dd>{{ d.email }}</dd>
            <dt>{{ 'account.whatsapp' | translate }}</dt>
            <dd>{{ d.whatsAppNumber ?? '—' }}</dd>
          </dl>
        </mat-card>

        @if (d.profile; as p) {
          <mat-card class="panel">
            <h2>{{ 'patientProfile.title' | translate }}</h2>
            <dl>
              <dt>{{ 'patientProfile.fullName' | translate }}</dt>
              <dd>{{ p.fullName }}</dd>
              <dt>{{ 'patientProfile.birthPlaceDate' | translate }}</dt>
              <dd>
                {{ p.birthPlace ?? '—' }}@if (p.birthDate) {, {{ p.birthDate | date: 'd MMMM y' }}}
              </dd>
              <dt>{{ 'patientProfile.gender' | translate }}</dt>
              <dd>{{ p.gender ? ('enums.gender.' + p.gender | translate) : '—' }}</dd>
              <dt>{{ 'patientProfile.domicileAddress' | translate }}</dt>
              <dd>{{ p.domicileAddress ?? '—' }}</dd>
              <dt>{{ 'patientProfile.maritalStatus' | translate }}</dt>
              <dd>{{ p.maritalStatus ? ('enums.maritalStatus.' + p.maritalStatus | translate) : '—' }}</dd>
              <dt>{{ 'patientProfile.lastEducation' | translate }}</dt>
              <dd>{{ p.lastEducation ? ('enums.education.' + p.lastEducation | translate) : '—' }}</dd>
              <dt>{{ 'patientProfile.occupation' | translate }}</dt>
              <dd>{{ p.occupation ?? '—' }}</dd>
              <dt>{{ 'patientProfile.hasAccessedServices' | translate }}</dt>
              <dd>{{ yesNo(p.hasAccessedPsychologyServices) }}</dd>
              <dt>{{ 'patientProfile.hasPriorDiagnosis' | translate }}</dt>
              <dd>{{ yesNo(p.hasPriorDiagnosis) }}</dd>
              @if (p.hasPriorDiagnosis) {
                <dt>{{ 'patientProfile.priorDiagnosis' | translate }}</dt>
                <dd>{{ p.priorDiagnosis ?? '—' }}</dd>
              }
              <dt>{{ 'patientProfile.consultationConcerns' | translate }}</dt>
              <dd class="prewrap">{{ p.consultationConcerns ?? '—' }}</dd>
              <dt>{{ 'patientProfile.counselingExpectations' | translate }}</dt>
              <dd class="prewrap">{{ p.counselingExpectations ?? '—' }}</dd>
            </dl>
          </mat-card>
        } @else {
          <mat-card class="panel">
            <p class="hint">{{ 'psych.patientDetail.noIntake' | translate }}</p>
          </mat-card>
        }

        <div class="logbook-header">
          <h2>{{ 'psych.logbook.title' | translate }}</h2>
          @if (editingId() === null && !adding()) {
            <button mat-flat-button type="button" (click)="startAdd()">
              {{ 'psych.logbook.add' | translate }}
            </button>
          }
        </div>
        <p class="hint">{{ 'psych.logbook.hint' | translate }}</p>

        @if (adding()) {
          <mat-card class="panel">
            <h3>{{ 'psych.logbook.add' | translate }}</h3>
            <ng-container *ngTemplateOutlet="entryForm" />
          </mat-card>
        }

        @if (entries().length === 0 && !adding()) {
          <mat-card class="panel">
            <p class="hint">{{ 'psych.logbook.empty' | translate }}</p>
          </mat-card>
        }

        @for (entry of entries(); track entry.id) {
          <mat-card class="panel">
            @if (editingId() === entry.id) {
              <h3>{{ 'psych.logbook.session' | translate: { number: entry.sessionNumber } }}</h3>
              <ng-container *ngTemplateOutlet="entryForm" />
            } @else {
              <div class="entry-header">
                <h3>
                  {{ 'psych.logbook.session' | translate: { number: entry.sessionNumber } }}
                  — {{ entry.sessionDate | date: 'd MMMM y' }}
                </h3>
                @if (entry.isOwn) {
                  <button mat-stroked-button type="button" (click)="startEdit(entry)">
                    {{ 'psych.logbook.edit' | translate }}
                  </button>
                }
              </div>
              <p class="hint">
                {{ 'psych.logbook.author' | translate }} {{ entry.authorDisplayName }}
                @if (entry.updatedAtUtc) { · {{ 'psych.logbook.edited' | translate }} }
              </p>
              <dl>
                <dt>{{ 'psych.logbook.caseSummary' | translate }}</dt>
                <dd class="prewrap">{{ entry.caseSummary }}</dd>
                <dt>{{ 'psych.logbook.sessionActivities' | translate }}</dt>
                <dd class="prewrap">{{ entry.sessionActivities }}</dd>
                <dt>{{ 'psych.logbook.homework' | translate }}</dt>
                <dd class="prewrap">{{ entry.homework ?? '—' }}</dd>
                <dt>{{ 'psych.logbook.nextSessionPlan' | translate }}</dt>
                <dd class="prewrap">{{ entry.nextSessionPlan ?? '—' }}</dd>
                <dt>{{ 'psych.logbook.followUpNeeded' | translate }}</dt>
                <dd>{{ yesNo(entry.followUpNeeded) }}</dd>
              </dl>
            }
          </mat-card>
        }

        <ng-template #entryForm>
          <form [formGroup]="form" (ngSubmit)="save()">
            <div class="pair">
              <mat-form-field appearance="outline">
                <mat-label>{{ 'psych.logbook.sessionDate' | translate }}</mat-label>
                <input matInput type="date" formControlName="sessionDate" />
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>{{ 'psych.logbook.sessionNumber' | translate }}</mat-label>
                <input matInput type="number" min="1" formControlName="sessionNumber" />
              </mat-form-field>
            </div>

            <mat-form-field appearance="outline">
              <mat-label>{{ 'psych.logbook.caseSummary' | translate }}</mat-label>
              <textarea matInput rows="4" formControlName="caseSummary"></textarea>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>{{ 'psych.logbook.sessionActivities' | translate }}</mat-label>
              <textarea matInput rows="4" formControlName="sessionActivities"></textarea>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>{{ 'psych.logbook.homework' | translate }}</mat-label>
              <textarea matInput rows="2" formControlName="homework"></textarea>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>{{ 'psych.logbook.nextSessionPlan' | translate }}</mat-label>
              <textarea matInput rows="2" formControlName="nextSessionPlan"></textarea>
            </mat-form-field>

            <p class="question">{{ 'psych.logbook.followUpNeeded' | translate }}</p>
            <mat-radio-group formControlName="followUpNeeded">
              <mat-radio-button [value]="true">{{ 'common.yes' | translate }}</mat-radio-button>
              <mat-radio-button [value]="false">{{ 'common.no' | translate }}</mat-radio-button>
            </mat-radio-group>

            @if (errorKey(); as key) {
              <p class="error">{{ key | translate }}</p>
            }
            <div class="actions">
              <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">
                {{ 'psych.logbook.save' | translate }}
              </button>
              <button mat-stroked-button type="button" (click)="cancelForm()">
                {{ 'psych.logbook.cancel' | translate }}
              </button>
            </div>
          </form>
        </ng-template>
      }
    </div>
  `,
  styles: `
    .page { padding-top: 32px; max-width: 720px; }
    h1 { font: var(--mat-sys-headline-medium); font-family: var(--font-display); }
    h2 { font: var(--mat-sys-title-large); margin-top: 0; }
    h3 { font: var(--mat-sys-title-medium); margin: 0; }
    .back { color: var(--mat-sys-on-surface-variant); text-decoration: none; font-size: 0.9rem; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .panel { padding: 24px; margin-bottom: 24px; }
    dt { font: var(--mat-sys-label-large); color: var(--mat-sys-on-surface-variant); margin-top: 12px; }
    dt:first-child { margin-top: 0; }
    dd { margin: 2px 0 0; }
    .prewrap { white-space: pre-wrap; }
    .logbook-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin-top: 8px; }
    .logbook-header h2 { margin: 0; }
    .entry-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
    form { display: flex; flex-direction: column; gap: 4px; margin-top: 16px; }
    .pair { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    @media (max-width: 600px) { .pair { grid-template-columns: 1fr; } }
    .question { margin: 4px 0 4px; font: var(--mat-sys-body-medium); }
    mat-radio-group { display: flex; gap: 24px; margin-bottom: 16px; }
    .error { color: var(--mat-sys-error); font: var(--mat-sys-body-small); margin: 0 0 8px; }
    .actions { display: flex; gap: 12px; }
  `,
})
export class PsychPatientDetail implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly translate = inject(TranslateService);
  private readonly fb = inject(FormBuilder);

  protected readonly detail = signal<PsychologistPatientDetail | null>(null);
  protected readonly notFound = signal(false);
  protected readonly entries = signal<LogbookEntry[]>([]);
  protected readonly adding = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly form = this.fb.group({
    sessionDate: this.fb.nonNullable.control('', Validators.required),
    sessionNumber: this.fb.nonNullable.control(1, [Validators.required, Validators.min(1)]),
    caseSummary: this.fb.nonNullable.control('', Validators.required),
    sessionActivities: this.fb.nonNullable.control('', Validators.required),
    homework: this.fb.control<string | null>(null),
    nextSessionPlan: this.fb.control<string | null>(null),
    followUpNeeded: this.fb.nonNullable.control<boolean>(false, Validators.required),
  });

  private patientId = '';

  ngOnInit(): void {
    this.patientId = this.route.snapshot.paramMap.get('id') ?? '';
    this.http.get<PsychologistPatientDetail>(`/api/psychologist/patients/${this.patientId}`).subscribe({
      next: (d) => {
        this.detail.set(d);
        this.loadEntries();
      },
      error: () => this.notFound.set(true),
    });
  }

  protected startAdd(): void {
    const nextNumber = this.entries().reduce((max, e) => Math.max(max, e.sessionNumber), 0) + 1;
    this.form.reset({
      sessionDate: new Date().toISOString().slice(0, 10),
      sessionNumber: nextNumber,
      caseSummary: '',
      sessionActivities: '',
      homework: null,
      nextSessionPlan: null,
      followUpNeeded: false,
    });
    this.errorKey.set(null);
    this.adding.set(true);
    this.editingId.set(null);
  }

  protected startEdit(entry: LogbookEntry): void {
    this.form.reset({
      sessionDate: entry.sessionDate,
      sessionNumber: entry.sessionNumber,
      caseSummary: entry.caseSummary,
      sessionActivities: entry.sessionActivities,
      homework: entry.homework,
      nextSessionPlan: entry.nextSessionPlan,
      followUpNeeded: entry.followUpNeeded,
    });
    this.errorKey.set(null);
    this.adding.set(false);
    this.editingId.set(entry.id);
  }

  protected cancelForm(): void {
    this.adding.set(false);
    this.editingId.set(null);
    this.errorKey.set(null);
  }

  protected save(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    this.errorKey.set(null);
    const value = this.form.getRawValue();
    const payload: SaveLogbookEntryRequest = { ...value, sessionNumber: Number(value.sessionNumber) };
    const editingId = this.editingId();
    const base = `/api/psychologist/patients/${this.patientId}/logbook`;
    const request$ = editingId === null
      ? this.http.post<LogbookEntry>(base, payload)
      : this.http.put<LogbookEntry>(`${base}/${editingId}`, payload);
    request$.subscribe({
      next: () => {
        this.busy.set(false);
        this.cancelForm();
        this.loadEntries();
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.errorKey.set(errorKeyFromResponse(err));
      },
    });
  }

  protected yesNo(value: boolean | null): string {
    if (value === null) return '—';
    return this.translate.instant(value ? 'common.yes' : 'common.no');
  }

  private loadEntries(): void {
    this.http.get<LogbookEntry[]>(`/api/psychologist/patients/${this.patientId}/logbook`).subscribe({
      next: (entries) => this.entries.set(entries),
      error: () => this.entries.set([]),
    });
  }
}
