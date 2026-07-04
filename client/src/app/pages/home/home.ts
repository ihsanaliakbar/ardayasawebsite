import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-home',
  imports: [RouterLink, MatButtonModule, TranslatePipe],
  template: `
    <section class="hero">
      <h1>{{ 'home.heroTitle' | translate }}</h1>
      <p>{{ 'home.heroSubtitle' | translate }}</p>
      <a mat-flat-button routerLink="/daftar">{{ 'home.cta' | translate }}</a>
      <p class="note">{{ 'home.comingSoon' | translate }}</p>
    </section>
  `,
  styles: `
    .hero { text-align: center; padding: 64px 16px; }
    h1 { font: var(--mat-sys-display-small); margin-bottom: 12px; }
    p { font: var(--mat-sys-body-large); color: var(--mat-sys-on-surface-variant); margin-bottom: 24px; }
    .note { margin-top: 40px; font: var(--mat-sys-body-small); }
  `,
})
export class Home {}
