import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { RichTextEditor } from '../../shared/rich-text-editor';

interface AdminArticle {
  id: string;
  title: string;
  slug: string;
  excerpt: string | null;
  contentHtml: string;
  featuredImageKey: string | null;
  featuredImageUrl: string | null;
  categoryId: string | null;
  status: 'Draft' | 'Published';
}

interface CategoryOption {
  id: string;
  name: string;
  slug: string;
}

@Component({
  selector: 'app-article-edit',
  imports: [
    ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, RichTextEditor, TranslatePipe,
  ],
  template: `
    <a routerLink="/admin/artikel" class="back">
      <mat-icon inline>arrow_back_ios</mat-icon> {{ 'admin.articles.title' | translate }}
    </a>

    <mat-card class="panel">
      <div class="header-row">
        <h2>{{ (isNew() ? 'admin.articles.newTitle' : 'admin.articles.editTitle') | translate }}</h2>
        @if (!isNew() && article(); as a) {
          @if (a.status === 'Published') {
            <button mat-stroked-button (click)="setPublished(false)">{{ 'admin.articles.unpublish' | translate }}</button>
          } @else {
            <button mat-flat-button (click)="setPublished(true)">{{ 'admin.articles.publish' | translate }}</button>
          }
        }
      </div>

      @if (errorKey(); as key) {
        <p class="error">{{ key | translate }}</p>
      }
      @if (saved()) {
        <p class="success">{{ 'admin.saved' | translate }}</p>
      }

      <form [formGroup]="form" (ngSubmit)="save()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'admin.articles.formTitle' | translate }}</mat-label>
          <input matInput formControlName="title" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>{{ 'admin.articles.formSlug' | translate }}</mat-label>
          <input matInput formControlName="slug" [placeholder]="'admin.articles.slugHint' | translate" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>{{ 'admin.articles.formExcerpt' | translate }}</mat-label>
          <textarea matInput formControlName="excerpt" rows="2"></textarea>
        </mat-form-field>

        <div class="category-row">
          <mat-form-field appearance="outline" class="category-field">
            <mat-label>{{ 'admin.articles.formCategory' | translate }}</mat-label>
            <mat-select formControlName="categoryId">
              <mat-option [value]="null">—</mat-option>
              @for (category of categories(); track category.id) {
                <mat-option [value]="category.id">{{ category.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline" class="category-field">
            <mat-label>{{ 'admin.articles.newCategory' | translate }}</mat-label>
            <input matInput #newCategory (keydown.enter)="$event.preventDefault(); addCategory(newCategory)" />
            <button matSuffix mat-icon-button type="button" (click)="addCategory(newCategory)">
              <mat-icon>add</mat-icon>
            </button>
          </mat-form-field>
        </div>

        <div class="featured-row">
          @if (featuredImageUrl(); as url) {
            <img [src]="url" alt="" class="featured-preview" />
          }
          <label>
            <input type="file" accept="image/*" hidden (change)="uploadFeatured($event)" />
            <span class="upload-btn">
              <mat-icon inline>image</mat-icon> {{ 'admin.articles.uploadFeatured' | translate }}
            </span>
          </label>
          @if (featuredImageUrl()) {
            <button mat-button type="button" (click)="clearFeatured()">{{ 'admin.articles.removeImage' | translate }}</button>
          }
        </div>

        <app-rich-text-editor formControlName="contentHtml" />

        <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">
          {{ 'admin.save' | translate }}
        </button>
      </form>
    </mat-card>
  `,
  styles: `
    .back { color: var(--mat-sys-on-surface-variant); text-decoration: none; font-size: 0.9rem; }
    .panel { padding: 24px; margin-top: 16px; max-width: 860px; }
    .header-row { display: flex; justify-content: space-between; align-items: center; }
    h2 { margin: 0 0 12px; }
    form { display: flex; flex-direction: column; gap: 8px; }
    mat-form-field { width: 100%; }
    .category-row { display: flex; gap: 12px; }
    .category-field { flex: 1; }
    .featured-row { display: flex; align-items: center; gap: 16px; margin-bottom: 12px; }
    .featured-preview { height: 72px; border-radius: 8px; }
    .upload-btn { cursor: pointer; display: inline-flex; align-items: center; gap: 6px; padding: 8px 16px; border: 1px solid var(--mat-sys-outline); border-radius: 999px; }
    button[type='submit'] { align-self: flex-start; margin-top: 12px; }
    .error { color: var(--mat-sys-error); }
    .success { color: var(--accent-gold); }
  `,
})
export class ArticleEdit implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly article = signal<AdminArticle | null>(null);
  protected readonly categories = signal<CategoryOption[]>([]);
  protected readonly featuredImageUrl = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly saved = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  private featuredImageKey: string | null = null;

  protected readonly form = this.fb.group({
    title: this.fb.nonNullable.control('', Validators.required),
    slug: this.fb.nonNullable.control(''),
    excerpt: this.fb.nonNullable.control(''),
    categoryId: this.fb.control<string | null>(null),
    contentHtml: this.fb.nonNullable.control('', Validators.required),
  });

  protected isNew(): boolean {
    return this.route.snapshot.paramMap.get('id') === null;
  }

  ngOnInit(): void {
    this.http.get<CategoryOption[]>('/api/articles/categories').subscribe((c) => this.categories.set(c));

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.http.get<AdminArticle>(`/api/admin/articles/${id}`).subscribe((a) => this.apply(a));
    }
  }

  protected save(): void {
    if (this.form.invalid) {
      return;
    }

    this.busy.set(true);
    this.saved.set(false);
    this.errorKey.set(null);

    const value = this.form.getRawValue();
    const payload = {
      title: value.title,
      slug: value.slug || null,
      excerpt: value.excerpt || null,
      contentHtml: value.contentHtml,
      categoryId: value.categoryId,
      featuredImageKey: this.featuredImageKey,
    };

    const current = this.article();
    const request = current
      ? this.http.put<AdminArticle>(`/api/admin/articles/${current.id}`, payload)
      : this.http.post<AdminArticle>('/api/admin/articles', payload);

    request.subscribe({
      next: (a) => {
        this.busy.set(false);
        this.saved.set(true);
        const wasNew = !current;
        this.apply(a);
        if (wasNew) {
          void this.router.navigate(['/admin/artikel', a.id], { replaceUrl: true });
        }
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.errorKey.set(errorKeyFromResponse(err));
      },
    });
  }

  protected setPublished(publish: boolean): void {
    const current = this.article();
    if (!current) {
      return;
    }

    const action = publish ? 'publish' : 'unpublish';
    this.http.post(`/api/admin/articles/${current.id}/${action}`, null).subscribe({
      next: () => this.article.set({ ...current, status: publish ? 'Published' : 'Draft' }),
      error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
    });
  }

  protected uploadFeatured(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }

    const form = new FormData();
    form.append('file', file);
    this.http.post<{ key: string; url: string }>('/api/admin/uploads', form).subscribe({
      next: (result) => {
        this.featuredImageKey = result.key;
        this.featuredImageUrl.set(result.url);
      },
      error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
    });
  }

  protected clearFeatured(): void {
    this.featuredImageKey = null;
    this.featuredImageUrl.set(null);
  }

  private apply(a: AdminArticle): void {
    this.article.set(a);
    this.featuredImageKey = a.featuredImageKey;
    this.featuredImageUrl.set(a.featuredImageUrl);
    this.form.patchValue({
      title: a.title,
      slug: a.slug,
      excerpt: a.excerpt ?? '',
      categoryId: a.categoryId,
      contentHtml: a.contentHtml,
    });
  }

  protected addCategory(input: HTMLInputElement): void {
    const name = input.value.trim();
    if (!name) {
      return;
    }

    this.http
      .post<CategoryOption>('/api/admin/articles/categories', { name, sortOrder: this.categories().length + 1 })
      .subscribe({
        next: (category) => {
          this.categories.update((list) => [...list, category]);
          this.form.patchValue({ categoryId: category.id });
          input.value = '';
        },
        error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
      });
  }
}
