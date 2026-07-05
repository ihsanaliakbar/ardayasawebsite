import { HttpClient } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { PsychologistPatientDetail } from '../../core/patients/patient.models';

/**
 * Read-only intake detail for one assigned patient. The API returns 404 unless
 * the logged-in psychologist has an assignment for this patient.
 */
@Component({
  selector: 'app-psych-patient-detail',
  imports: [RouterLink, MatCardModule, TranslatePipe, DatePipe],
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
      }
    </div>
  `,
  styles: `
    .page { padding-top: 32px; max-width: 720px; }
    h1 { font: var(--mat-sys-headline-medium); font-family: var(--font-display); }
    h2 { font: var(--mat-sys-title-large); margin-top: 0; }
    .back { color: var(--mat-sys-on-surface-variant); text-decoration: none; font-size: 0.9rem; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .panel { padding: 24px; margin-bottom: 24px; }
    dt { font: var(--mat-sys-label-large); color: var(--mat-sys-on-surface-variant); margin-top: 12px; }
    dt:first-child { margin-top: 0; }
    dd { margin: 2px 0 0; }
    .prewrap { white-space: pre-wrap; }
  `,
})
export class PsychPatientDetail implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly translate = inject(TranslateService);

  protected readonly detail = signal<PsychologistPatientDetail | null>(null);
  protected readonly notFound = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.http.get<PsychologistPatientDetail>(`/api/psychologist/patients/${id}`).subscribe({
      next: (d) => this.detail.set(d),
      error: () => this.notFound.set(true),
    });
  }

  protected yesNo(value: boolean | null): string {
    if (value === null) return '—';
    return this.translate.instant(value ? 'common.yes' : 'common.no');
  }
}
