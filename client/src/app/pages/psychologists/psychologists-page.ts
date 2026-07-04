import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ContentService } from '../../core/content/content.service';
import { SeoService } from '../../core/seo';

@Component({
  selector: 'app-psychologists-page',
  imports: [RouterLink, MatIconModule, TranslatePipe],
  template: `
    <div class="page-container page">
      <h1 class="section-title">{{ 'psychologists.title' | translate }}</h1>
      <p class="section-subtitle">{{ 'psychologists.subtitle' | translate }}</p>

      <div class="grid">
        @for (p of psychologists(); track p.id) {
          <a class="card-panel psych" [routerLink]="['/psikolog-kami', p.slug]">
            @if (p.photoUrl) {
              <img [src]="p.photoUrl" [alt]="p.displayName" />
            } @else {
              <div class="placeholder"><mat-icon>person</mat-icon></div>
            }
            <div class="info">
              <h2>{{ p.displayName }}<span class="degree">, {{ p.title }}</span></h2>
              @if (p.specialization) {
                <span class="badge">{{ p.specialization }}</span>
              }
              <ul class="expertise">
                @for (item of p.expertise.slice(0, 3); track item) {
                  <li>{{ item }}</li>
                }
                @if (p.expertise.length > 3) {
                  <li class="more">{{ 'psychologists.moreExpertise' | translate: { count: p.expertise.length - 3 } }}</li>
                }
              </ul>
              <span class="profile-link">
                {{ 'psychologists.viewProfile' | translate }} <mat-icon inline>arrow_forward</mat-icon>
              </span>
            </div>
          </a>
        }
      </div>
    </div>
  `,
  styles: `
    .page { padding-top: 48px; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 20px; }
    .psych { display: flex; gap: 20px; padding: 24px; text-decoration: none; color: inherit; transition: transform 0.15s; }
    .psych:hover { transform: translateY(-4px); }
    img, .placeholder {
      width: 110px; height: 132px; object-fit: cover; border-radius: 14px; flex-shrink: 0;
      border: 2px solid rgba(244, 217, 111, 0.35);
    }
    .placeholder { display: grid; place-items: center; background: var(--primary); }
    h2 { font-size: 1.1rem; margin: 0 0 6px; }
    .degree { font-family: var(--font-body); font-weight: 400; font-size: 0.85rem; color: var(--text-muted); }
    .badge {
      display: inline-block; font-size: 0.72rem; color: var(--accent-gold);
      border: 1px solid rgba(244, 217, 111, 0.4); border-radius: 999px; padding: 3px 10px; margin-bottom: 8px;
    }
    .expertise { margin: 0 0 10px; padding-left: 18px; color: var(--text-muted); font-size: 0.82rem; }
    .expertise li { margin-bottom: 2px; }
    .expertise .more { list-style: none; color: var(--teal-muted); }
    .profile-link { color: var(--accent-gold); font-size: 0.85rem; }
  `,
})
export class PsychologistsPage {
  private readonly content = inject(ContentService);
  private readonly seo = inject(SeoService);
  private readonly translate = inject(TranslateService);

  protected readonly psychologists = toSignal(this.content.getPsychologists());

  constructor() {
    this.seo.update({
      title: this.translate.instant('psychologists.title'),
      description: this.translate.instant('psychologists.metaDescription'),
    });
  }
}
