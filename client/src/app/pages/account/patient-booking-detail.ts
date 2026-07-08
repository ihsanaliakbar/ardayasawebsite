import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, switchMap } from 'rxjs';
import { CLINIC } from '../../core/clinic';
import { IdrPipe } from '../../core/idr.pipe';
import { PatientBooking } from '../../core/scheduling/scheduling.models';
import { WibDatePipe, WibTimePipe } from '../../core/scheduling/wib';

@Component({
  selector: 'app-patient-booking-detail',
  imports: [RouterLink, MatCardModule, MatIconModule, TranslatePipe, IdrPipe, WibDatePipe, WibTimePipe],
  template: `
    <div class="page-container page">
      <a routerLink="/akun/booking" class="back">
        <mat-icon inline>arrow_back_ios</mat-icon> {{ 'myBookings.title' | translate }}
      </a>

      @if (booking(); as b) {
        <h1 class="section-title">{{ 'myBookings.detailTitle' | translate }}</h1>

        <mat-card class="panel">
          <span class="badge" [class.gold]="b.status === 'PendingPayment' || b.status === 'AwaitingVerification'">
            {{ 'enums.bookingStatus.' + b.status | translate }}
          </span>

          <dl>
            <dt>{{ 'myBookings.psychologist' | translate }}</dt>
            <dd>{{ b.psychologistName }}</dd>
            <dt>{{ 'booking.summaryService' | translate }}</dt>
            <dd>{{ b.serviceName }} · {{ 'enums.bookingMode.' + b.mode | translate }}</dd>
            <dt>{{ 'booking.summaryTime' | translate }}</dt>
            <dd>{{ b.startUtc | wibDate }}, {{ b.startUtc | wibTime }}–{{ b.endUtc | wibTime }} WIB</dd>
            <dt>{{ 'booking.summaryPrice' | translate }}</dt>
            <dd>{{ b.priceIdr | idr }}</dd>
          </dl>

          @if (b.status === 'PendingPayment') {
            <div class="notice">
              <mat-icon>schedule</mat-icon>
              <div>
                <p class="notice-title">{{ 'myBookings.pendingTitle' | translate }}</p>
                <p class="hint">
                  {{ 'myBookings.pendingText' | translate }}
                  @if (b.paymentDueAtUtc) {
                    {{ 'myBookings.pendingUntil' | translate: { time: (b.paymentDueAtUtc | wibTime) } }}
                  }
                </p>
                <a [href]="clinic.whatsAppUrl" target="_blank" rel="noopener">{{ 'myBookings.contactAdmin' | translate }}</a>
              </div>
            </div>
          }

          @if (b.mode === 'Online') {
            <div class="notice">
              <mat-icon>videocam</mat-icon>
              <div>
                @if (b.zoomLink) {
                  <p class="notice-title">{{ 'myBookings.zoomTitle' | translate }}</p>
                  <a [href]="b.zoomLink" target="_blank" rel="noopener">{{ b.zoomLink }}</a>
                } @else {
                  <p class="hint">{{ 'myBookings.zoomPending' | translate }}</p>
                }
              </div>
            </div>
          }
        </mat-card>
      }
    </div>
  `,
  styles: `
    .page { padding-top: 32px; }
    .back { color: var(--mat-sys-on-surface-variant); text-decoration: none; font-size: 0.9rem; }
    .section-title { margin: 8px 0 24px; }
    .panel { padding: 24px; max-width: 640px; margin: 0 auto; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; margin: 4px 0; }
    dt { font: var(--mat-sys-label-large); color: var(--mat-sys-on-surface-variant); margin-top: 12px; }
    dd { margin: 0; }
    .badge {
      font: var(--mat-sys-label-medium); padding: 4px 12px; border-radius: 12px;
      background: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface-variant);
      width: fit-content; display: inline-block;
    }
    .badge.gold { background: transparent; border: 1px solid var(--accent-gold); color: var(--accent-gold); }
    .notice {
      display: flex; gap: 12px; align-items: flex-start; margin-top: 20px;
      border-left: 3px solid var(--accent-gold); padding: 8px 0 8px 16px;
    }
    .notice mat-icon { color: var(--accent-gold); }
    .notice-title { font: var(--mat-sys-title-small); margin: 0; }
    .notice a { color: var(--accent-gold); word-break: break-all; }
  `,
})
export class PatientBookingDetail {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly clinic = CLINIC;

  protected readonly booking = toSignal(
    this.route.paramMap.pipe(
      switchMap((params) =>
        this.http.get<PatientBooking>(`/api/me/bookings/${params.get('id')}`).pipe(
          catchError(() => {
            void this.router.navigateByUrl('/akun/booking');
            return EMPTY;
          }),
        ),
      ),
    ),
  );
}
