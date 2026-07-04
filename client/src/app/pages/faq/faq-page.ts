import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CLINIC } from '../../core/clinic';
import { ContentService } from '../../core/content/content.service';
import { SeoService } from '../../core/seo';

/** FAQ in the chat-bubble style of the brand's Instagram/mockup. */
@Component({
  selector: 'app-faq-page',
  imports: [MatButtonModule, MatIconModule, TranslatePipe],
  template: `
    <div class="page-container page">
      <h1 class="section-title">FAQ</h1>
      <p class="section-subtitle">{{ 'faq.subtitle' | translate }}</p>

      <div class="chat">
        @for (item of faq(); track item.id) {
          <div class="bubble question">
            <mat-icon>account_circle</mat-icon>
            <span>{{ item.question }}</span>
          </div>
          <div class="bubble answer">
            <div class="rich-text" [innerHTML]="item.answerHtml"></div>
            <mat-icon class="avatar">support_agent</mat-icon>
          </div>
        }
      </div>

      <div class="card-panel still-question">
        <h2>{{ 'faq.stillTitle' | translate }}</h2>
        <p>{{ 'faq.stillText' | translate }}</p>
        <a mat-flat-button class="gold-btn" [href]="clinic.whatsAppUrl" target="_blank" rel="noopener">
          <mat-icon>chat</mat-icon> {{ 'faq.stillCta' | translate }}
        </a>
      </div>
    </div>
  `,
  styles: `
    .page { padding-top: 48px; max-width: 760px; }
    .chat { display: flex; flex-direction: column; gap: 14px; }
    .bubble { border-radius: 18px; padding: 16px 20px; max-width: 88%; }
    .bubble.question {
      background: rgba(163, 165, 164, 0.22); align-self: flex-start; border-bottom-left-radius: 4px;
      display: flex; gap: 10px; align-items: flex-start; font-weight: 500;
    }
    .bubble.question mat-icon { color: var(--bubble-light); flex-shrink: 0; }
    .bubble.answer {
      background: var(--primary-light); align-self: flex-end; border-bottom-right-radius: 4px;
      display: flex; gap: 10px; align-items: flex-start;
    }
    .bubble.answer .avatar { color: var(--accent-gold); order: 2; flex-shrink: 0; }
    .bubble.answer .rich-text { font-size: 0.925rem; }
    .bubble.answer .rich-text :last-child { margin-bottom: 0; }

    .still-question { margin-top: 48px; padding: 32px; text-align: center; }
    .still-question h2 { margin: 0 0 8px; }
    .still-question p { color: var(--text-muted); margin: 0 0 20px; }
    .gold-btn { --mat-button-filled-container-color: var(--accent-gold); --mat-button-filled-label-text-color: #071c1f; font-weight: 600; }
  `,
})
export class FaqPage {
  private readonly content = inject(ContentService);
  private readonly seo = inject(SeoService);
  private readonly translate = inject(TranslateService);

  protected readonly clinic = CLINIC;
  protected readonly faq = toSignal(this.content.getFaq());

  constructor() {
    this.seo.update({
      title: 'FAQ',
      description: this.translate.instant('faq.metaDescription'),
    });
  }
}
