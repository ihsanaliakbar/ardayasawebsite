import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { ClinicSettings } from '../../core/scheduling/scheduling.models';

@Component({
  selector: 'app-settings-admin',
  imports: [FormsModule, MatButtonModule, MatCardModule, TranslatePipe],
  template: `
    <mat-card class="panel">
      <h2>{{ 'adminSettings.title' | translate }}</h2>

      <label for="buffer">{{ 'adminSettings.slotBuffer' | translate }}</label>
      <div class="field">
        <input id="buffer" type="number" min="0" max="120" [(ngModel)]="buffer" />
        <span class="hint">{{ 'adminSettings.minutes' | translate }}</span>
      </div>
      <p class="hint">{{ 'adminSettings.slotBufferHint' | translate }}</p>

      @if (errorKey(); as key) {
        <p class="error">{{ key | translate }}</p>
      }
      @if (saved()) {
        <p class="success">{{ 'admin.saved' | translate }}</p>
      }

      <button mat-flat-button (click)="save()" [disabled]="busy()">{{ 'admin.save' | translate }}</button>
    </mat-card>
  `,
  styles: `
    h2 { font: var(--mat-sys-title-large); margin-top: 0; }
    .panel { padding: 24px; max-width: 560px; }
    label { font: var(--mat-sys-label-large); }
    .field { display: flex; align-items: center; gap: 10px; margin: 8px 0; }
    input {
      background: var(--mat-sys-surface-container-high); color: inherit; width: 100px;
      border: 1px solid var(--mat-sys-outline-variant); border-radius: 8px; padding: 8px 10px;
      font: inherit; color-scheme: dark;
    }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .error { color: var(--mat-sys-error); }
    .success { color: var(--accent-gold); }
    button { margin-top: 8px; }
  `,
})
export class SettingsAdmin implements OnInit {
  private readonly http = inject(HttpClient);

  protected buffer = 0;
  protected readonly busy = signal(false);
  protected readonly saved = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  ngOnInit(): void {
    this.http.get<ClinicSettings>('/api/admin/settings')
      .subscribe((s) => (this.buffer = s.slotBufferMinutes));
  }

  protected save(): void {
    this.busy.set(true);
    this.saved.set(false);
    this.errorKey.set(null);
    this.http.put<ClinicSettings>('/api/admin/settings', { slotBufferMinutes: this.buffer }).subscribe({
      next: (s) => {
        this.busy.set(false);
        this.saved.set(true);
        this.buffer = s.slotBufferMinutes;
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.errorKey.set(errorKeyFromResponse(err));
      },
    });
  }
}
