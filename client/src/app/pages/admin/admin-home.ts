import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';

interface PsychologistRow {
  id: string;
  userId: string;
  displayName: string;
  title: string | null;
  email: string;
  isActive: boolean;
  invitationAccepted: boolean;
}

@Component({
  selector: 'app-admin-home',
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, TranslatePipe,
  ],
  template: `
    <h1>{{ 'admin.title' | translate }}</h1>

    <mat-card class="panel">
      <h2>{{ 'admin.psychologists.inviteTitle' | translate }}</h2>
      <form [formGroup]="inviteForm" (ngSubmit)="invite()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'admin.psychologists.fullName' | translate }}</mat-label>
          <input matInput formControlName="fullName" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'admin.psychologists.email' | translate }}</mat-label>
          <input matInput type="email" formControlName="email" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'admin.psychologists.professionalTitle' | translate }}</mat-label>
          <input matInput formControlName="title" placeholder="M.Psi., Psikolog" />
        </mat-form-field>
        @if (errorKey(); as key) {
          <p class="error">{{ key | translate }}</p>
        }
        @if (notice(); as key) {
          <p class="success">{{ key | translate }}</p>
        }
        <button mat-flat-button type="submit" [disabled]="inviteForm.invalid || busy()">
          {{ 'admin.psychologists.inviteSubmit' | translate }}
        </button>
      </form>
    </mat-card>

    <mat-card class="panel">
      <h2>{{ 'admin.psychologists.title' | translate }}</h2>
      @if (psychologists().length === 0) {
        <p>{{ 'admin.psychologists.empty' | translate }}</p>
      }
      @for (p of psychologists(); track p.id) {
        <div class="row">
          <div>
            <strong>{{ p.displayName }}</strong>
            @if (p.title) { <span class="muted"> — {{ p.title }}</span> }
            <br />
            <span class="muted">{{ p.email }}</span>
          </div>
          <div class="row-actions">
            @if (p.invitationAccepted) {
              <span class="badge ok">{{ 'admin.psychologists.statusAccepted' | translate }}</span>
            } @else {
              <span class="badge pending">{{ 'admin.psychologists.statusPending' | translate }}</span>
              <button mat-stroked-button (click)="resend(p)">
                {{ 'admin.psychologists.resend' | translate }}
              </button>
            }
          </div>
        </div>
      }
    </mat-card>
  `,
  styles: `
    h1 { font: var(--mat-sys-headline-medium); }
    h2 { font: var(--mat-sys-title-large); margin-top: 0; }
    .panel { padding: 24px; margin-bottom: 24px; max-width: 640px; }
    form { display: flex; flex-direction: column; gap: 4px; }
    .error { color: var(--mat-sys-error); font: var(--mat-sys-body-small); margin: 0 0 8px; }
    .success { color: var(--mat-sys-primary); font: var(--mat-sys-body-small); margin: 0 0 8px; }
    .row { display: flex; justify-content: space-between; align-items: center; gap: 16px; padding: 12px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); }
    .row:last-child { border-bottom: none; }
    .row-actions { display: flex; align-items: center; gap: 8px; }
    .muted { color: var(--mat-sys-on-surface-variant); font: var(--mat-sys-body-small); }
    .badge { font: var(--mat-sys-label-medium); padding: 4px 10px; border-radius: 12px; }
    .badge.ok { background: var(--mat-sys-primary-container); color: var(--mat-sys-on-primary-container); }
    .badge.pending { background: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface-variant); }
  `,
})
export class AdminHome implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);

  protected readonly psychologists = signal<PsychologistRow[]>([]);
  protected readonly busy = signal(false);
  protected readonly errorKey = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  protected readonly inviteForm = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    title: [''],
  });

  ngOnInit(): void {
    this.reload();
  }

  protected invite(): void {
    if (this.inviteForm.invalid) return;
    this.busy.set(true);
    this.errorKey.set(null);
    this.notice.set(null);
    const { fullName, email, title } = this.inviteForm.getRawValue();
    this.http.post('/api/admin/psychologists', { fullName, email, title: title || null }).subscribe({
      next: () => {
        this.busy.set(false);
        this.notice.set('admin.psychologists.invited');
        this.inviteForm.reset();
        this.reload();
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.errorKey.set(errorKeyFromResponse(err));
      },
    });
  }

  protected resend(p: PsychologistRow): void {
    this.errorKey.set(null);
    this.notice.set(null);
    this.http.post(`/api/admin/psychologists/${p.id}/resend-invitation`, null).subscribe({
      next: () => this.notice.set('admin.psychologists.resent'),
      error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
    });
  }

  private reload(): void {
    this.http.get<PsychologistRow[]>('/api/admin/psychologists')
      .subscribe((rows) => this.psychologists.set(rows));
  }
}
