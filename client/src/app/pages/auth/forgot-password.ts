import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, TranslatePipe,
  ],
  template: `
    <mat-card class="auth-card">
      <h1>{{ 'auth.forgot.title' | translate }}</h1>
      @if (done()) {
        <p class="success">{{ 'auth.forgot.success' | translate }}</p>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field appearance="outline">
            <mat-label>{{ 'auth.email' | translate }}</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
          </mat-form-field>
          <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">
            {{ 'auth.forgot.submit' | translate }}
          </button>
        </form>
      }
    </mat-card>
  `,
  styleUrl: './auth.scss',
})
export class ForgotPassword {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  protected readonly busy = signal(false);
  protected readonly done = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  protected submit(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    // The API always answers success (no account enumeration), so the UI does too.
    this.auth.forgotPassword(this.form.getRawValue().email).subscribe({
      next: () => this.done.set(true),
      error: () => this.done.set(true),
    });
  }
}
