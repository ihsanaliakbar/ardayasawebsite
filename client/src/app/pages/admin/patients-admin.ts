import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe } from '@ngx-translate/core';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { errorKeyFromResponse } from '../../core/api-error';
import { AdminPatientListItem, PagedPatients } from '../../core/patients/patient.models';

interface PsychologistOption {
  id: string;
  displayName: string;
}

/**
 * Admin patient management: search patients and assign/unassign psychologists.
 * Deliberately shows only account basics + an intake-completed flag — intake
 * answers are visible solely to the patient's assigned psychologists.
 */
@Component({
  selector: 'app-patients-admin',
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, TranslatePipe,
  ],
  template: `
    <mat-card class="panel">
      <h2>{{ 'admin.patients.title' | translate }}</h2>
      <p class="hint">{{ 'admin.patients.privacyNote' | translate }}</p>

      <mat-form-field appearance="outline" class="search">
        <mat-label>{{ 'admin.patients.search' | translate }}</mat-label>
        <input matInput [formControl]="search" />
      </mat-form-field>

      @if (errorKey(); as key) {
        <p class="error">{{ key | translate }}</p>
      }
      @if (patients().length === 0) {
        <p class="hint">{{ 'admin.patients.empty' | translate }}</p>
      }

      @for (patient of patients(); track patient.userId) {
        <div class="row">
          <div class="who">
            <strong>{{ patient.fullName }}</strong>
            @if (patient.profileCompleted) {
              <span class="badge ok">{{ 'admin.patients.intakeComplete' | translate }}</span>
            } @else {
              <span class="badge pending">{{ 'admin.patients.intakeIncomplete' | translate }}</span>
            }
            <br />
            <span class="muted">{{ patient.email }} · {{ patient.whatsAppNumber ?? '—' }}</span>
          </div>

          <div class="assignments">
            @for (a of patient.assignments; track a.psychologistId) {
              <span class="chip">
                {{ a.displayName }}
                <button
                  type="button"
                  class="chip-remove"
                  [attr.aria-label]="'admin.patients.unassign' | translate"
                  (click)="unassign(patient, a.psychologistId)"
                ><mat-icon inline>close</mat-icon></button>
              </span>
            }
            <mat-form-field appearance="outline" class="assign-select" subscriptSizing="dynamic">
              <mat-label>{{ 'admin.patients.assign' | translate }}</mat-label>
              <mat-select #sel (selectionChange)="assign(patient, sel.value); sel.value = null">
                @for (p of assignablePsychologists(patient); track p.id) {
                  <mat-option [value]="p.id">{{ p.displayName }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
          </div>
        </div>
      }

      @if (totalCount() > pageSize) {
        <div class="pager">
          <button mat-stroked-button [disabled]="page() <= 1" (click)="goToPage(page() - 1)">‹</button>
          <span class="muted">{{ page() }} / {{ maxPage() }}</span>
          <button mat-stroked-button [disabled]="page() >= maxPage()" (click)="goToPage(page() + 1)">›</button>
        </div>
      }
    </mat-card>
  `,
  styles: `
    h2 { font: var(--mat-sys-title-large); margin-top: 0; }
    .panel { padding: 24px; margin-bottom: 24px; max-width: 860px; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .error { color: var(--mat-sys-error); font: var(--mat-sys-body-small); }
    .search { width: 100%; max-width: 360px; }
    .row {
      display: flex; justify-content: space-between; align-items: center; gap: 16px;
      padding: 14px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); flex-wrap: wrap;
    }
    .row:last-of-type { border-bottom: none; }
    .muted { color: var(--mat-sys-on-surface-variant); font: var(--mat-sys-body-small); }
    .badge { font: var(--mat-sys-label-medium); padding: 2px 10px; border-radius: 12px; margin-left: 8px; }
    .badge.ok { background: var(--mat-sys-primary-container); color: var(--mat-sys-on-primary-container); }
    .badge.pending { background: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface-variant); }
    .assignments { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .chip {
      display: inline-flex; align-items: center; gap: 4px;
      background: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-label-large); padding: 4px 6px 4px 12px; border-radius: 16px;
    }
    .chip-remove {
      border: none; background: none; cursor: pointer; color: inherit;
      display: inline-flex; align-items: center; padding: 2px;
    }
    .assign-select { width: 200px; }
    .pager { display: flex; align-items: center; gap: 12px; margin-top: 16px; }
  `,
})
export class PatientsAdmin implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly pageSize = 20;
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly patients = signal<AdminPatientListItem[]>([]);
  protected readonly psychologists = signal<PsychologistOption[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);
  protected readonly errorKey = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
    this.http
      .get<PsychologistOption[]>('/api/admin/psychologists')
      .subscribe((rows) => this.psychologists.set(rows));
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => this.goToPage(1));
  }

  protected maxPage(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize));
  }

  protected assignablePsychologists(patient: AdminPatientListItem): PsychologistOption[] {
    const assigned = new Set(patient.assignments.map((a) => a.psychologistId));
    return this.psychologists().filter((p) => !assigned.has(p.id));
  }

  protected goToPage(page: number): void {
    this.page.set(page);
    this.reload();
  }

  protected assign(patient: AdminPatientListItem, psychologistId: string | null): void {
    if (!psychologistId) return;
    this.errorKey.set(null);
    this.http
      .post(`/api/admin/patients/${patient.userId}/assignments`, { psychologistId })
      .subscribe({
        next: () => this.reload(),
        error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
      });
  }

  protected unassign(patient: AdminPatientListItem, psychologistId: string): void {
    this.errorKey.set(null);
    this.http
      .delete(`/api/admin/patients/${patient.userId}/assignments/${psychologistId}`)
      .subscribe({
        next: () => this.reload(),
        error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
      });
  }

  private reload(): void {
    const params = new HttpParams()
      .set('search', this.search.value)
      .set('page', this.page())
      .set('pageSize', this.pageSize);
    this.http.get<PagedPatients>('/api/admin/patients', { params }).subscribe((result) => {
      this.patients.set(result.items);
      this.totalCount.set(result.totalCount);
    });
  }
}
