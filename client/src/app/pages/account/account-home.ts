import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { ROLE_PATIENT } from '../../core/auth/auth.models';
import { AssignedPsychologist, PatientProfile } from '../../core/patients/patient.models';

@Component({
  selector: 'app-account-home',
  imports: [RouterLink, MatCardModule, MatButtonModule, MatIconModule, TranslatePipe],
  template: `
    <div class="page-container page">
    <h1 class="section-title">{{ 'account.title' | translate }}</h1>

    @if (isPatient && intakeIncomplete()) {
      <mat-card class="banner">
        <mat-icon>assignment_late</mat-icon>
        <div>
          <p class="banner-title">{{ 'account.intakeBanner.title' | translate }}</p>
          <p class="banner-text">{{ 'account.intakeBanner.text' | translate }}</p>
        </div>
        <a mat-flat-button routerLink="/akun/data-pribadi">
          {{ 'account.intakeBanner.cta' | translate }}
        </a>
      </mat-card>
    }

    @if (auth.user(); as user) {
      <mat-card class="profile">
        <p class="welcome">{{ 'account.welcome' | translate: { name: user.fullName } }}</p>
        <dl>
          <dt>{{ 'account.email' | translate }}</dt>
          <dd>{{ user.email }}</dd>
          <dt>{{ 'account.whatsapp' | translate }}</dt>
          <dd>{{ user.whatsAppNumber ?? '—' }}</dd>
        </dl>
        @if (isPatient) {
          <div class="actions">
            <a mat-stroked-button routerLink="/akun/data-pribadi">
              {{ 'account.editIntake' | translate }}
            </a>
            <a mat-stroked-button routerLink="/akun/booking">
              {{ 'account.myBookings' | translate }}
            </a>
          </div>
        }
      </mat-card>
    }

    @if (isPatient) {
      <mat-card class="profile">
        <h2>{{ 'account.myPsychologists.title' | translate }}</h2>
        @if (psychologists().length === 0) {
          <p class="muted">{{ 'account.myPsychologists.empty' | translate }}</p>
        }
        @for (p of psychologists(); track p.psychologistId) {
          <div class="psych-row">
            @if (p.photoUrl) {
              <img [src]="p.photoUrl" [alt]="p.displayName" />
            } @else {
              <mat-icon class="avatar-fallback">person</mat-icon>
            }
            <div>
              <strong>{{ p.displayName }}</strong>
              @if (p.title) { <span class="muted">, {{ p.title }}</span> }
              @if (p.specialization) { <br /><span class="muted">{{ p.specialization }}</span> }
            </div>
            @if (p.slug) {
              <span class="row-actions">
                <a mat-stroked-button [routerLink]="['/psikolog-kami', p.slug]">
                  {{ 'account.myPsychologists.viewProfile' | translate }}
                </a>
                <a mat-flat-button routerLink="/janji-temu" [queryParams]="{ psikolog: p.slug }">
                  {{ 'account.myPsychologists.book' | translate }}
                </a>
              </span>
            }
          </div>
        }
      </mat-card>
    }
    </div>
  `,
  styles: `
    .page { padding-top: 32px; }
    .section-title { margin-bottom: 24px; }
    h2 { font: var(--mat-sys-title-large); margin-top: 0; }
    .banner {
      padding: 20px 24px; max-width: 640px; margin: 0 auto 16px;
      display: flex; flex-direction: row; align-items: center; gap: 16px;
      border-left: 4px solid var(--accent-gold);
    }
    .banner mat-icon { color: var(--accent-gold); flex-shrink: 0; }
    .banner-title { font: var(--mat-sys-title-medium); margin: 0; }
    .banner-text { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; margin: 4px 0 0; }
    .banner a { margin-left: auto; flex-shrink: 0; }
    @media (max-width: 600px) {
      .banner { flex-direction: column; align-items: center; }
      .banner a { margin-left: 0; }
    }
    .profile { padding: 24px; max-width: 640px; margin: 0 auto 16px; }
    .welcome { font: var(--mat-sys-title-medium); margin-top: 0; }
    dt { font: var(--mat-sys-label-large); color: var(--mat-sys-on-surface-variant); }
    dd { margin: 0 0 12px; }
    .muted { color: var(--mat-sys-on-surface-variant); font: var(--mat-sys-body-small); }
    .psych-row {
      display: flex; align-items: center; gap: 16px; padding: 12px 0;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .psych-row:last-child { border-bottom: none; }
    .psych-row img { width: 48px; height: 48px; border-radius: 50%; object-fit: cover; }
    .psych-row .row-actions { margin-left: auto; display: flex; gap: 8px; flex-wrap: wrap; }
    .actions { display: flex; gap: 8px; flex-wrap: wrap; }
    .avatar-fallback { width: 48px; height: 48px; font-size: 48px; color: var(--mat-sys-outline); }
  `,
})
export class AccountHome implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);

  protected readonly psychologists = signal<AssignedPsychologist[]>([]);
  protected readonly intakeIncomplete = signal(false);

  protected get isPatient(): boolean {
    return this.auth.user()?.roles.includes(ROLE_PATIENT) ?? false;
  }

  ngOnInit(): void {
    if (!this.isPatient) return;
    this.http.get<AssignedPsychologist[]>('/api/me/psychologists')
      .subscribe((rows) => this.psychologists.set(rows));
    this.http.get<PatientProfile>('/api/me/patient-profile').subscribe({
      next: (p) => this.intakeIncomplete.set(!p.isComplete),
      error: () => this.intakeIncomplete.set(true), // 404 = never filled in
    });
  }
}
