import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { RichTextEditor } from '../../shared/rich-text-editor';

interface AdminFaqItem {
  id: string;
  question: string;
  answerHtml: string;
  sortOrder: number;
  isPublished: boolean;
}

@Component({
  selector: 'app-faq-admin',
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatCheckboxModule, MatIconModule, RichTextEditor, TranslatePipe,
  ],
  template: `
    <h2>{{ 'admin.faq.title' | translate }}</h2>
    @if (errorKey(); as key) {
      <p class="error">{{ key | translate }}</p>
    }

    <mat-card class="panel">
      <h3>{{ (editing() ? 'admin.faq.editItem' : 'admin.faq.newItem') | translate }}</h3>
      <form [formGroup]="form" (ngSubmit)="save()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'admin.faq.question' | translate }}</mat-label>
          <input matInput formControlName="question" />
        </mat-form-field>
        <app-rich-text-editor formControlName="answerHtml" />
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
          <div class="q">
            {{ item.sortOrder }}. {{ item.question }}
            @if (!item.isPublished) {
              <span class="badge">{{ 'admin.hidden' | translate }}</span>
            }
          </div>
          <div class="row-actions">
            <button mat-icon-button (click)="edit(item)" [attr.aria-label]="'admin.edit' | translate">
              <mat-icon>edit</mat-icon>
            </button>
            <button mat-icon-button (click)="remove(item)" [attr.aria-label]="'admin.delete' | translate">
              <mat-icon>delete</mat-icon>
            </button>
          </div>
        </div>
      }
    </mat-card>
  `,
  styles: `
    h2 { margin: 0 0 12px; }
    h3 { margin: 0 0 12px; }
    .panel { padding: 24px; margin-bottom: 20px; }
    form { display: flex; flex-direction: column; gap: 10px; }
    mat-form-field { width: 100%; }
    .controls { display: flex; align-items: center; gap: 20px; }
    .order { width: 140px; }
    .actions { display: flex; gap: 8px; }
    .row { display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); }
    .row:last-child { border-bottom: none; }
    .q { font-size: 0.925rem; }
    .badge { font-size: 0.7rem; background: var(--mat-sys-surface-container-high); color: var(--mat-sys-on-surface-variant); border-radius: 10px; padding: 2px 8px; margin-left: 8px; }
    .error { color: var(--mat-sys-error); }
  `,
})
export class FaqAdmin implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);

  protected readonly items = signal<AdminFaqItem[]>([]);
  protected readonly editing = signal<AdminFaqItem | null>(null);
  protected readonly busy = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    question: ['', Validators.required],
    answerHtml: ['', Validators.required],
    sortOrder: [1],
    isPublished: [true],
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
    const payload = this.form.getRawValue();
    const current = this.editing();
    const request = current
      ? this.http.put(`/api/admin/faq/${current.id}`, payload)
      : this.http.post('/api/admin/faq', payload);

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

  protected edit(item: AdminFaqItem): void {
    this.editing.set(item);
    this.form.patchValue(item);
  }

  protected remove(item: AdminFaqItem): void {
    this.http.delete(`/api/admin/faq/${item.id}`).subscribe({
      next: () => this.reload(),
      error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
    });
  }

  protected reset(): void {
    this.editing.set(null);
    this.form.reset({ question: '', answerHtml: '', sortOrder: this.items().length + 1, isPublished: true });
  }

  private reload(): void {
    this.http.get<AdminFaqItem[]>('/api/admin/faq').subscribe((rows) => {
      this.items.set(rows);
      if (!this.editing()) {
        this.form.patchValue({ sortOrder: rows.length + 1 });
      }
    });
  }
}
