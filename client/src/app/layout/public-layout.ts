import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../core/auth/auth.service';
import { CLINIC } from '../core/clinic';

@Component({
  selector: 'app-public-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule, MatIconModule, TranslatePipe],
  template: `
    <header class="header">
      <div class="header-inner page-container">
        <a routerLink="/" class="brand" (click)="menuOpen.set(false)">
          <img src="/images/ardayasa-logo.jpg" alt="" class="brand-logo" />
          <span class="brand-text">
            <span class="brand-name">Ardayasa</span>
            <span class="brand-sub">{{ 'app.subtitle' | translate }}</span>
          </span>
        </a>

        <nav class="nav" [class.open]="menuOpen()">
          @for (item of navItems; track item.path) {
            <a
              [routerLink]="item.path"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: item.path === '/' }"
              (click)="menuOpen.set(false)"
            >{{ item.label | translate }}</a>
          }
          <div class="nav-auth">
            @if (auth.isLoggedIn()) {
              <a mat-button [routerLink]="auth.homePath()" (click)="menuOpen.set(false)">{{ accountLabelKey() | translate }}</a>
              <button mat-button (click)="logout()">{{ 'nav.logout' | translate }}</button>
            } @else {
              <a mat-button routerLink="/masuk" (click)="menuOpen.set(false)">{{ 'nav.login' | translate }}</a>
            }
            <a mat-flat-button class="cta" [href]="clinic.whatsAppUrl" target="_blank" rel="noopener">
              <mat-icon>event</mat-icon>
              {{ 'nav.book' | translate }}
            </a>
          </div>
        </nav>

        <button class="menu-toggle" mat-icon-button (click)="menuOpen.set(!menuOpen())" aria-label="Menu">
          <mat-icon>{{ menuOpen() ? 'close' : 'menu' }}</mat-icon>
        </button>
      </div>
    </header>

    <main class="content">
      <router-outlet />
    </main>

    <footer class="footer">
      <div class="page-container footer-grid">
        <div class="footer-brand">
          <a routerLink="/" class="brand">
            <img src="/images/ardayasa-logo.jpg" alt="" class="brand-logo" />
            <span class="brand-text">
              <span class="brand-name">Ardayasa</span>
              <span class="brand-sub">{{ 'app.subtitle' | translate }}</span>
            </span>
          </a>
          <p>{{ 'footer.blurb' | translate }}</p>
          <a [href]="clinic.instagramUrl" target="_blank" rel="noopener" class="footer-link">Instagram</a>
        </div>

        <div>
          <h4>{{ 'footer.links' | translate }}</h4>
          @for (item of navItems; track item.path) {
            <a [routerLink]="item.path" class="footer-link">{{ item.label | translate }}</a>
          }
        </div>

        <div>
          <h4>{{ 'footer.services' | translate }}</h4>
          <a routerLink="/layanan" class="footer-link">{{ 'footer.serviceCounseling' | translate }}</a>
          <a routerLink="/layanan" class="footer-link">{{ 'footer.serviceAssessment' | translate }}</a>
          <a routerLink="/layanan" class="footer-link">{{ 'footer.servicePsychotherapy' | translate }}</a>
          <a routerLink="/layanan" class="footer-link">{{ 'footer.serviceConsultation' | translate }}</a>
        </div>

        <div>
          <h4>{{ 'footer.contact' | translate }}</h4>
          <a [href]="clinic.whatsAppUrl" target="_blank" rel="noopener" class="footer-link">
            <mat-icon inline>call</mat-icon> {{ clinic.whatsAppNumber }}
          </a>
          <a [href]="clinic.mapsUrl" target="_blank" rel="noopener" class="footer-link">
            <mat-icon inline>place</mat-icon> {{ clinic.address }}
          </a>
          <span class="footer-link">
            <mat-icon inline>schedule</mat-icon> {{ 'footer.hours' | translate }}
          </span>
          <span class="footer-link footer-hours-note">{{ 'footer.hoursThursday' | translate }}</span>
        </div>
      </div>
      <div class="footer-bottom">
        <span>© {{ year }} {{ 'app.title' | translate }}. {{ 'footer.rights' | translate }}</span>
      </div>
    </footer>
  `,
  styles: `
    :host { display: flex; flex-direction: column; min-height: 100vh; }

    .header {
      position: sticky; top: 0; z-index: 20;
      background: rgba(7, 28, 31, 0.92);
      backdrop-filter: blur(8px);
      border-bottom: 1px solid rgba(102, 146, 152, 0.2);
    }
    .header-inner { display: flex; align-items: center; gap: 16px; height: 68px; }

    .brand { display: flex; align-items: center; gap: 10px; text-decoration: none; color: inherit; }
    .brand-logo { width: 40px; height: 40px; border-radius: 10px; }
    .brand-text { display: flex; flex-direction: column; line-height: 1.15; }
    .brand-name { font-family: var(--font-display); font-size: 1.25rem; font-weight: 600; }
    .brand-sub { font-size: 0.7rem; color: var(--text-muted); letter-spacing: 0.02em; }

    .nav { display: flex; align-items: center; gap: 4px; margin-left: auto; }
    .nav > a {
      color: var(--text-main); text-decoration: none; font-size: 0.925rem;
      padding: 8px 12px; border-radius: 8px;
    }
    .nav > a:hover { background: rgba(33, 83, 90, 0.5); }
    .nav > a.active { color: var(--accent-gold); border-bottom: 2px solid var(--accent-gold); border-radius: 8px 8px 0 0; }
    .nav-auth { display: flex; align-items: center; gap: 8px; margin-left: 12px; }
    .cta { --mat-button-filled-container-color: var(--accent-gold); --mat-button-filled-label-text-color: #071c1f; font-weight: 600; }

    .menu-toggle { display: none; margin-left: auto; }

    @media (max-width: 960px) {
      .menu-toggle { display: inline-flex; }
      .nav {
        display: none; position: absolute; top: 68px; left: 0; right: 0;
        background: var(--bg-teal); flex-direction: column; align-items: stretch;
        padding: 12px 20px 20px; gap: 2px; border-bottom: 1px solid rgba(102, 146, 152, 0.3);
      }
      .nav.open { display: flex; }
      .nav-auth { margin: 12px 0 0; flex-wrap: wrap; }
    }

    .content { flex: 1; }

    .footer { background: var(--bg-teal); border-top: 1px solid rgba(102, 146, 152, 0.2); margin-top: 64px; }
    .footer-grid {
      display: grid; grid-template-columns: 1.4fr 1fr 1fr 1.4fr; gap: 32px;
      padding-top: 40px; padding-bottom: 32px;
    }
    .footer h4 { font-family: var(--font-body); font-size: 0.95rem; margin: 4px 0 12px; color: var(--text-main); }
    .footer-brand p { color: var(--text-muted); font-size: 0.875rem; max-width: 30ch; }
    .footer-link {
      display: block; color: var(--text-muted); text-decoration: none;
      font-size: 0.875rem; padding: 3px 0;
    }
    a.footer-link:hover { color: var(--accent-gold); }
    .footer-link mat-icon { font-size: 1rem; vertical-align: -2px; margin-right: 4px; }
    .footer-hours-note { padding-left: 24px; }
    .footer-bottom {
      border-top: 1px solid rgba(102, 146, 152, 0.15); padding: 16px 20px; text-align: center;
      color: var(--text-muted); font-size: 0.8rem;
    }

    @media (max-width: 720px) {
      .footer-grid { grid-template-columns: 1fr 1fr; }
    }
    @media (max-width: 480px) {
      .footer-grid { grid-template-columns: 1fr; }
    }
  `,
})
export class PublicLayout {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly year = new Date().getFullYear();
  protected readonly clinic = CLINIC;
  protected readonly menuOpen = signal(false);

  protected readonly navItems = [
    { path: '/', label: 'nav.home' },
    { path: '/layanan', label: 'nav.services' },
    { path: '/tentang-kami', label: 'nav.about' },
    { path: '/psikolog-kami', label: 'nav.psychologists' },
    { path: '/artikel', label: 'nav.articles' },
    { path: '/faq', label: 'nav.faq' },
  ];

  protected accountLabelKey(): string {
    const home = this.auth.homePath();
    if (home === '/admin') return 'nav.admin';
    if (home === '/psikolog') return 'nav.psychologist';
    return 'nav.account';
  }

  protected logout(): void {
    this.auth.logout().subscribe(() => this.router.navigateByUrl('/'));
  }
}
