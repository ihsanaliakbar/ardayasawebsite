import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';

interface AdminArticleRow {
  id: string;
  title: string;
  slug: string;
  status: 'Draft' | 'Published';
  publishedAtUtc: string | null;
  updatedAtUtc: string;
}

@Component({
  selector: 'app-articles-admin',
  imports: [RouterLink, DatePipe, MatButtonModule, MatCardModule, MatIconModule, TranslatePipe],
  template: `
    <div class="header-row">
      <h2>{{ 'admin.articles.title' | translate }}</h2>
      <a mat-flat-button routerLink="/admin/artikel/baru">
        <mat-icon>add</mat-icon> {{ 'admin.articles.new' | translate }}
      </a>
    </div>

    <mat-card class="panel">
      @if (articles().length === 0) {
        <p>{{ 'admin.articles.empty' | translate }}</p>
      }
      @for (article of articles(); track article.id) {
        <div class="row">
          <div>
            <a [routerLink]="['/admin/artikel', article.id]" class="title">{{ article.title }}</a>
            <div class="muted">
              /artikel/{{ article.slug }} ·
              {{ 'admin.articles.updated' | translate }} {{ article.updatedAtUtc | date: 'd MMM y HH:mm' }}
            </div>
          </div>
          <span class="badge" [class.ok]="article.status === 'Published'">
            {{ (article.status === 'Published' ? 'admin.articles.published' : 'admin.articles.draft') | translate }}
          </span>
        </div>
      }
    </mat-card>
  `,
  styles: `
    .header-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
    h2 { margin: 0; }
    .panel { padding: 16px 24px; }
    .row { display: flex; justify-content: space-between; align-items: center; gap: 16px; padding: 12px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); }
    .row:last-child { border-bottom: none; }
    .title { color: var(--mat-sys-on-surface); font-weight: 500; text-decoration: none; }
    .title:hover { color: var(--accent-gold); }
    .muted { color: var(--mat-sys-on-surface-variant); font-size: 0.8rem; margin-top: 2px; }
    .badge { font-size: 0.75rem; padding: 4px 10px; border-radius: 12px; background: var(--mat-sys-surface-container-high); color: var(--mat-sys-on-surface-variant); }
    .badge.ok { background: rgba(244, 217, 111, 0.15); color: var(--accent-gold); }
  `,
})
export class ArticlesAdmin implements OnInit {
  private readonly http = inject(HttpClient);
  protected readonly articles = signal<AdminArticleRow[]>([]);

  ngOnInit(): void {
    this.http.get<AdminArticleRow[]>('/api/admin/articles').subscribe((rows) => this.articles.set(rows));
  }
}
