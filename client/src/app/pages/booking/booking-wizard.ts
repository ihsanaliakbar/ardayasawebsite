import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { ContentService } from '../../core/content/content.service';
import { PsychologistDetail } from '../../core/content/content.models';
import { IdrPipe } from '../../core/idr.pipe';
import {
  BookableService,
  BookingMode,
  DaySlots,
  PatientBooking,
  Slot,
} from '../../core/scheduling/scheduling.models';
import { WibCalendarDatePipe, WibTimePipe } from '../../core/scheduling/wib';

/**
 * Patient booking wizard: service → mode → slot → confirm. Slots come from the
 * public API; creating the booking requires the Patient role (route-guarded).
 * A patient with an incomplete intake is redirected to the Data Pribadi form
 * (booking.intake_incomplete — clinic decision 2026-07-07).
 */
@Component({
  selector: 'app-booking-wizard',
  imports: [
    RouterLink, MatButtonModule, MatCardModule, MatIconModule,
    TranslatePipe, IdrPipe, WibCalendarDatePipe, WibTimePipe,
  ],
  template: `
    <div class="page-container page">
      @if (psychologist(); as p) {
        <a [routerLink]="['/psikolog-kami', p.slug]" class="back">
          <mat-icon inline>arrow_back_ios</mat-icon> {{ p.displayName }}
        </a>
        <h1 class="section-title">{{ 'booking.title' | translate: { name: p.displayName } }}</h1>

        <!-- Step 1: service -->
        <mat-card class="panel">
          <h2>1. {{ 'booking.stepService' | translate }}</h2>
          @if (services().length === 0) {
            <p class="hint">{{ 'booking.noServices' | translate }}</p>
          }
          @for (s of services(); track s.id) {
            <button
              class="option"
              [class.selected]="service()?.id === s.id"
              (click)="pickService(s)"
            >
              <span class="option-title">{{ s.name }}</span>
              <span class="hint">
                {{ s.durationMinutes }} {{ 'services.minutes' | translate }}
                @if (s.offlinePrice != null) { · Offline {{ s.offlinePrice | idr }} }
                @if (s.onlinePrice != null) { · Online {{ s.onlinePrice | idr }} }
              </span>
            </button>
          }
        </mat-card>

        <!-- Step 2: mode -->
        @if (service(); as s) {
          <mat-card class="panel">
            <h2>2. {{ 'booking.stepMode' | translate }}</h2>
            <div class="modes">
              @if (s.offlinePrice != null) {
                <button class="option mode" [class.selected]="mode() === 'Offline'" (click)="pickMode('Offline')">
                  <mat-icon>apartment</mat-icon>
                  <span class="option-title">{{ 'enums.bookingMode.Offline' | translate }}</span>
                  <span class="hint">{{ s.offlinePrice | idr }}</span>
                </button>
              }
              @if (s.onlinePrice != null) {
                <button class="option mode" [class.selected]="mode() === 'Online'" (click)="pickMode('Online')">
                  <mat-icon>videocam</mat-icon>
                  <span class="option-title">{{ 'enums.bookingMode.Online' | translate }}</span>
                  <span class="hint">{{ s.onlinePrice | idr }}</span>
                </button>
              }
            </div>
          </mat-card>
        }

        <!-- Step 3: slot -->
        @if (service() && mode()) {
          <mat-card class="panel">
            <h2>3. {{ 'booking.stepSlot' | translate }}</h2>
            <p class="hint">{{ 'booking.slotHint' | translate }}</p>
            @if (days().length === 0) {
              <p class="hint">{{ 'booking.noSlots' | translate }}</p>
            }
            @for (day of days(); track day.date) {
              <div class="day">
                <strong>{{ day.date | wibCalendarDate }}</strong>
                <div class="chips">
                  @for (s of day.slots; track s.startUtc) {
                    <button
                      class="chip"
                      [class.selected]="slot()?.startUtc === s.startUtc"
                      (click)="slot.set(s)"
                    >{{ s.startUtc | wibTime }}</button>
                  }
                </div>
              </div>
            }
            @if (days().length > 0) {
              <button mat-stroked-button (click)="loadMore()">{{ 'booking.moreDays' | translate }}</button>
            }
          </mat-card>
        }

        <!-- Step 4: confirm -->
        @if (service() && mode() && slot(); as chosenSlot) {
          <mat-card class="panel confirm">
            <h2>4. {{ 'booking.stepConfirm' | translate }}</h2>
            <dl>
              <dt>{{ 'booking.summaryService' | translate }}</dt>
              <dd>{{ service()!.name }} · {{ 'enums.bookingMode.' + mode() | translate }}</dd>
              <dt>{{ 'booking.summaryTime' | translate }}</dt>
              <dd>{{ chosenSlot.startUtc | wibTime }}–{{ chosenSlot.endUtc | wibTime }} WIB</dd>
              <dt>{{ 'booking.summaryPrice' | translate }}</dt>
              <dd>{{ price() | idr }}</dd>
            </dl>
            <p class="hint">{{ 'booking.paymentNote' | translate }}</p>

            @if (intakeRequired()) {
              <p class="error">{{ 'booking.intakeRequired' | translate }}</p>
              <a mat-flat-button routerLink="/akun/data-pribadi">{{ 'booking.completeIntake' | translate }}</a>
            } @else {
              @if (errorKey(); as key) {
                <p class="error">{{ key | translate }}</p>
              }
              <button mat-flat-button class="gold-btn" (click)="submit()" [disabled]="busy()">
                {{ 'booking.submit' | translate }}
              </button>
            }
          </mat-card>
        }
      }
    </div>
  `,
  styles: `
    .page { padding-top: 32px; }
    .back { color: var(--text-muted); text-decoration: none; font-size: 0.9rem; }
    .section-title { margin: 8px 0 24px; }
    .panel { padding: 24px; max-width: 720px; margin: 0 auto 20px; }
    h2 { margin-top: 0; font: var(--mat-sys-title-large); }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .error { color: var(--mat-sys-error); }
    .option {
      display: flex; flex-direction: column; align-items: flex-start; gap: 2px; width: 100%;
      background: var(--mat-sys-surface-container-high); color: inherit; text-align: left;
      border: 1px solid var(--mat-sys-outline-variant); border-radius: 12px;
      padding: 12px 16px; margin-bottom: 8px; cursor: pointer; font: inherit;
    }
    .option.selected { border-color: var(--accent-gold); box-shadow: 0 0 0 1px var(--accent-gold); }
    .option-title { font-weight: 600; }
    .modes { display: flex; gap: 12px; }
    .mode { width: auto; min-width: 140px; align-items: center; }
    .mode mat-icon { color: var(--accent-gold); }
    .day { margin-bottom: 14px; }
    .chips { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 6px; }
    .chip {
      background: var(--mat-sys-surface-container-high); color: inherit; font: inherit;
      border: 1px solid var(--mat-sys-outline-variant); border-radius: 999px;
      padding: 6px 14px; cursor: pointer;
    }
    .chip.selected { border-color: var(--accent-gold); color: var(--accent-gold); font-weight: 600; }
    .confirm dt { font: var(--mat-sys-label-large); color: var(--mat-sys-on-surface-variant); }
    .confirm dd { margin: 0 0 10px; }
    .gold-btn { --mat-button-filled-container-color: var(--accent-gold); --mat-button-filled-label-text-color: #071c1f; font-weight: 600; }
  `,
})
export class BookingWizard implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly content = inject(ContentService);

  protected readonly psychologist = signal<PsychologistDetail | null>(null);
  protected readonly services = signal<BookableService[]>([]);
  protected readonly service = signal<BookableService | null>(null);
  protected readonly mode = signal<BookingMode | null>(null);
  protected readonly days = signal<DaySlots[]>([]);
  protected readonly slot = signal<Slot | null>(null);
  protected readonly busy = signal(false);
  protected readonly errorKey = signal<string | null>(null);
  protected readonly intakeRequired = signal(false);

  private slug = '';
  private rangeDays = 14;

  ngOnInit(): void {
    this.slug = this.route.snapshot.paramMap.get('slug')!;
    this.content.getPsychologist(this.slug).subscribe({
      next: (p) => this.psychologist.set(p),
      error: () => void this.router.navigateByUrl('/psikolog-kami'),
    });
    this.http
      .get<BookableService[]>(`/api/psychologists/${this.slug}/services`)
      .subscribe((rows) => this.services.set(rows));
  }

  protected pickService(service: BookableService): void {
    this.service.set(service);
    this.slot.set(null);
    // Preselect the only available mode.
    const mode: BookingMode | null =
      service.offlinePrice != null && service.onlinePrice == null ? 'Offline'
      : service.onlinePrice != null && service.offlinePrice == null ? 'Online'
      : null;
    this.mode.set(mode);
    this.loadSlots();
  }

  protected pickMode(mode: BookingMode): void {
    this.mode.set(mode);
  }

  protected price(): number | null {
    const s = this.service();
    return s == null || this.mode() == null ? null : this.mode() === 'Offline' ? s.offlinePrice : s.onlinePrice;
  }

  protected loadMore(): void {
    this.rangeDays = Math.min(this.rangeDays + 14, 31);
    this.loadSlots();
  }

  protected submit(): void {
    const service = this.service();
    const slot = this.slot();
    const psychologist = this.psychologist();
    if (!service || !slot || !this.mode() || !psychologist) return;

    this.busy.set(true);
    this.errorKey.set(null);
    this.http
      .post<PatientBooking>('/api/bookings', {
        psychologistId: psychologist.id,
        serviceId: service.id,
        mode: this.mode(),
        startUtc: slot.startUtc,
      })
      .subscribe({
        next: (booking) => void this.router.navigate(['/akun/booking', booking.id]),
        error: (err: unknown) => {
          this.busy.set(false);
          const key = errorKeyFromResponse(err);
          if (key === 'apiErrors.booking_intake_incomplete') {
            this.intakeRequired.set(true);
            return;
          }
          this.errorKey.set(key);
          // The slot may have just been taken — refresh the picker.
          this.slot.set(null);
          this.loadSlots();
        },
      });
  }

  private loadSlots(): void {
    const service = this.service();
    if (!service) return;
    this.http
      .get<DaySlots[]>(`/api/psychologists/${this.slug}/slots`, {
        params: { serviceId: service.id, to: this.toDateParam() },
      })
      .subscribe((days) => this.days.set(days));
  }

  /** WIB end date for the slot range ('YYYY-MM-DD'). */
  private toDateParam(): string {
    const end = new Date(Date.now() + (this.rangeDays - 1) * 24 * 60 * 60 * 1000);
    return new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Jakarta' }).format(end);
  }
}
