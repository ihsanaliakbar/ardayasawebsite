import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-account-home',
  imports: [MatCardModule, TranslatePipe],
  template: `
    <h1>{{ 'account.title' | translate }}</h1>
    @if (auth.user(); as user) {
      <mat-card class="profile">
        <p class="welcome">{{ 'account.welcome' | translate: { name: user.fullName } }}</p>
        <dl>
          <dt>{{ 'account.email' | translate }}</dt>
          <dd>{{ user.email }}</dd>
          <dt>{{ 'account.whatsapp' | translate }}</dt>
          <dd>{{ user.whatsAppNumber ?? '—' }}</dd>
        </dl>
      </mat-card>
    }
  `,
  styles: `
    h1 { font: var(--mat-sys-headline-medium); }
    .profile { padding: 24px; max-width: 480px; }
    .welcome { font: var(--mat-sys-title-medium); margin-top: 0; }
    dt { font: var(--mat-sys-label-large); color: var(--mat-sys-on-surface-variant); }
    dd { margin: 0 0 12px; }
  `,
})
export class AccountHome {
  protected readonly auth = inject(AuthService);
}
