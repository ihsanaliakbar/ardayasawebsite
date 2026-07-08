import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CLINIC } from '../../core/clinic';
import { ContentService } from '../../core/content/content.service';
import { IdrPipe } from '../../core/idr.pipe';
import { SeoService } from '../../core/seo';

@Component({
  selector: 'app-services-page',
  imports: [RouterLink, MatButtonModule, MatIconModule, TranslatePipe, IdrPipe],
  template: `
    <div class="page-container page">
      <h1 class="section-title">{{ 'services.title' | translate }}</h1>
      <p class="section-subtitle">{{ 'services.subtitle' | translate }}</p>

      @if (catalog(); as categories) {
        @for (category of categories; track category.id) {
          <section class="category card-panel">
            <h2>{{ category.name }}</h2>
            @if (category.description) {
              <p class="category-desc">{{ category.description }}</p>
            }
            <div class="service-list">
              @for (service of category.services; track service.id) {
                <div class="service-row">
                  <div class="service-info">
                    <span class="service-name">{{ service.name }}</span>
                    <span class="service-meta">
                      @if (service.durationMinutes) {
                        {{ service.durationMinutes }} {{ 'services.minutes' | translate }}
                      }
                      @if (service.sessionCount > 1) {
                        · {{ service.sessionCount }} {{ 'services.sessions' | translate }}
                      }
                      @if (service.notes) {
                        · {{ service.notes }}
                      }
                    </span>
                  </div>
                  <div class="service-prices">
                    @if (service.offlinePrice != null) {
                      <span class="price">
                        <span class="price-label">{{ 'services.offline' | translate }}</span>
                        {{ service.offlinePrice | idr }}
                      </span>
                    }
                    @if (service.onlinePrice != null) {
                      <span class="price">
                        <span class="price-label">{{ 'services.online' | translate }}</span>
                        {{ service.onlinePrice | idr }}
                      </span>
                    }
                  </div>
                </div>
              }
            </div>
          </section>
        }
      }

      <p class="overtime-note">
        <mat-icon inline>info</mat-icon>
        {{ 'services.overtimeNote' | translate }}
      </p>

      <div class="cta">
        <a mat-flat-button class="gold-btn" routerLink="/janji-temu">
          {{ 'services.cta' | translate }}
        </a>
      </div>
    </div>
  `,
  styles: `
    .page { padding-top: 48px; }
    .category { padding: 28px; margin-bottom: 24px; }
    .category h2 { margin: 0 0 4px; color: var(--accent-gold); font-size: 1.4rem; }
    .category-desc { color: var(--text-muted); font-size: 0.9rem; max-width: 72ch; margin: 0 0 16px; }
    .service-list { display: flex; flex-direction: column; }
    .service-row {
      display: flex; justify-content: space-between; align-items: baseline; gap: 16px;
      padding: 12px 0; border-bottom: 1px solid rgba(102, 146, 152, 0.15);
    }
    .service-row:last-child { border-bottom: none; }
    .service-info { display: flex; flex-direction: column; }
    .service-name { font-weight: 500; }
    .service-meta { color: var(--teal-muted); font-size: 0.8rem; margin-top: 2px; }
    .service-prices { display: flex; gap: 20px; flex-shrink: 0; }
    .price { font-weight: 600; color: var(--text-main); display: flex; flex-direction: column; align-items: flex-end; }
    .price-label { font-size: 0.7rem; font-weight: 400; color: var(--teal-muted); text-transform: uppercase; letter-spacing: 0.05em; }
    .overtime-note { color: var(--text-muted); font-size: 0.85rem; }
    .overtime-note mat-icon { vertical-align: -3px; margin-right: 4px; color: var(--accent-gold); }
    .cta { text-align: center; margin-top: 32px; }
    .gold-btn { --mat-button-filled-container-color: var(--accent-gold); --mat-button-filled-label-text-color: #071c1f; font-weight: 600; }

    @media (max-width: 640px) {
      .service-row { flex-direction: column; align-items: stretch; }
      .service-prices { justify-content: flex-start; }
      .price { align-items: flex-start; }
    }
  `,
})
export class ServicesPage {
  private readonly content = inject(ContentService);
  private readonly seo = inject(SeoService);
  private readonly translate = inject(TranslateService);

  protected readonly clinic = CLINIC;
  protected readonly catalog = toSignal(this.content.getServiceCatalog());

  constructor() {
    this.seo.update({
      title: this.translate.instant('services.title'),
      description: this.translate.instant('services.metaDescription'),
    });
  }
}
