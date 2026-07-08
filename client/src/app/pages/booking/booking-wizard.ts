import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { errorKeyFromResponse } from '../../core/api-error';
import { IdrPipe } from '../../core/idr.pipe';
import {
  BookableService,
  BookingMode,
  DaySlots,
  PatientBooking,
  ServicePsychologist,
  Slot,
} from '../../core/scheduling/scheduling.models';
import { WibCalendarDatePipe, WibTimePipe } from '../../core/scheduling/wib';

/**
 * Service-first booking wizard (flow decided 2026-07-08): pilih layanan →
 * pilih psikolog offering it (or "tanpa preferensi") → mode → slot → confirm.
 * Deep links: ?layanan=<serviceId> and/or ?psikolog=<slug> preselect steps.
 * The route is patient-guarded, so anonymous visitors are sent to login and
 * returned here; an incomplete intake is redirected to Data Pribadi.
 */
@Component({
  selector: 'app-booking-wizard',
  imports: [
    RouterLink, MatButtonModule, MatCardModule, MatIconModule,
    TranslatePipe, IdrPipe, WibCalendarDatePipe, WibTimePipe,
  ],
  template: `
    <div class="page-container page">
      <h1 class="section-title">{{ 'booking.title' | translate }}</h1>

      <!-- Step 1: service -->
      <mat-card class="panel">
        <h2>1. {{ 'booking.stepService' | translate }}</h2>
        @if (catalog().length === 0) {
          <p class="hint">{{ 'booking.noServices' | translate }}</p>
        }
        @for (category of categories(); track category) {
          <h3>{{ category }}</h3>
          @for (s of servicesIn(category); track s.id) {
            <button class="option" [class.selected]="service()?.id === s.id" (click)="pickService(s)">
              <span class="option-title">{{ s.name }}</span>
              <span class="hint">
                {{ s.durationMinutes }} {{ 'services.minutes' | translate }}
                @if (s.offlinePrice != null) { · Offline {{ s.offlinePrice | idr }} }
                @if (s.onlinePrice != null) { · Online {{ s.onlinePrice | idr }} }
              </span>
            </button>
          }
        }
      </mat-card>

      <!-- Step 2: psychologist (or no preference) -->
      @if (service()) {
        <mat-card class="panel">
          <h2>2. {{ 'booking.stepPsychologist' | translate }}</h2>
          <div class="psychs">
            <button class="option psych" [class.selected]="noPreference()" (click)="pickNoPreference()">
              <mat-icon>shuffle</mat-icon>
              <span class="option-title">{{ 'booking.noPreference' | translate }}</span>
              <span class="hint">{{ 'booking.noPreferenceHint' | translate }}</span>
            </button>
            @for (p of psychologists(); track p.psychologistId) {
              <button
                class="option psych"
                [class.selected]="psychologist()?.psychologistId === p.psychologistId"
                (click)="pickPsychologist(p)"
              >
                @if (p.photoUrl) {
                  <img [src]="p.photoUrl" [alt]="p.displayName" />
                } @else {
                  <mat-icon>person</mat-icon>
                }
                <span class="option-title">{{ p.displayName }}</span>
                @if (p.specialization) {
                  <span class="hint">{{ p.specialization }}</span>
                }
              </button>
            }
          </div>
        </mat-card>
      }

      <!-- Step 3: mode -->
      @if (service(); as s) {
        @if (psychologistChosen()) {
          <mat-card class="panel">
            <h2>3. {{ 'booking.stepMode' | translate }}</h2>
            <div class="modes">
              @if (s.offlinePrice != null) {
                <button class="option mode" [class.selected]="mode() === 'Offline'" (click)="mode.set('Offline')">
                  <mat-icon>apartment</mat-icon>
                  <span class="option-title">{{ 'enums.bookingMode.Offline' | translate }}</span>
                  <span class="hint">{{ s.offlinePrice | idr }}</span>
                </button>
              }
              @if (s.onlinePrice != null) {
                <button class="option mode" [class.selected]="mode() === 'Online'" (click)="mode.set('Online')">
                  <mat-icon>videocam</mat-icon>
                  <span class="option-title">{{ 'enums.bookingMode.Online' | translate }}</span>
                  <span class="hint">{{ s.onlinePrice | idr }}</span>
                </button>
              }
            </div>
          </mat-card>
        }
      }

      <!-- Step 4: slot -->
      @if (service() && psychologistChosen() && mode()) {
        <mat-card class="panel">
          <h2>4. {{ 'booking.stepSlot' | translate }}</h2>
          <p class="hint">{{ 'booking.slotHint' | translate }}</p>
          @if (days().length === 0) {
            <p class="hint">{{ 'booking.noSlots' | translate }}</p>
          }
          @for (day of days(); track day.date) {
            <div class="day">
              <strong>{{ day.date | wibCalendarDate }}</strong>
              <div class="chips">
                @for (s of day.slots; track s.startUtc) {
                  <button class="chip" [class.selected]="slot()?.startUtc === s.startUtc" (click)="slot.set(s)">
                    {{ s.startUtc | wibTime }}
                  </button>
                }
              </div>
            </div>
          }
          @if (days().length > 0 && rangeDays < 31) {
            <button mat-stroked-button (click)="loadMore()">{{ 'booking.moreDays' | translate }}</button>
          }
        </mat-card>
      }

      <!-- Step 5: confirm -->
      @if (service() && mode() && slot(); as chosenSlot) {
        <mat-card class="panel confirm">
          <h2>5. {{ 'booking.stepConfirm' | translate }}</h2>
          <dl>
            <dt>{{ 'booking.summaryService' | translate }}</dt>
            <dd>{{ service()!.name }} · {{ 'enums.bookingMode.' + mode() | translate }}</dd>
            <dt>{{ 'booking.summaryPsychologist' | translate }}</dt>
            <dd>{{ assignedPsychologistName(chosenSlot) }}</dd>
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
    </div>
  `,
  styles: `
    .page { padding-top: 32px; }
    .section-title { margin: 8px 0 24px; }
    .panel { padding: 24px; max-width: 720px; margin: 0 auto 20px; }
    h2 { margin-top: 0; font: var(--mat-sys-title-large); }
    h3 { font: var(--mat-sys-title-small); color: var(--mat-sys-on-surface-variant); margin: 14px 0 6px; }
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
    .psychs { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 10px; }
    .psych { align-items: center; text-align: center; }
    .psych img { width: 64px; height: 64px; border-radius: 50%; object-fit: cover; }
    .psych mat-icon { width: 64px; height: 64px; font-size: 64px; color: var(--mat-sys-outline); }
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

  protected readonly catalog = signal<BookableService[]>([]);
  protected readonly service = signal<BookableService | null>(null);
  protected readonly psychologists = signal<ServicePsychologist[]>([]);
  protected readonly psychologist = signal<ServicePsychologist | null>(null);
  protected readonly noPreference = signal(false);
  protected readonly mode = signal<BookingMode | null>(null);
  protected readonly days = signal<DaySlots[]>([]);
  protected readonly slot = signal<Slot | null>(null);
  protected readonly busy = signal(false);
  protected readonly errorKey = signal<string | null>(null);
  protected readonly intakeRequired = signal(false);

  protected rangeDays = 14;

  ngOnInit(): void {
    this.http.get<BookableService[]>('/api/booking/services').subscribe((services) => {
      this.catalog.set(services);
      const preselectedId = this.route.snapshot.queryParamMap.get('layanan');
      const preselected = services.find((s) => s.id === preselectedId);
      if (preselected) {
        this.pickService(preselected);
      }
    });
  }

  protected categories(): string[] {
    return [...new Set(this.catalog().map((s) => s.categoryName))];
  }

  protected servicesIn(category: string): BookableService[] {
    return this.catalog().filter((s) => s.categoryName === category);
  }

  protected pickService(service: BookableService): void {
    this.service.set(service);
    this.psychologist.set(null);
    this.noPreference.set(false);
    this.slot.set(null);
    this.days.set([]);
    // Preselect the only available mode.
    this.mode.set(
      service.offlinePrice != null && service.onlinePrice == null ? 'Offline'
      : service.onlinePrice != null && service.offlinePrice == null ? 'Online'
      : null,
    );
    this.http
      .get<ServicePsychologist[]>(`/api/booking/services/${service.id}/psychologists`)
      .subscribe((rows) => {
        this.psychologists.set(rows);
        const slug = this.route.snapshot.queryParamMap.get('psikolog');
        const preselected = rows.find((p) => p.slug === slug);
        if (preselected) {
          this.pickPsychologist(preselected);
        }
      });
  }

  protected pickPsychologist(psychologist: ServicePsychologist): void {
    this.psychologist.set(psychologist);
    this.noPreference.set(false);
    this.slot.set(null);
    this.loadSlots();
  }

  protected pickNoPreference(): void {
    this.psychologist.set(null);
    this.noPreference.set(true);
    this.slot.set(null);
    this.loadSlots();
  }

  protected psychologistChosen(): boolean {
    return this.psychologist() !== null || this.noPreference();
  }

  protected price(): number | null {
    const s = this.service();
    return s == null || this.mode() == null ? null : this.mode() === 'Offline' ? s.offlinePrice : s.onlinePrice;
  }

  /** Confirm-step display: the chosen psychologist, or (tanpa preferensi) the one the slot resolves to. */
  protected assignedPsychologistName(slot: Slot): string {
    const chosen = this.psychologist();
    if (chosen) return chosen.displayName;
    const id = slot.psychologistIds[0];
    return this.psychologists().find((p) => p.psychologistId === id)?.displayName ?? '';
  }

  protected loadMore(): void {
    this.rangeDays = Math.min(this.rangeDays + 14, 31);
    this.loadSlots();
  }

  protected submit(): void {
    const service = this.service();
    const slot = this.slot();
    if (!service || !slot || !this.mode()) return;
    const psychologistId = this.psychologist()?.psychologistId ?? slot.psychologistIds[0];

    this.busy.set(true);
    this.errorKey.set(null);
    this.http
      .post<PatientBooking>('/api/bookings', {
        psychologistId,
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
    if (!service || !this.psychologistChosen()) return;
    const params: Record<string, string> = { serviceId: service.id, to: this.toDateParam() };
    const psychologist = this.psychologist();
    if (psychologist) {
      params['psychologistId'] = psychologist.psychologistId;
    }

    this.http
      .get<DaySlots[]>('/api/booking/slots', { params })
      .subscribe((days) => this.days.set(days));
  }

  /** WIB end date for the slot range ('YYYY-MM-DD'). */
  private toDateParam(): string {
    const end = new Date(Date.now() + (this.rangeDays - 1) * 24 * 60 * 60 * 1000);
    return new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Jakarta' }).format(end);
  }
}
