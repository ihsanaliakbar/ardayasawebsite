import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { errorKeyFromResponse } from '../../core/api-error';
import { IdrPipe } from '../../core/idr.pipe';
import {
  AvailabilityException,
  AvailabilityView,
  DayOfWeek,
  ExceptionKind,
  PsychologistServiceMapRow,
  WEEK_DAYS,
} from '../../core/scheduling/scheduling.models';
import { WibCalendarDatePipe, dayLabelKey } from '../../core/scheduling/wib';
import { PsychologistProfile } from '../../shared/psychologist-profile-form';

interface EditableRange {
  start: string; // 'HH:mm'
  end: string;
}

/** '09:00:00' → '09:00' for <input type="time">; back with seconds on save. */
const toInput = (t: string) => t.slice(0, 5);
const toApi = (t: string) => `${t}:00`;

/**
 * Admin editor for one psychologist's jadwal praktik (weekly day rows + dated
 * exceptions) and the services they can be booked for. This is the ONLY place
 * availability is edited — psychologists see it read-only (decision 2026-07-07).
 */
@Component({
  selector: 'app-psychologist-schedule-edit',
  imports: [
    RouterLink, FormsModule, MatButtonModule, MatCardModule, MatCheckboxModule,
    MatIconModule, TranslatePipe, IdrPipe, WibCalendarDatePipe,
  ],
  template: `
    <a routerLink="/admin" class="back"><mat-icon inline>arrow_back_ios</mat-icon> {{ 'admin.psychologists.title' | translate }}</a>

    @if (name(); as psychologistName) {
      <mat-card class="panel">
        <h2>{{ 'adminSchedule.title' | translate: { name: psychologistName } }}</h2>
        <p class="hint">{{ 'adminSchedule.hint' | translate }}</p>

        @if (errorKey(); as key) {
          <p class="error">{{ key | translate }}</p>
        }
        @if (saved()) {
          <p class="success">{{ 'admin.saved' | translate }}</p>
        }

        <div class="week">
          @for (day of weekDays; track day) {
            <div class="day-row">
              <span class="day-name">{{ dayKey(day) | translate }}</span>
              <div class="ranges">
                @for (range of rules[day]; track $index) {
                  <span class="range">
                    <input type="time" [(ngModel)]="range.start" />
                    –
                    <input type="time" [(ngModel)]="range.end" />
                    <button mat-icon-button (click)="removeRange(day, $index)" [attr.aria-label]="'adminSchedule.removeRange' | translate">
                      <mat-icon>close</mat-icon>
                    </button>
                  </span>
                }
                @if (rules[day].length === 0) {
                  <span class="closed">{{ 'adminSchedule.closed' | translate }}</span>
                }
                <button mat-stroked-button class="add" (click)="addRange(day)">
                  <mat-icon inline>add</mat-icon> {{ 'adminSchedule.addRange' | translate }}
                </button>
              </div>
            </div>
          }
        </div>

        <button mat-flat-button (click)="saveRules()" [disabled]="busy()">
          {{ 'adminSchedule.saveRules' | translate }}
        </button>
      </mat-card>

      <mat-card class="panel">
        <h2>{{ 'adminSchedule.exceptions.title' | translate }}</h2>
        <p class="hint">{{ 'adminSchedule.exceptions.hint' | translate }}</p>

        @for (x of exceptions(); track x.id) {
          <div class="exception-row">
            <mat-icon [class.block]="x.kind === 'Block'" [class.extra]="x.kind === 'Extra'">
              {{ x.kind === 'Block' ? 'event_busy' : 'more_time' }}
            </mat-icon>
            <div>
              <strong>{{ x.date | wibCalendarDate }}</strong>
              <br />
              <span class="hint">
                {{ (x.kind === 'Block' ? 'adminSchedule.exceptions.block' : 'adminSchedule.exceptions.extra') | translate }}
                @if (x.startTime && x.endTime) {
                  · {{ formatTime(x.startTime) }}–{{ formatTime(x.endTime) }}
                } @else {
                  · {{ 'adminSchedule.exceptions.allDay' | translate }}
                }
              </span>
            </div>
            <button mat-icon-button (click)="removeException(x)" [attr.aria-label]="'admin.delete' | translate">
              <mat-icon>delete</mat-icon>
            </button>
          </div>
        } @empty {
          <p class="hint">{{ 'adminSchedule.exceptions.empty' | translate }}</p>
        }

        <div class="exception-form">
          <input type="date" [(ngModel)]="newException.date" />
          <select [(ngModel)]="newException.kind">
            <option value="Block">{{ 'adminSchedule.exceptions.block' | translate }}</option>
            <option value="Extra">{{ 'adminSchedule.exceptions.extra' | translate }}</option>
          </select>
          <input type="time" [(ngModel)]="newException.start" [attr.placeholder]="'adminSchedule.exceptions.start' | translate" />
          <input type="time" [(ngModel)]="newException.end" />
          <button mat-stroked-button (click)="addException()" [disabled]="busy() || !newException.date">
            {{ 'adminSchedule.exceptions.add' | translate }}
          </button>
        </div>
        <p class="hint">{{ 'adminSchedule.exceptions.timesHint' | translate }}</p>
      </mat-card>

      <mat-card class="panel">
        <h2>{{ 'adminSchedule.services.title' | translate }}</h2>
        <p class="hint">{{ 'adminSchedule.services.hint' | translate }}</p>

        @for (category of serviceCategories(); track category) {
          <h3>{{ category }}</h3>
          @for (row of servicesIn(category); track row.serviceId) {
            <div class="service-row">
              <mat-checkbox [(ngModel)]="row.enabled">{{ row.name }}</mat-checkbox>
              <span class="hint">
                {{ row.durationMinutes }} {{ 'services.minutes' | translate }}
                @if (row.offlinePrice != null) { · Offline {{ row.offlinePrice | idr }} }
                @if (row.onlinePrice != null) { · Online {{ row.onlinePrice | idr }} }
              </span>
            </div>
          }
        }

        <button mat-flat-button (click)="saveServices()" [disabled]="busy()">
          {{ 'adminSchedule.services.save' | translate }}
        </button>
      </mat-card>
    }
  `,
  styles: `
    .back { color: var(--mat-sys-on-surface-variant); text-decoration: none; font-size: 0.9rem; }
    .panel { padding: 24px; margin-top: 16px; max-width: 840px; }
    h2 { margin-top: 0; }
    h3 { font: var(--mat-sys-title-small); color: var(--mat-sys-on-surface-variant); margin: 16px 0 4px; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .error { color: var(--mat-sys-error); }
    .success { color: var(--accent-gold); }
    .week { margin: 16px 0; }
    .day-row {
      display: grid; grid-template-columns: 96px 1fr; gap: 12px; align-items: start;
      padding: 10px 0; border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .day-name { font-weight: 600; padding-top: 8px; }
    .ranges { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; }
    .range { display: inline-flex; align-items: center; gap: 4px; }
    .closed { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; font-style: italic; }
    input[type='time'], input[type='date'], select {
      background: var(--mat-sys-surface-container-high); color: inherit;
      border: 1px solid var(--mat-sys-outline-variant); border-radius: 8px; padding: 8px 10px;
      font: inherit; color-scheme: dark;
    }
    .add { min-width: 0; }
    .exception-row {
      display: flex; align-items: center; gap: 12px; padding: 10px 0;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .exception-row button { margin-left: auto; }
    .exception-row mat-icon.block { color: var(--mat-sys-error); }
    .exception-row mat-icon.extra { color: var(--accent-gold); }
    .exception-form { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 16px; align-items: center; }
    .service-row { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; padding: 4px 0; }
    button[mat-flat-button] { margin-top: 16px; }
    @media (max-width: 600px) { .day-row { grid-template-columns: 1fr; } .day-name { padding-top: 0; } }
  `,
})
export class PsychologistScheduleEdit implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);

  protected readonly weekDays = WEEK_DAYS;
  protected readonly name = signal<string | null>(null);
  protected readonly exceptions = signal<AvailabilityException[]>([]);
  protected readonly serviceRows = signal<PsychologistServiceMapRow[]>([]);
  protected readonly busy = signal(false);
  protected readonly saved = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  /** Editable copy of the weekly rules, keyed by day. */
  protected rules: Record<DayOfWeek, EditableRange[]> = Object.fromEntries(
    WEEK_DAYS.map((d) => [d, [] as EditableRange[]]),
  ) as Record<DayOfWeek, EditableRange[]>;

  protected newException: { date: string; kind: ExceptionKind; start: string; end: string } = {
    date: '', kind: 'Block', start: '', end: '',
  };

  private psychologistId = '';

  ngOnInit(): void {
    this.psychologistId = this.route.snapshot.paramMap.get('id')!;
    this.http
      .get<PsychologistProfile>(`/api/admin/psychologists/${this.psychologistId}/profile`)
      .subscribe((p) => this.name.set(p.displayName));
    this.reloadAvailability();
    this.http
      .get<PsychologistServiceMapRow[]>(`/api/admin/psychologists/${this.psychologistId}/services`)
      .subscribe((rows) => this.serviceRows.set(rows));
  }

  protected dayKey = dayLabelKey;

  protected formatTime(t: string): string {
    return t.slice(0, 5).replace(':', '.');
  }

  protected addRange(day: DayOfWeek): void {
    this.rules[day].push({ start: '09:00', end: '17:00' });
  }

  protected removeRange(day: DayOfWeek, index: number): void {
    this.rules[day].splice(index, 1);
  }

  protected saveRules(): void {
    const payload = {
      rules: this.weekDays.flatMap((day) =>
        this.rules[day]
          .filter((r) => r.start && r.end)
          .map((r) => ({ dayOfWeek: day, startTime: toApi(r.start), endTime: toApi(r.end) })),
      ),
    };
    this.mutate(
      this.http.put<AvailabilityView>(`/api/admin/psychologists/${this.psychologistId}/availability`, payload),
      (view) => this.applyAvailability(view),
    );
  }

  protected addException(): void {
    const { date, kind, start, end } = this.newException;
    const payload = {
      date,
      kind,
      startTime: start ? toApi(start) : null,
      endTime: end ? toApi(end) : null,
    };
    this.mutate(
      this.http.post<AvailabilityException>(
        `/api/admin/psychologists/${this.psychologistId}/availability/exceptions`, payload),
      () => {
        this.newException = { date: '', kind: 'Block', start: '', end: '' };
        this.reloadAvailability();
      },
    );
  }

  protected removeException(x: AvailabilityException): void {
    this.mutate(
      this.http.delete(`/api/admin/psychologists/${this.psychologistId}/availability/exceptions/${x.id}`),
      () => this.reloadAvailability(),
    );
  }

  protected saveServices(): void {
    const serviceIds = this.serviceRows().filter((r) => r.enabled).map((r) => r.serviceId);
    this.mutate(
      this.http.put(`/api/admin/psychologists/${this.psychologistId}/services`, { serviceIds }),
      () => undefined,
    );
  }

  protected serviceCategories(): string[] {
    return [...new Set(this.serviceRows().map((r) => r.categoryName))];
  }

  protected servicesIn(category: string): PsychologistServiceMapRow[] {
    return this.serviceRows().filter((r) => r.categoryName === category);
  }

  private mutate<T>(request: Observable<T>, onSuccess: (value: T) => void): void {
    this.busy.set(true);
    this.saved.set(false);
    this.errorKey.set(null);
    request.subscribe({
      next: (value) => {
        this.busy.set(false);
        this.saved.set(true);
        onSuccess(value);
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.errorKey.set(errorKeyFromResponse(err));
      },
    });
  }

  private reloadAvailability(): void {
    this.http
      .get<AvailabilityView>(`/api/admin/psychologists/${this.psychologistId}/availability`)
      .subscribe((view) => this.applyAvailability(view));
  }

  private applyAvailability(view: AvailabilityView): void {
    for (const day of this.weekDays) {
      this.rules[day] = view.rules
        .filter((r) => r.dayOfWeek === day)
        .map((r) => ({ start: toInput(r.startTime), end: toInput(r.endTime) }));
    }
    this.exceptions.set(view.exceptions);
  }
}
