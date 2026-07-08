import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CLINIC } from '../../core/clinic';
import { ContentService } from '../../core/content/content.service';
import { SeoService } from '../../core/seo';

@Component({
  selector: 'app-home',
  imports: [RouterLink, MatButtonModule, MatIconModule, TranslatePipe],
  template: `
    <!-- Hero -->
    <section class="hero">
      <div class="page-container hero-grid">
        <div class="hero-copy">
          <h1 [innerHTML]="'home.heroTitle' | translate"></h1>
          <p class="hero-sub">{{ 'home.heroSubtitle' | translate }}</p>
          <div class="hero-actions">
            <a mat-flat-button class="gold-btn" [href]="clinic.whatsAppUrl" target="_blank" rel="noopener">
              {{ 'home.ctaConsult' | translate }}
            </a>
            <a mat-stroked-button routerLink="/layanan">{{ 'home.ctaServices' | translate }}</a>
          </div>
          <div class="hero-badges">
            <span><mat-icon inline>verified_user</mat-icon> {{ 'home.badgePrivacy' | translate }}</span>
            <span><mat-icon inline>lock</mat-icon> {{ 'home.badgeConfidential' | translate }}</span>
          </div>
        </div>
        <div class="hero-art" aria-hidden="true">
          <div class="hero-glow"></div>
          <img src="/images/ardayasa-logo.jpg" alt="" />
        </div>
      </div>
    </section>

    <!-- Services teaser -->
    <section class="page-container section">
      <h2 class="section-title">{{ 'home.servicesTitle' | translate }}</h2>
      <p class="section-subtitle">{{ 'home.servicesSubtitle' | translate }}</p>
      <div class="cards-3">
        @for (card of serviceCards; track card.icon) {
          <div class="card-panel service-card">
            <mat-icon class="service-icon">{{ card.icon }}</mat-icon>
            <h3>{{ card.title | translate }}</h3>
            <p>{{ card.text | translate }}</p>
            <a routerLink="/layanan" class="more-link">
              {{ 'home.more' | translate }} <mat-icon inline>arrow_forward_ios</mat-icon>
            </a>
          </div>
        }
      </div>
    </section>

    <!-- Milestones (real numbers — DECISIONS.md 2026-07-04) -->
    <section class="page-container section">
      <div class="card-panel stats">
        @for (stat of stats; track stat.label) {
          <div class="stat">
            <mat-icon>{{ stat.icon }}</mat-icon>
            <div>
              <div class="stat-value">{{ stat.value }}</div>
              <div class="stat-label">{{ stat.label | translate }}</div>
            </div>
          </div>
        }
      </div>
    </section>

    <!-- Psychologists -->
    @if (psychologists(); as psychs) {
      @if (psychs.length > 0) {
        <section class="page-container section">
          <h2 class="section-title">{{ 'home.psychologistsTitle' | translate }}</h2>
          <p class="section-subtitle">{{ 'home.psychologistsSubtitle' | translate }}</p>
          <div class="psych-row">
            @for (p of psychs; track p.id) {
              <a class="psych-card card-panel" [routerLink]="['/psikolog-kami', p.slug]">
                @if (p.photoUrl) {
                  <img [src]="p.photoUrl" [alt]="p.displayName" />
                } @else {
                  <div class="psych-placeholder"><mat-icon>person</mat-icon></div>
                }
                <div class="psych-name">{{ p.displayName }}</div>
                <div class="psych-title">{{ p.specialization }}</div>
              </a>
            }
          </div>
        </section>
      }
    }

    <!-- FAQ preview: chat-bubble style from the brand mockup -->
    @if (faq(); as faqItems) {
      @if (faqItems.length > 0) {
        <section class="page-container section">
          <div class="card-panel faq-panel">
            <div class="faq-intro">
              <h2>FAQ</h2>
              <p>{{ 'home.faqSubtitle' | translate }}</p>
              <a mat-stroked-button routerLink="/faq">{{ 'home.faqAll' | translate }}</a>
            </div>
            <div class="faq-chat">
              @for (item of faqItems.slice(0, 2); track item.id) {
                <div class="bubble question"><mat-icon>account_circle</mat-icon> {{ item.question }}</div>
                <div class="bubble answer rich-text" [innerHTML]="item.answerHtml"></div>
              }
            </div>
          </div>
        </section>
      }
    }

    <!-- Testimonials -->
    @if (testimonials(); as items) {
      @if (items.length > 0) {
        <section class="page-container section">
          <h2 class="section-title">{{ 'home.testimonialsTitle' | translate }}</h2>
          <p class="section-subtitle">{{ 'home.testimonialsSubtitle' | translate }}</p>
          <div class="cards-3">
            @for (t of items.slice(0, 3); track t.id) {
              <div class="card-panel testimonial">
                <mat-icon class="quote-icon">format_quote</mat-icon>
                <p class="testimonial-text">{{ t.content }}</p>
                <div class="testimonial-meta">
                  <div class="testimonial-name">{{ t.authorName }}</div>
                  <div class="testimonial-role">{{ t.roleLabel }}</div>
                  <div class="stars" [attr.aria-label]="t.rating + '/5'">
                    @for (star of [1, 2, 3, 4, 5]; track star) {
                      <mat-icon inline [class.dim]="star > t.rating">star</mat-icon>
                    }
                  </div>
                </div>
              </div>
            }
          </div>
        </section>
      }
    }

    <!-- CTA -->
    <section class="page-container section">
      <div class="card-panel cta-panel">
        <mat-icon class="cta-icon">event_available</mat-icon>
        <div class="cta-copy">
          <h2>{{ 'home.ctaTitle' | translate }}</h2>
          <p>{{ 'home.ctaText' | translate }}</p>
        </div>
        <div class="cta-actions">
          <a mat-flat-button class="gold-btn" routerLink="/janji-temu">
            {{ 'home.ctaButton' | translate }}
          </a>
          <a class="cta-alt" [href]="clinic.whatsAppUrl" target="_blank" rel="noopener">
            {{ 'home.ctaWhatsApp' | translate }}
          </a>
        </div>
      </div>
    </section>
  `,
  styles: `
    .section { margin-top: 64px; }

    .gold-btn {
      --mat-button-filled-container-color: var(--accent-gold);
      --mat-button-filled-label-text-color: #071c1f;
      font-weight: 600;
    }

    /* Hero */
    .hero {
      background:
        radial-gradient(60% 80% at 80% 20%, rgba(11, 84, 97, 0.55), transparent),
        linear-gradient(180deg, var(--bg-teal), var(--bg-dark));
      border-bottom: 1px solid rgba(102, 146, 152, 0.15);
    }
    .hero-grid {
      display: grid; grid-template-columns: 1.2fr 0.8fr; gap: 32px;
      align-items: center; padding-top: 72px; padding-bottom: 72px;
    }
    h1 { font-size: clamp(2rem, 5vw, 3.2rem); line-height: 1.15; margin: 0 0 16px; }
    .hero-sub { color: var(--text-muted); font-size: 1.05rem; max-width: 46ch; margin: 0 0 28px; }
    .hero-actions { display: flex; gap: 12px; flex-wrap: wrap; }
    .hero-badges { display: flex; gap: 20px; margin-top: 24px; color: var(--text-muted); font-size: 0.85rem; }
    .hero-badges mat-icon { font-size: 1rem; vertical-align: -3px; color: var(--accent-gold); }
    .hero-art { position: relative; display: flex; justify-content: center; }
    .hero-art img { width: min(240px, 60%); border-radius: 28px; box-shadow: 0 24px 80px rgba(0, 0, 0, 0.45); }
    .hero-glow {
      position: absolute; inset: -20%;
      background: radial-gradient(closest-side, rgba(244, 217, 111, 0.12), transparent);
    }

    /* Service cards */
    .cards-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 20px; }
    .service-card { padding: 28px 24px; display: flex; flex-direction: column; }
    .service-icon {
      width: 48px; height: 48px; font-size: 28px; display: grid; place-items: center;
      background: rgba(244, 217, 111, 0.12); color: var(--accent-gold); border-radius: 50%;
      padding: 10px; box-sizing: content-box; margin-bottom: 16px;
    }
    .service-card h3 { margin: 0 0 10px; font-size: 1.15rem; }
    .service-card p { color: var(--text-muted); font-size: 0.9rem; line-height: 1.6; flex: 1; }
    .more-link { color: var(--accent-gold); text-decoration: none; font-size: 0.9rem; margin-top: 14px; display: flex; gap: 8px;}

    /* Stats */
    .stats { display: grid; grid-template-columns: repeat(3, 1fr); padding: 28px 24px; gap: 20px; }
    .stat { display: flex; align-items: center; gap: 14px; justify-content: center; }
    .stat > mat-icon { color: var(--accent-gold); font-size: 32px; width: 32px; height: 32px; }
    .stat-value { font-family: var(--font-display); font-size: 1.7rem; font-weight: 700; color: var(--accent-gold); }
    .stat-label { color: var(--text-muted); font-size: 0.85rem; margin-top: 8px;}

    /* Psychologists row */
    .psych-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); gap: 16px; }
    .psych-card { padding: 20px 16px; text-align: center; text-decoration: none; color: inherit; transition: transform 0.15s; }
    .psych-card:hover { transform: translateY(-4px); }
    .psych-card img, .psych-placeholder {
      width: 108px; height: 128px; object-fit: cover; border-radius: 14px;
      border: 2px solid rgba(244, 217, 111, 0.35); margin-bottom: 12px;
    }
    .psych-placeholder { display: grid; place-items: center; background: var(--primary); margin-inline: auto; }
    .psych-name { font-weight: 600; font-size: 0.95rem; }
    .psych-title { color: var(--teal-muted); font-size: 0.8rem; margin-top: 2px; }

    /* FAQ preview */
    .faq-panel { display: grid; grid-template-columns: 1fr 1.6fr; gap: 32px; padding: 32px; }
    .faq-intro h2 { font-size: 2rem; margin: 0 0 8px; }
    .faq-intro p { color: var(--text-muted); margin-bottom: 20px; }
    .faq-chat { display: flex; flex-direction: column; gap: 12px; }
    .bubble { border-radius: 16px; padding: 14px 18px; max-width: 85%; font-size: 0.9rem; }
    .bubble.question {
      background: rgba(163, 165, 164, 0.25); align-self: flex-start;
      border-bottom-left-radius: 4px; display: flex; gap: 8px; align-items: flex-start;
    }
    .bubble.question mat-icon { color: var(--bubble-light); flex-shrink: 0; }
    .bubble.answer {
      background: var(--primary-light); align-self: flex-end; border-bottom-right-radius: 4px;
    }

    /* Testimonials */
    .testimonial { padding: 24px; display: flex; flex-direction: column; }
    .quote-icon { color: var(--accent-gold); }
    .testimonial-text { color: var(--text-main); font-size: 0.925rem; line-height: 1.65; flex: 1; }
    .testimonial-name { font-weight: 600; margin-top: 12px; }
    .testimonial-role { color: var(--teal-muted); font-size: 0.8rem; }
    .stars { color: var(--accent-gold); margin-top: 4px; }
    .stars mat-icon { font-size: 1rem; }
    .stars mat-icon.dim { opacity: 0.25; }

    /* CTA */
    .cta-panel {
      display: flex; align-items: center; gap: 28px; padding: 32px;
      background: linear-gradient(120deg, var(--primary), var(--bg-teal));
    }
    .cta-icon {
      color: var(--accent-gold); font-size: 40px; width: 40px; height: 40px;
      background: rgba(244, 217, 111, 0.12); border-radius: 50%; padding: 16px; box-sizing: content-box;
    }
    .cta-copy { flex: 1; }
    .cta-copy h2 { margin: 0 0 6px; font-size: 1.4rem; }
    .cta-copy p { color: var(--text-muted); margin: 0; }
    .cta-actions { display: flex; flex-direction: column; gap: 8px; align-items: center; }
    .cta-alt { color: var(--text-muted); font-size: 0.8rem; }

    @media (max-width: 860px) {
      .hero-grid { grid-template-columns: 1fr; padding-top: 48px; padding-bottom: 48px; }
      .hero-art { display: none; }
      .cards-3 { grid-template-columns: 1fr; }
      .stats { grid-template-columns: 1fr; }
      .faq-panel { grid-template-columns: 1fr; }
      .cta-panel { flex-direction: column; text-align: center; }
    }
  `,
})
export class Home {
  private readonly content = inject(ContentService);
  private readonly seo = inject(SeoService);
  private readonly translate = inject(TranslateService);

  protected readonly clinic = CLINIC;
  protected readonly psychologists = toSignal(this.content.getPsychologists());
  protected readonly faq = toSignal(this.content.getFaq());
  protected readonly testimonials = toSignal(this.content.getTestimonials());

  protected readonly serviceCards = [
    { icon: 'forum', title: 'home.cardCounselingTitle', text: 'home.cardCounselingText' },
    { icon: 'diversity_1', title: 'home.cardCoupleTitle', text: 'home.cardCoupleText' },
    { icon: 'psychology', title: 'home.cardAssessmentTitle', text: 'home.cardAssessmentText' },
  ];

  // Real milestones from the clinic's Instagram (not placeholder marketing numbers).
  protected readonly stats = [
    { icon: 'group', value: '135+', label: 'home.statClients' },
    { icon: 'forum', value: '205+', label: 'home.statSessions' },
    { icon: 'handshake', value: '3', label: 'home.statCollaborations' },
  ];

  constructor() {
    this.seo.update({
      title: 'Ardayasa Wellbeing and Growth Center',
      description: this.translate.instant('home.metaDescription'),
    });
  }
}
