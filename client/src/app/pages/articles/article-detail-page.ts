import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, switchMap, tap } from 'rxjs';
import { ContentService } from '../../core/content/content.service';
import { SeoService } from '../../core/seo';

@Component({
  selector: 'app-article-detail-page',
  imports: [RouterLink, DatePipe, MatIconModule, TranslatePipe],
  template: `
    @if (article(); as a) {
      <article class="page-container page">
        <a routerLink="/artikel" class="back">
          <mat-icon inline>arrow_back_ios</mat-icon> {{ 'articles.backToList' | translate }}
        </a>

        <header>
          <div class="meta">
            @if (a.categoryName) {
              <span class="category">{{ a.categoryName }}</span>
            }
            @if (a.publishedAtUtc) {
              <span>{{ a.publishedAtUtc | date: 'd MMMM y' }}</span>
            }
          </div>
          <h1>{{ a.title }}</h1>
          @if (a.excerpt) {
            <p class="excerpt">{{ a.excerpt }}</p>
          }
        </header>

        @if (a.featuredImageUrl) {
          <img class="featured" [src]="a.featuredImageUrl" [alt]="a.title" />
        }

        <div class="rich-text article-body" [innerHTML]="a.contentHtml"></div>
      </article>
    }
  `,
  styles: `
    .page { padding-top: 32px; max-width: 760px; }
    .back { color: var(--text-muted); text-decoration: none; font-size: 0.9rem; }
    .back:hover { color: var(--accent-gold); }
    header { margin: 20px 0 24px; }
    .meta { display: flex; gap: 12px; color: var(--teal-muted); font-size: 0.85rem; margin-bottom: 10px; }
    .category { color: var(--accent-gold); }
    h1 { font-size: clamp(1.7rem, 4vw, 2.4rem); line-height: 1.25; margin: 0 0 12px; }
    .excerpt { color: var(--text-muted); font-size: 1.05rem; line-height: 1.6; }
    .featured { width: 100%; border-radius: 16px; margin-bottom: 28px; }
    .article-body { font-size: 1rem; }
  `,
})
export class ArticleDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly content = inject(ContentService);
  private readonly seo = inject(SeoService);

  protected readonly article = toSignal(
    this.route.paramMap.pipe(
      switchMap((params) =>
        this.content.getArticle(params.get('slug')!).pipe(
          catchError(() => {
            void this.router.navigateByUrl('/artikel');
            return EMPTY;
          }),
        ),
      ),
      tap((a) =>
        this.seo.update({
          title: a.title,
          description: a.excerpt ?? undefined,
          image: a.featuredImageUrl ?? undefined,
          type: 'article',
        }),
      ),
    ),
  );
}
