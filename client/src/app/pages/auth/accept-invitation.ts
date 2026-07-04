import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-accept-invitation',
  imports: [
    ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, TranslatePipe,
  ],
  template: `
    <mat-card class="auth-card">
      <h1>{{ 'auth.invite.title' | translate }}</h1>
      @if (done()) {
        <p class="success">{{ 'auth.invite.success' | translate }}</p>
        <a mat-flat-button routerLink="/masuk">{{ 'auth.login.title' | translate }}</a>
      } @else {
        <p>{{ 'auth.invite.intro' | translate }}</p>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field appearance="outline">
            <mat-label>{{ 'auth.password' | translate }}</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="new-password" />
          </mat-form-field>
          @if (errorKey(); as key) {
            <p class="error">{{ key | translate }}</p>
          }
          <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">
            {{ 'auth.invite.submit' | translate }}
          </button>
        </form>
      }
    </mat-card>
  `,
  styleUrl: './auth.scss',
})
export class AcceptInvitation {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  protected readonly busy = signal(false);
  protected readonly done = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected submit(): void {
    if (this.form.invalid) return;
    const params = this.route.snapshot.queryParamMap;
    const email = params.get('email');
    const token = params.get('token');
    if (!email || !token) {
      this.errorKey.set('apiErrors.auth_invalid_token');
      return;
    }

    this.busy.set(true);
    this.errorKey.set(null);
    this.auth.acceptInvitation(email, token, this.form.getRawValue().password).subscribe({
      next: () => this.done.set(true),
      error: (err: unknown) => {
        this.busy.set(false);
        this.errorKey.set(errorKeyFromResponse(err));
      },
    });
  }
}
