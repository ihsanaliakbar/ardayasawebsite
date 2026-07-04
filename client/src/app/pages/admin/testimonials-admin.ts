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

interface AdminTestimonial {
  id: string;
  authorName: string;
  roleLabel: string | null;
  content: string;
  rating: number;
  psychologistId: string | null;
  isPublished: boolean;
  sortOrder: number;
}

interface PsychologistOption {
  id: string;
  displayName: string;
}

@Component({
  selector: 'app-testimonials-admin',
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatCheckboxModule, MatIconModule, TranslatePipe,
  ],
  template: `
    <h2>{{ 'admin.testimonials.title' | translate }}</h2>
    @if (errorKey(); as key) {
      <p class="error">{{ key | translate }}</p>
    }

    <mat-card class="panel">
      <h3>{{ (editing() ? 'admin.testimonials.editItem' : 'admin.testimonials.newItem') | translate }}</h3>
      <form [formGroup]="form" (ngSubmit)="save()">
        <div class="two-col">
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.testimonials.author' | translate }}</mat-label>
            <input matInput formControlName="authorName" placeholder="Alya D." />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.testimonials.role' | translate }}</mat-label>
            <input matInput formControlName="roleLabel" placeholder="Klien Konseling Individu" />
          </mat-form-field>
        </div>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'admin.testimonials.content' | translate }}</mat-label>
          <textarea matInput formControlName="content" rows="3"></textarea>
        </mat-form-field>
        <div class="two-col">
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.testimonials.rating' | translate }}</mat-label>
            <mat-select formControlName="rating">
              @for (value of [5, 4, 3, 2, 1]; track value) {
                <mat-option [value]="value">{{ value }} ★</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'admin.testimonials.psychologist' | translate }}</mat-label>
            <mat-select formControlName="psychologistId">
              <mat-option [value]="null">—</mat-option>
              @for (p of psychologists(); track p.id) {
                <mat-option [value]="p.id">{{ p.displayName }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </div>
        <div class="controls">
          <mat-form-field appearance="outline" class="order">
            <mat-label>{{ 'admin.sortOrder' | translate }}</mat-label>
            <input matInput type="number" formControlName="sortOrder" />
          </mat-form-field>
          <mat-checkbox formControlName="isPublished">{{ 'admin.published' | translate }}</mat-checkbox>
        </div>
        <div class="actions">
          <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">{{ 'admin.save' | translate }}</button>
          @if (editing()) {
            <button mat-button type="button" (click)="reset()">{{ 'admin.cancel' | translate }}</button>
          }
        </div>
      </form>
    </mat-card>

    <mat-card class="panel">
      @for (item of items(); track item.id) {
        <div class="row">
          <div>
            <strong>{{ item.authorName }}</strong>
            <span class="muted"> — {{ item.roleLabel }} · {{ item.rating }}★</span>
            @if (!item.isPublished) {
              <span class="badge">{{ 'admin.hidden' | translate }}</span>
            }
            <div class="muted content">{{ item.content }}</div>
          </div>
          <div class="row-actions">
            <button mat-icon-button (click)="edit(item)"><mat-icon>edit</mat-icon></button>
            <button mat-icon-button (click)="remove(item)"><mat-icon>delete</mat-icon></button>
          </div>
        </div>
      }
    </mat-card>
  `,
  styles: `
    h2 { margin: 0 0 12px; }
    h3 { margin: 0 0 12px; }
    .panel { padding: 24px; margin-bottom: 20px; }
    form { display: flex; flex-direction: column; gap: 4px; }
    mat-form-field { width: 100%; }
    .two-col { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .controls { display: flex; align-items: center; gap: 20px; }
    .order { width: 140px; }
    .actions { display: flex; gap: 8px; }
    .row { display: flex; justify-content: space-between; gap: 16px; padding: 10px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); }
    .row:last-child { border-bottom: none; }
    .muted { color: var(--mat-sys-on-surface-variant); font-size: 0.85rem; }
    .content { margin-top: 4px; max-width: 70ch; }
    .badge { font-size: 0.7rem; background: var(--mat-sys-surface-container-high); border-radius: 10px; padding: 2px 8px; margin-left: 8px; }
    .error { color: var(--mat-sys-error); }
  `,
})
export class TestimonialsAdmin implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);

  protected readonly items = signal<AdminTestimonial[]>([]);
  protected readonly psychologists = signal<PsychologistOption[]>([]);
  protected readonly editing = signal<AdminTestimonial | null>(null);
  protected readonly busy = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly form = this.fb.group({
    authorName: this.fb.nonNullable.control('', Validators.required),
    roleLabel: this.fb.nonNullable.control(''),
    content: this.fb.nonNullable.control('', Validators.required),
    rating: this.fb.nonNullable.control(5),
    psychologistId: this.fb.control<string | null>(null),
    isPublished: this.fb.nonNullable.control(true),
    sortOrder: this.fb.nonNullable.control(1),
  });

  ngOnInit(): void {
    this.reload();
    this.http
      .get<PsychologistOption[]>('/api/admin/psychologists')
      .subscribe((rows) => this.psychologists.set(rows));
  }

  protected save(): void {
    if (this.form.invalid) {
      return;
    }

    this.busy.set(true);
    this.errorKey.set(null);
    const value = this.form.getRawValue();
    const payload = { ...value, roleLabel: value.roleLabel || null };
    const current = this.editing();
    const request = current
      ? this.http.put(`/api/admin/testimonials/${current.id}`, payload)
      : this.http.post('/api/admin/testimonials', payload);

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

  protected edit(item: AdminTestimonial): void {
    this.editing.set(item);
    this.form.patchValue({ ...item, roleLabel: item.roleLabel ?? '' });
  }

  protected remove(item: AdminTestimonial): void {
    this.http.delete(`/api/admin/testimonials/${item.id}`).subscribe({
      next: () => this.reload(),
      error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
    });
  }

  protected reset(): void {
    this.editing.set(null);
    this.form.reset({
      authorName: '', roleLabel: '', content: '', rating: 5,
      psychologistId: null, isPublished: true, sortOrder: this.items().length + 1,
    });
  }

  private reload(): void {
    this.http.get<AdminTestimonial[]>('/api/admin/testimonials').subscribe((rows) => this.items.set(rows));
  }
}
