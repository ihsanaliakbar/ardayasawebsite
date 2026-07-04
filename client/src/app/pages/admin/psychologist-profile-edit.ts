import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { switchMap } from 'rxjs';
import { errorKeyFromResponse } from '../../core/api-error';
import {
  ProfileSavePayload,
  PsychologistProfile,
  PsychologistProfileForm,
} from '../../shared/psychologist-profile-form';

@Component({
  selector: 'app-psychologist-profile-edit',
  imports: [RouterLink, MatCardModule, MatIconModule, PsychologistProfileForm, TranslatePipe],
  template: `
    <a routerLink="/admin" class="back"><mat-icon inline>arrow_back</mat-icon> {{ 'admin.psychologists.title' | translate }}</a>
    @if (current(); as p) {
      <mat-card class="panel">
        <h2>{{ 'admin.psychologists.editProfileTitle' | translate: { name: p.displayName } }}</h2>
        @if (errorKey(); as key) {
          <p class="error">{{ key | translate }}</p>
        }
        @if (saved()) {
          <p class="success">{{ 'admin.saved' | translate }}</p>
        }
        <app-psychologist-profile-form
          [profile]="p"
          [adminMode]="true"
          [busy]="busy()"
          (save)="save(p, $event)"
          (photoSelected)="uploadPhoto(p, $event)"
        />
      </mat-card>
    }
  `,
  styles: `
    .back { color: var(--mat-sys-on-surface-variant); text-decoration: none; font-size: 0.9rem; }
    .panel { padding: 24px; margin-top: 16px; max-width: 720px; }
    h2 { margin-top: 0; }
    .error { color: var(--mat-sys-error); }
    .success { color: var(--accent-gold); }
  `,
})
export class PsychologistProfileEdit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);

  protected readonly busy = signal(false);
  protected readonly saved = signal(false);
  protected readonly errorKey = signal<string | null>(null);
  protected readonly override = signal<PsychologistProfile | null>(null);

  private readonly loaded = toSignal(
    this.route.paramMap.pipe(
      switchMap((params) =>
        this.http.get<PsychologistProfile>(`/api/admin/psychologists/${params.get('id')}/profile`),
      ),
    ),
  );

  protected current(): PsychologistProfile | null {
    return this.override() ?? this.loaded() ?? null;
  }

  protected save(profile: PsychologistProfile, payload: ProfileSavePayload): void {
    this.busy.set(true);
    this.saved.set(false);
    this.errorKey.set(null);
    this.http
      .put<PsychologistProfile>(`/api/admin/psychologists/${profile.id}/profile`, payload)
      .subscribe({
        next: (updated) => {
          this.busy.set(false);
          this.saved.set(true);
          this.override.set(updated);
        },
        error: (err: unknown) => {
          this.busy.set(false);
          this.errorKey.set(errorKeyFromResponse(err));
        },
      });
  }

  protected uploadPhoto(profile: PsychologistProfile, file: File): void {
    const form = new FormData();
    form.append('file', file);
    this.http
      .post<PsychologistProfile>(`/api/admin/psychologists/${profile.id}/profile/photo`, form)
      .subscribe({
        next: (updated) => this.override.set(updated),
        error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
      });
  }
}
