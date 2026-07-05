import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, MatIconModule, TranslatePipe],
  template: `
    <div class="page-container admin">
      <h1>{{ 'admin.title' | translate }}</h1>
      <nav class="tabs">
        @for (tab of tabs; track tab.path) {
          <a
            [routerLink]="tab.path"
            routerLinkActive="active"
            [routerLinkActiveOptions]="{ exact: tab.exact }"
          ><mat-icon inline>{{ tab.icon }}</mat-icon> {{ tab.label | translate }}</a>
        }
      </nav>
      <router-outlet />
    </div>
  `,
  styles: `
    .admin { padding-top: 32px; }
    h1 { font: var(--mat-sys-headline-medium); font-family: var(--font-display); }
    .tabs {
      display: flex; gap: 4px; flex-wrap: wrap; border-bottom: 1px solid var(--mat-sys-outline-variant);
      margin-bottom: 24px;
    }
    .tabs a {
      color: var(--mat-sys-on-surface-variant); text-decoration: none; padding: 10px 14px;
      font-size: 0.9rem; border-bottom: 2px solid transparent; display: flex; align-items: center; gap: 6px;
    }
    .tabs a.active { color: var(--accent-gold); border-bottom-color: var(--accent-gold); }
  `,
})
export class AdminLayout {
  protected readonly tabs = [
    { path: '/admin', icon: 'group', label: 'admin.tabs.psychologists', exact: true },
    { path: '/admin/pasien', icon: 'people_alt', label: 'admin.tabs.patients', exact: true },
    { path: '/admin/artikel', icon: 'article', label: 'admin.tabs.articles', exact: false },
    { path: '/admin/faq', icon: 'quiz', label: 'admin.tabs.faq', exact: true },
    { path: '/admin/testimoni', icon: 'reviews', label: 'admin.tabs.testimonials', exact: true },
    { path: '/admin/layanan', icon: 'sell', label: 'admin.tabs.services', exact: true },
  ];
}
