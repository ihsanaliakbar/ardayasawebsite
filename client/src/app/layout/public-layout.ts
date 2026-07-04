import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../core/auth/auth.service';

@Component({
  selector: 'app-public-layout',
  imports: [RouterOutlet, RouterLink, MatToolbarModule, MatButtonModule, TranslatePipe],
  template: `
    <mat-toolbar class="toolbar">
      <a routerLink="/" class="brand">{{ 'app.title' | translate }}</a>
      <span class="spacer"></span>
      @if (auth.isLoggedIn()) {
        <a mat-button [routerLink]="auth.homePath()">{{ accountLabelKey() | translate }}</a>
        <button mat-button (click)="logout()">{{ 'nav.logout' | translate }}</button>
      } @else {
        <a mat-button routerLink="/masuk">{{ 'nav.login' | translate }}</a>
        <a mat-stroked-button routerLink="/daftar">{{ 'nav.register' | translate }}</a>
      }
    </mat-toolbar>

    <main class="content">
      <router-outlet />
    </main>

    <footer class="footer">
      <span>© {{ year }} {{ 'app.title' | translate }}</span>
    </footer>
  `,
  styles: `
    :host { display: flex; flex-direction: column; min-height: 100vh; }
    .toolbar { gap: 8px; }
    .brand { color: inherit; text-decoration: none; font-weight: 600; white-space: normal; line-height: 1.2; }
    .spacer { flex: 1; }
    .content { flex: 1; padding: 24px; max-width: 960px; width: 100%; margin: 0 auto; box-sizing: border-box; }
    .footer { padding: 16px 24px; text-align: center; color: var(--mat-sys-on-surface-variant); font: var(--mat-sys-body-small); }
  `,
})
export class PublicLayout {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly year = new Date().getFullYear();

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
