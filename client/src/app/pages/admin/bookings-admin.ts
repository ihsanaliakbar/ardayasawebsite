import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { IdrPipe } from '../../core/idr.pipe';
import {
  BOOKING_STATUSES,
  BookingStatus,
  PagedBookings,
  StaffBooking,
} from '../../core/scheduling/scheduling.models';
import { WibDatePipe, WibTimePipe } from '../../core/scheduling/wib';

/**
 * Admin booking list (Phase 2: overview + meeting links; the payment
 * verification actions arrive with the Phase 3 state-machine endpoints).
 */
@Component({
  selector: 'app-bookings-admin',
  imports: [
    FormsModule, MatButtonModule, MatCardModule, MatIconModule,
    TranslatePipe, IdrPipe, WibDatePipe, WibTimePipe,
  ],
  template: `
    <mat-card class="panel">
      <h2>{{ 'adminBookings.title' | translate }}</h2>
      <p class="hint">{{ 'adminBookings.hint' | translate }}</p>

      <div class="filter">
        <label>{{ 'adminBookings.statusFilter' | translate }}</label>
        <select [ngModel]="status()" (ngModelChange)="setStatus($event)">
          <option value="">{{ 'adminBookings.allStatuses' | translate }}</option>
          @for (s of statuses; track s) {
            <option [value]="s">{{ 'enums.bookingStatus.' + s | translate }}</option>
          }
        </select>
      </div>

      @if (errorKey(); as key) {
        <p class="error">{{ key | translate }}</p>
      }

      @for (b of bookings(); track b.id) {
        <div class="row">
          <div class="when">
            <strong>{{ b.startUtc | wibDate }}</strong>
            <span>{{ b.startUtc | wibTime }}–{{ b.endUtc | wibTime }} WIB</span>
            <span class="badge" [class]="'status-' + b.status">{{ 'enums.bookingStatus.' + b.status | translate }}</span>
          </div>
          <div class="who">
            <strong>{{ b.patientName }}</strong>
            <span class="hint">{{ b.patientWhatsApp ?? '—' }}</span>
            <span class="hint">{{ b.serviceName }} · {{ b.psychologistName }}</span>
            <span class="hint">{{ 'enums.bookingMode.' + b.mode | translate }} · {{ b.priceIdr | idr }}</span>
          </div>
          <div class="actions">
            @if (b.mode === 'Online') {
              <input
                type="url"
                [placeholder]="'adminBookings.zoomPlaceholder' | translate"
                [(ngModel)]="zoomDrafts[b.id]"
              />
              <button mat-stroked-button (click)="saveZoom(b)" [disabled]="!zoomDrafts[b.id]">
                {{ 'adminBookings.saveZoom' | translate }}
              </button>
            }
          </div>
        </div>
      } @empty {
        <p class="hint">{{ 'adminBookings.empty' | translate }}</p>
      }

      @if (totalCount() > pageSize) {
        <div class="pager">
          <button mat-stroked-button [disabled]="page() <= 1" (click)="goto(page() - 1)">
            {{ 'articles.previous' | translate }}
          </button>
          <span>{{ page() }} / {{ lastPage() }}</span>
          <button mat-stroked-button [disabled]="page() >= lastPage()" (click)="goto(page() + 1)">
            {{ 'articles.next' | translate }}
          </button>
        </div>
      }
    </mat-card>
  `,
  styles: `
    h2 { font: var(--mat-sys-title-large); margin-top: 0; }
    .panel { padding: 24px; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .error { color: var(--mat-sys-error); }
    .filter { display: flex; align-items: center; gap: 12px; margin: 12px 0 4px; }
    select, input[type='url'] {
      background: var(--mat-sys-surface-container-high); color: inherit;
      border: 1px solid var(--mat-sys-outline-variant); border-radius: 8px; padding: 8px 10px;
      font: inherit; color-scheme: dark;
    }
    .row {
      display: grid; grid-template-columns: 180px 1fr auto; gap: 16px; align-items: center;
      padding: 14px 0; border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .row:last-of-type { border-bottom: none; }
    .when, .who { display: flex; flex-direction: column; gap: 2px; }
    .actions { display: flex; gap: 8px; align-items: center; }
    .actions input { width: 220px; }
    .badge {
      font: var(--mat-sys-label-medium); padding: 2px 10px; border-radius: 12px; width: fit-content;
      background: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface-variant); margin-top: 4px;
    }
    .status-Confirmed { background: var(--mat-sys-primary-container); color: var(--mat-sys-on-primary-container); }
    .status-PendingPayment, .status-AwaitingVerification { border: 1px solid var(--accent-gold); color: var(--accent-gold); background: transparent; }
    .pager { display: flex; align-items: center; gap: 16px; justify-content: center; margin-top: 16px; }
    @media (max-width: 720px) { .row { grid-template-columns: 1fr; } }
  `,
})
export class BookingsAdmin implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly statuses = BOOKING_STATUSES;
  protected readonly pageSize = 20;
  protected readonly bookings = signal<StaffBooking[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);
  protected readonly status = signal<BookingStatus | ''>('');
  protected readonly errorKey = signal<string | null>(null);
  protected zoomDrafts: Record<string, string> = {};

  ngOnInit(): void {
    this.reload();
  }

  protected setStatus(status: BookingStatus | ''): void {
    this.status.set(status);
    this.page.set(1);
    this.reload();
  }

  protected goto(page: number): void {
    this.page.set(page);
    this.reload();
  }

  protected lastPage(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize));
  }

  protected saveZoom(booking: StaffBooking): void {
    this.errorKey.set(null);
    this.http
      .put<StaffBooking>(`/api/admin/bookings/${booking.id}/zoom-link`, { zoomLink: this.zoomDrafts[booking.id] })
      .subscribe({
        next: () => this.reload(),
        error: (err: unknown) => this.errorKey.set(errorKeyFromResponse(err)),
      });
  }

  private reload(): void {
    const status = this.status() ? `&status=${this.status()}` : '';
    this.http
      .get<PagedBookings>(`/api/admin/bookings?page=${this.page()}&pageSize=${this.pageSize}${status}`)
      .subscribe((result) => {
        this.bookings.set(result.items);
        this.totalCount.set(result.totalCount);
        this.zoomDrafts = Object.fromEntries(result.items.map((b) => [b.id, b.zoomLink ?? '']));
      });
  }
}
