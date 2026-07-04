import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, TranslatePipe,
  ],
  template: `
    <mat-card class="auth-card">
      <h1>{{ 'auth.register.title' | translate }}</h1>
      @if (done()) {
        <p class="success">{{ 'auth.register.success' | translate }}</p>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field appearance="outline">
            <mat-label>{{ 'auth.register.fullName' | translate }}</mat-label>
            <input matInput formControlName="fullName" autocomplete="name" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'auth.email' | translate }}</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'auth.register.whatsapp' | translate }}</mat-label>
            <input matInput type="tel" formControlName="whatsAppNumber" placeholder="+62812xxxxxxx" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'auth.password' | translate }}</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="new-password" />
          </mat-form-field>
          @if (errorKey(); as key) {
            <p class="error">{{ key | translate }}</p>
          }
          <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">
            {{ 'auth.register.submit' | translate }}
          </button>
        </form>
      }
      <p class="links">
        {{ 'auth.register.haveAccount' | translate }}
        <a routerLink="/masuk">{{ 'auth.register.loginLink' | translate }}</a>
      </p>
    </mat-card>
  `,
  styleUrl: './auth.scss',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  protected readonly busy = signal(false);
  protected readonly done = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    whatsAppNumber: ['', [Validators.required, Validators.pattern(/^\+?[0-9]{9,15}$/)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected submit(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    this.errorKey.set(null);
    const { fullName, email, whatsAppNumber, password } = this.form.getRawValue();
    this.auth.register(fullName, email, whatsAppNumber, password).subscribe({
      next: () => this.done.set(true),
      error: (err: unknown) => {
        this.busy.set(false);
        this.errorKey.set(errorKeyFromResponse(err));
      },
    });
  }
}
