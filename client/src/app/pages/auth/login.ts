import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, TranslatePipe,
  ],
  template: `
    <mat-card class="auth-card">
      <h1>{{ 'auth.login.title' | translate }}</h1>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'auth.email' | translate }}</mat-label>
          <input matInput type="email" formControlName="email" autocomplete="email" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'auth.password' | translate }}</mat-label>
          <input matInput type="password" formControlName="password" autocomplete="current-password" />
        </mat-form-field>
        @if (errorKey(); as key) {
          <p class="error">{{ key | translate }}</p>
        }
        @if (errorKey() === 'apiErrors.auth_email_not_verified') {
          <button mat-stroked-button type="button" (click)="resendVerification()">
            {{ 'auth.login.resendVerification' | translate }}
          </button>
        }
        @if (verificationResent()) {
          <p class="success">{{ 'auth.login.verificationSent' | translate }}</p>
        }
        <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">
          {{ 'auth.login.submit' | translate }}
        </button>
      </form>
      <p class="links">
        <a routerLink="/lupa-kata-sandi">{{ 'auth.login.forgot' | translate }}</a>
      </p>
      <p class="links">
        {{ 'auth.login.noAccount' | translate }}
        <a routerLink="/daftar">{{ 'auth.login.registerLink' | translate }}</a>
      </p>
    </mat-card>
  `,
  styleUrl: './auth.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  protected readonly busy = signal(false);
  protected readonly errorKey = signal<string | null>(null);
  protected readonly verificationResent = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected submit(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    this.errorKey.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => {
        const redirect = this.route.snapshot.queryParamMap.get('redirect');
        this.router.navigateByUrl(redirect ?? this.auth.homePath());
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.errorKey.set(errorKeyFromResponse(err));
      },
    });
  }

  protected resendVerification(): void {
    const email = this.form.getRawValue().email;
    if (!email) return;
    this.auth.resendVerification(email).subscribe(() => {
      this.verificationResent.set(true);
      this.errorKey.set(null);
    });
  }
}
