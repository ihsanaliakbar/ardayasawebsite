import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CLINIC } from '../../core/clinic';
import { SeoService } from '../../core/seo';

@Component({
  selector: 'app-about-page',
  imports: [MatButtonModule, MatIconModule, TranslatePipe],
  template: `
    <div class="page-container page">
      <h1 class="section-title">{{ 'about.title' | translate }}</h1>
      <p class="section-subtitle">{{ 'about.tagline' | translate }}</p>

      <div class="card-panel story">
        <img src="/images/ardayasa-logo.jpg" alt="" class="logo" />
        <div>
          <p>{{ 'about.paragraph1' | translate }}</p>
          <p>{{ 'about.paragraph2' | translate }}</p>
          <p class="hashtag">#HatiDipelukAsaDitanam</p>
        </div>
      </div>

      <div class="milestones">
        @for (stat of stats; track stat.label) {
          <div class="card-panel milestone">
            <div class="value">{{ stat.value }}</div>
            <div class="label">{{ stat.label | translate }}</div>
            <p>{{ stat.text | translate }}</p>
          </div>
        }
      </div>

      <div class="contact-grid">
        <div class="card-panel contact">
          <h2>{{ 'about.contactTitle' | translate }}</h2>
          <a [href]="clinic.whatsAppUrl" target="_blank" rel="noopener" class="line">
            <mat-icon>call</mat-icon> {{ clinic.whatsAppNumber }}
          </a>
          <a [href]="clinic.instagramUrl" target="_blank" rel="noopener" class="line">
            <mat-icon>photo_camera</mat-icon> {{ 'about.instagram' | translate }}
          </a>
          <span class="line"><mat-icon>place</mat-icon> {{ clinic.address }}</span>
          <span class="line"><mat-icon>schedule</mat-icon> {{ 'footer.hours' | translate }}</span>
          <span class="line indent">{{ 'footer.hoursThursday' | translate }}</span>
          <p class="online-note">{{ 'about.onlineNote' | translate }}</p>
          <a mat-flat-button class="gold-btn" [href]="clinic.whatsAppUrl" target="_blank" rel="noopener">
            <mat-icon>chat</mat-icon> {{ 'about.contactCta' | translate }}
          </a>
        </div>
        <div class="card-panel map-panel">
          <iframe
            [src]="mapUrl"
            width="100%"
            height="100%"
            style="border: 0"
            loading="lazy"
            referrerpolicy="no-referrer-when-downgrade"
            title="Peta lokasi Ardayasa"
          ></iframe>
        </div>
      </div>
    </div>
  `,
  styles: `
    .page { padding-top: 48px; }
    .story { display: flex; gap: 28px; align-items: center; padding: 32px; margin-bottom: 24px; }
    .logo { width: 120px; height: 120px; border-radius: 20px; flex-shrink: 0; }
    .story p { color: var(--text-main); line-height: 1.75; margin: 0 0 12px; }
    .hashtag { color: var(--accent-gold); font-weight: 500; }

    .milestones { display: grid; grid-template-columns: repeat(3, 1fr); gap: 20px; margin-bottom: 24px; }
    .milestone { padding: 28px 24px; text-align: center; border: 1px solid rgba(244, 217, 111, 0.3); }
    .value { font-family: var(--font-display); font-size: 2.2rem; font-weight: 700; color: var(--accent-gold); }
    .label { font-weight: 600; margin: 4px 0 8px; }
    .milestone p { color: var(--text-muted); font-size: 0.85rem; margin: 0; }

    .contact-grid { display: grid; grid-template-columns: 1fr 1.2fr; gap: 20px; }
    .contact { padding: 28px; display: flex; flex-direction: column; gap: 6px; }
    .contact h2 { margin: 0 0 10px; font-size: 1.3rem; }
    .line { display: flex; align-items: center; gap: 10px; color: var(--text-muted); text-decoration: none; padding: 4px 0; font-size: 0.925rem; }
    a.line:hover { color: var(--accent-gold); }
    .line mat-icon { color: var(--accent-gold); font-size: 20px; width: 20px; height: 20px; }
    .line.indent { padding-left: 30px; }
    .online-note { color: var(--teal-muted); font-size: 0.85rem; margin: 10px 0 16px; }
    .gold-btn { --mat-button-filled-container-color: var(--accent-gold); --mat-button-filled-label-text-color: #071c1f; font-weight: 600; align-self: flex-start; }
    .map-panel { overflow: hidden; min-height: 320px; }

    @media (max-width: 800px) {
      .story { flex-direction: column; text-align: center; }
      .milestones { grid-template-columns: 1fr; }
      .contact-grid { grid-template-columns: 1fr; }
    }
  `,
})
export class AboutPage {
  private readonly seo = inject(SeoService);
  private readonly translate = inject(TranslateService);
  private readonly sanitizer = inject(DomSanitizer);

  protected readonly clinic = CLINIC;
  protected readonly mapUrl: SafeResourceUrl = this.sanitizer.bypassSecurityTrustResourceUrl(
    CLINIC.mapsEmbedUrl,
  );

  protected readonly stats = [
    { value: '135+', label: 'about.statClients', text: 'about.statClientsText' },
    { value: '205+', label: 'about.statSessions', text: 'about.statSessionsText' },
    { value: '3', label: 'about.statCollaborations', text: 'about.statCollaborationsText' },
  ];

  constructor() {
    this.seo.update({
      title: this.translate.instant('about.title'),
      description: this.translate.instant('about.metaDescription'),
    });
  }
}
