import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { IdrPipe } from '../../core/idr.pipe';
import { PatientBooking } from '../../core/scheduling/scheduling.models';
import { WibDatePipe, WibTimePipe } from '../../core/scheduling/wib';

@Component({
  selector: 'app-patient-bookings',
  imports: [
    RouterLink, MatButtonModule, MatCardModule, MatIconModule,
    TranslatePipe, IdrPipe, WibDatePipe, WibTimePipe,
  ],
  template: `
    <div class="page-container page">
      <a routerLink="/akun" class="back"><mat-icon inline>arrow_back_ios</mat-icon> {{ 'account.title' | translate }}</a>
      <h1 class="section-title">{{ 'myBookings.title' | translate }}</h1>

      <mat-card class="panel">
        @for (b of bookings(); track b.id) {
          <div class="row">
            <div>
              <strong>{{ b.startUtc | wibDate }} · {{ b.startUtc | wibTime }} WIB</strong>
              <span class="badge" [class.gold]="b.status === 'PendingPayment' || b.status === 'AwaitingVerification'">
                {{ 'enums.bookingStatus.' + b.status | translate }}
              </span>
              <br />
              <span class="hint">
                {{ b.serviceName }} · {{ b.psychologistName }} ·
                {{ 'enums.bookingMode.' + b.mode | translate }} · {{ b.priceIdr | idr }}
              </span>
            </div>
            <a mat-stroked-button [routerLink]="['/akun/booking', b.id]">{{ 'myBookings.detail' | translate }}</a>
          </div>
        } @empty {
          <p class="hint">{{ 'myBookings.empty' | translate }}</p>
        }
        <a mat-flat-button routerLink="/janji-temu" class="new-booking">{{ 'myBookings.newBooking' | translate }}</a>
      </mat-card>
    </div>
  `,
  styles: `
    .page { padding-top: 32px; }
    .back { color: var(--mat-sys-on-surface-variant); text-decoration: none; font-size: 0.9rem; }
    .section-title { margin: 8px 0 24px; }
    .panel { padding: 24px; max-width: 720px; margin: 0 auto; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .row {
      display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 12px;
      padding: 12px 0; border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .row:last-child { border-bottom: none; }
    .badge {
      font: var(--mat-sys-label-medium); padding: 2px 10px; border-radius: 12px; margin-left: 8px;
      background: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface-variant);
    }
    .badge.gold { background: transparent; border: 1px solid var(--accent-gold); color: var(--accent-gold); }
    .new-booking { margin-top: 16px; }
  `,
})
export class PatientBookings implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly bookings = signal<PatientBooking[]>([]);

  ngOnInit(): void {
    this.http.get<PatientBooking[]>('/api/me/bookings').subscribe((rows) => this.bookings.set(rows));
  }
}
