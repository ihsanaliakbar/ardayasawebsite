import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { BehaviorSubject, switchMap } from 'rxjs';
import { ContentService } from '../../core/content/content.service';
import { SeoService } from '../../core/seo';

interface ArticleFilter {
  category?: string;
  search?: string;
  page: number;
}

@Component({
  selector: 'app-articles-page',
  imports: [RouterLink, DatePipe, FormsModule, MatButtonModule, MatIconModule, TranslatePipe],
  template: `
    <div class="page-container page">
      <h1 class="section-title">{{ 'articles.title' | translate }}</h1>
      <p class="section-subtitle">{{ 'articles.subtitle' | translate }}</p>

      <div class="filters">
        <div class="chips">
          <button class="chip" [class.selected]="!activeCategory()" (click)="setCategory(undefined)">
            {{ 'articles.allCategories' | translate }}
          </button>
          @for (category of categories() ?? []; track category.id) {
            <button
              class="chip"
              [class.selected]="activeCategory() === category.slug"
              (click)="setCategory(category.slug)"
            >{{ category.name }}</button>
          }
        </div>
        <div class="search">
          <mat-icon inline>search</mat-icon>
          <input
            type="search"
            [placeholder]="'articles.searchPlaceholder' | translate"
            [ngModel]="search()"
            (ngModelChange)="setSearch($event)"
          />
        </div>
      </div>

      @if (result(); as page) {
        @if (page.items.length === 0) {
          <p class="empty">{{ 'articles.empty' | translate }}</p>
        }
        <div class="grid">
          @for (article of page.items; track article.slug) {
            <a class="card-panel article" [routerLink]="['/artikel', article.slug]">
              @if (article.featuredImageUrl) {
                <img [src]="article.featuredImageUrl" [alt]="article.title" />
              }
              <div class="body">
                <div class="meta">
                  @if (article.categoryName) {
                    <span class="category">{{ article.categoryName }}</span>
                  }
                  @if (article.publishedAtUtc) {
                    <span>{{ article.publishedAtUtc | date: 'd MMMM y' }}</span>
                  }
                </div>
                <h2>{{ article.title }}</h2>
                @if (article.excerpt) {
                  <p>{{ article.excerpt }}</p>
                }
                <span class="read-more">{{ 'articles.readMore' | translate }} <mat-icon inline>arrow_forward_ios</mat-icon></span>
              </div>
            </a>
          }
        </div>

        @if (page.totalCount > page.pageSize) {
          <div class="pagination">
            <button mat-stroked-button [disabled]="page.page <= 1" (click)="setPage(page.page - 1)">
              {{ 'articles.previous' | translate }}
            </button>
            <span>{{ page.page }} / {{ totalPages(page.totalCount, page.pageSize) }}</span>
            <button
              mat-stroked-button
              [disabled]="page.page >= totalPages(page.totalCount, page.pageSize)"
              (click)="setPage(page.page + 1)"
            >{{ 'articles.next' | translate }}</button>
          </div>
        }
      }
    </div>
  `,
  styles: `
    .page { padding-top: 48px; }
    .filters { display: flex; justify-content: space-between; gap: 16px; flex-wrap: wrap; margin-bottom: 28px; }
    .chips { display: flex; gap: 8px; flex-wrap: wrap; }
    .chip {
      background: transparent; border: 1px solid rgba(102, 146, 152, 0.4); color: var(--text-muted);
      border-radius: 999px; padding: 6px 14px; cursor: pointer; font-size: 0.85rem; font-family: var(--font-body);
    }
    .chip.selected { background: var(--accent-gold); border-color: var(--accent-gold); color: #071c1f; font-weight: 600; }
    .search {
      display: flex; align-items: center; gap: 8px; background: var(--bg-teal);
      border: 1px solid rgba(102, 146, 152, 0.3); border-radius: 999px; padding: 6px 16px;
    }
    .search mat-icon { color: var(--teal-muted); }
    .search input {
      background: transparent; border: none; outline: none; color: var(--text-main);
      font-family: var(--font-body); width: 180px;
    }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 20px; }
    .article { display: flex; flex-direction: column; overflow: hidden; text-decoration: none; color: inherit; transition: transform 0.15s; }
    .article:hover { transform: translateY(-4px); }
    .article img { width: 100%; height: 170px; object-fit: cover; }
    .body { padding: 20px; display: flex; flex-direction: column; flex: 1; }
    .meta { display: flex; gap: 10px; font-size: 0.75rem; color: var(--teal-muted); margin-bottom: 8px; }
    .category { color: var(--accent-gold); }
    h2 { font-size: 1.1rem; margin: 0 0 8px; line-height: 1.4; }
    .body p { color: var(--text-muted); font-size: 0.875rem; line-height: 1.6; flex: 1; margin: 0 0 12px; }
    .read-more { color: var(--accent-gold); font-size: 0.85rem; display: flex; gap: 8px; }
    .empty { text-align: center; color: var(--text-muted); padding: 40px 0; }
    .pagination { display: flex; justify-content: center; align-items: center; gap: 16px; margin-top: 32px; color: var(--text-muted); }
  `,
})
export class ArticlesPage {
  private readonly content = inject(ContentService);
  private readonly seo = inject(SeoService);
  private readonly translate = inject(TranslateService);

  private readonly filter$ = new BehaviorSubject<ArticleFilter>({ page: 1 });
  private searchDebounce: ReturnType<typeof setTimeout> | undefined;

  protected readonly activeCategory = signal<string | undefined>(undefined);
  protected readonly search = signal('');
  protected readonly categories = toSignal(this.content.getArticleCategories());
  protected readonly result = toSignal(
    this.filter$.pipe(switchMap((f) => this.content.getArticles(f))),
  );

  constructor() {
    this.seo.update({
      title: this.translate.instant('articles.title'),
      description: this.translate.instant('articles.metaDescription'),
    });
  }

  protected setCategory(slug: string | undefined): void {
    this.activeCategory.set(slug);
    this.filter$.next({ category: slug, search: this.search() || undefined, page: 1 });
  }

  protected setSearch(term: string): void {
    this.search.set(term);
    clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => {
      this.filter$.next({ category: this.activeCategory(), search: term || undefined, page: 1 });
    }, 300);
  }

  protected setPage(page: number): void {
    this.filter$.next({ category: this.activeCategory(), search: this.search() || undefined, page });
  }

  protected totalPages(total: number, pageSize: number): number {
    return Math.max(1, Math.ceil(total / pageSize));
  }
}
