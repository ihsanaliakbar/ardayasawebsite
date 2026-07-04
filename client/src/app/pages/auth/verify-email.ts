import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-verify-email',
  imports: [RouterLink, MatCardModule, MatButtonModule, TranslatePipe],
  template: `
    <mat-card class="auth-card">
      <h1>{{ 'auth.verify.title' | translate }}</h1>
      @switch (status()) {
        @case ('pending') {
          <p>{{ 'auth.verify.verifying' | translate }}</p>
        }
        @case ('ok') {
          <p class="success">{{ 'auth.verify.success' | translate }}</p>
          <a mat-flat-button routerLink="/masuk">{{ 'auth.login.title' | translate }}</a>
        }
        @case ('fail') {
          <p class="error">{{ 'auth.verify.failed' | translate }}</p>
        }
      }
    </mat-card>
  `,
  styleUrl: './auth.scss',
})
export class VerifyEmail implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  protected readonly status = signal<'pending' | 'ok' | 'fail'>('pending');

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const email = params.get('email');
    const token = params.get('token');
    if (!email || !token) {
      this.status.set('fail');
      return;
    }

    this.auth.verifyEmail(email, token).subscribe({
      next: () => this.status.set('ok'),
      error: () => this.status.set('fail'),
    });
  }
}
