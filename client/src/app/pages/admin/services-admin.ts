import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { IdrPipe } from '../../core/idr.pipe';

interface AdminService {
  id: string;
  categoryId: string;
  name: string;
  description: string | null;
  durationMinutes: number | null;
  offlinePrice: number | null;
  onlinePrice: number | null;
  sessionCount: number;
  notes: string | null;
  sortOrder: number;
  isActive: boolean;
}

interface AdminServiceCategory {
  id: string;
  name: string;
  description: string | null;
  sortOrder: number;
  services: AdminService[];
}

@Component({
  selector: 'app-services-admin',
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatCheckboxModule, MatIconModule, IdrPipe, TranslatePipe,
  ],
  template: `
    <h2>{{ 'admin.services.title' | translate }}</h2>
    @if (errorKey(); as key) {
      <p class="error">{{ key | translate }}</p>
    }

    <mat-card class="panel">
      <h3>{{ (editing() ? 'admin.services.editItem' : 'admin.services.newItem') | translate }}</h3>
      <form [formGroup]="form" (ngSubmit)="save()">
        <div class="two-col">
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.services.category' | translate }}</mat-label>
            <mat-select formControlName="categoryId">
              @for (category of categories(); track category.id) {
                <mat-option [value]="category.id">{{ category.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.services.name' | translate }}</mat-label>
            <input matInput formControlName="name" />
          </mat-form-field>
        </div>
        <div class="four-col">
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.services.duration' | translate }}</mat-label>
            <input matInput type="number" formControlName="durationMinutes" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.services.offlinePrice' | translate }}</mat-label>
            <input matInput type="number" formControlName="offlinePrice" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.services.onlinePrice' | translate }}</mat-label>
            <input matInput type="number" formControlName="onlinePrice" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.services.sessionCount' | translate }}</mat-label>
            <input matInput type="number" formControlName="sessionCount" />
          </mat-form-field>
        </div>
        <div class="two-col">
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.services.notes' | translate }}</mat-label>
            <input matInput formControlName="notes" />
          </mat-form-field>
          <div class="inline-controls">
            <mat-form-field appearance="outline" class="order">
              <mat-label>{{ 'admin.sortOrder' | translate }}</mat-label>
              <input matInput type="number" formControlName="sortOrder" />
            </mat-form-field>
            <mat-checkbox formControlName="isActive">{{ 'admin.services.active' | translate }}</mat-checkbox>
          </div>
        </div>
        <div class="actions">
          <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">{{ 'admin.save' | translate }}</button>
          @if (editing()) {
            <button mat-button type="button" (click)="reset()">{{ 'admin.cancel' | translate }}</button>
          }
        </div>
      </form>
    </mat-card>

    @for (category of categories(); track category.id) {
      <mat-card class="panel">
        <h3>{{ category.name }}</h3>
        @for (service of category.services; track service.id) {
          <div class="row" [class.inactive]="!service.isActive">
            <div>
              {{ service.name }}
              <span class="muted">
                @if (service.durationMinutes) { · {{ service.durationMinutes }} mnt }
                @if (service.sessionCount > 1) { · {{ service.sessionCount }} sesi }
                @if (service.offlinePrice != null) { · Offline {{ service.offlinePrice | idr }} }
                @if (service.onlinePrice != null) { · Online {{ service.onlinePrice | idr }} }
              </span>
              @if (!service.isActive) {
                <span class="badge">{{ 'admin.hidden' | translate }}</span>
              }
            </div>
            <div class="row-actions">
              <button mat-icon-button (click)="edit(service)"><mat-icon>edit</mat-icon></button>
              <button mat-icon-button (click)="remove(service)"><mat-icon>delete</mat-icon></button>
            </div>
          </div>
        }
      </mat-card>
    }
  `,
  styles: `
    h2 { margin: 0 0 12px; }
    h3 { margin: 0 0 8px; color: var(--accent-gold); }
    .panel { padding: 24px; margin-bottom: 20px; }
    form { display: flex; flex-direction: column; gap: 4px; }
    mat-form-field { width: 100%; }
    .two-col { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .four-col { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
    .inline-controls { display: flex; align-items: center; gap: 16px; }
    .order { width: 120px; }
    .actions { display: flex; gap: 8px; }
    .row { display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); font-size: 0.925rem; }
    .row:last-child { border-bottom: none; }
    .row.inactive { opacity: 0.55; }
    .muted { color: var(--mat-sys-on-surface-variant); font-size: 0.85rem; }
    .badge { font-size: 0.7rem; background: var(--mat-sys-surface-container-high); border-radius: 10px; padding: 2px 8px; margin-left: 8px; }
    .error { color: var(--mat-sys-error); }

    @media (max-width: 720px) {
      .two-col, .four-col { grid-template-columns: 1fr; }
    }
  `,
})
export class ServicesAdmin implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);

  protected readonly categories = signal<AdminServiceCategory[]>([]);
  protected readonly editing = signal<AdminService | null>(null);
  protected readonly busy = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly form = this.fb.group({
    categoryId: this.fb.control<string | null>(null, Validators.required),
    name: this.fb.nonNullable.control('', Validators.required),
    durationMinutes: this.fb.control<number | null>(null),
    offlinePrice: this.fb.control<number | null>(null),
    onlinePrice: this.fb.control<number | null>(null),
    sessionCount: this.fb.nonNullable.control(1),
    notes: this.fb.nonNullable.control(''),
    sortOrder: this.fb.nonNullable.control(1),
    isActive: this.fb.nonNullable.control(true),
  });

  ngOnInit(): void {
    this.reload();
  }

  protected save(): void {
    if (this.form.invalid) {
      return;
    }

    this.busy.set(true);
    this.errorKey.set(null);
    const value = this.form.getRawValue();
    const payload = { ...value, description: null, notes: value.notes || null };
    const current = this.editing();
    const request = current
      ? this.http.put(`/api/admin/services/${current.id}`, payload)
      : this.http.post('/api/admin/services', payload);

    request.subscribe({
      next: () => {
        this.busy.set(false);
        this.reset();
        this.reload();
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.errorKey.set(errorKeyFromResponse(err));
      },
    });
  }

  protected edit(service: AdminService): void {
    this.editing.set(service);
    this.form.patchValue({ ...service, notes: service.notes ?? '' });
  }

  protected remove(service: AdminService): void {
    this.http.delete(`/api/admin/services/${service.id}`).subscribe({
      next: () => this.reload(),
      error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
    });
  }

  protected reset(): void {
    this.editing.set(null);
    this.form.reset({
      categoryId: null, name: '', durationMinutes: null, offlinePrice: null,
      onlinePrice: null, sessionCount: 1, notes: '', sortOrder: 1, isActive: true,
    });
  }

  private reload(): void {
    this.http
      .get<AdminServiceCategory[]>('/api/admin/services')
      .subscribe((rows) => this.categories.set(rows));
  }
}
