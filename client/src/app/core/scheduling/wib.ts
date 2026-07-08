import { Pipe, PipeTransform } from '@angular/core';
import { DayOfWeek } from './scheduling.models';

const WIB_TZ = 'Asia/Jakarta';

/** '09:00:00' (API TimeOnly) → '09.00' (Indonesian time convention). */
export function formatWibTimeOnly(time: string): string {
  return time.slice(0, 5).replace(':', '.');
}

/** Translation key for a day-of-week label; values live in id.json under enums.day. */
export function dayLabelKey(day: DayOfWeek): string {
  return `enums.day.${day}`;
}

/** UTC instant → WIB wall-clock time 'HH.mm' — via Intl time zones, never a hardcoded +7. */
@Pipe({ name: 'wibTime' })
export class WibTimePipe implements PipeTransform {
  private readonly formatter = new Intl.DateTimeFormat('id-ID', {
    timeZone: WIB_TZ,
    hour: '2-digit',
    minute: '2-digit',
  });

  transform(utc: string | Date | null | undefined): string {
    return utc == null ? '' : this.formatter.format(new Date(utc));
  }
}

/** UTC instant → WIB date like 'Sen, 13 Jul 2026'. */
@Pipe({ name: 'wibDate' })
export class WibDatePipe implements PipeTransform {
  private readonly formatter = new Intl.DateTimeFormat('id-ID', {
    timeZone: WIB_TZ,
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });

  transform(utc: string | Date | null | undefined): string {
    return utc == null ? '' : this.formatter.format(new Date(utc));
  }
}

/** WIB calendar date 'YYYY-MM-DD' → 'Senin, 13 Juli 2026' (no timezone shift: it is already a WIB date). */
@Pipe({ name: 'wibCalendarDate' })
export class WibCalendarDatePipe implements PipeTransform {
  private readonly formatter = new Intl.DateTimeFormat('id-ID', {
    timeZone: 'UTC',
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });

  transform(date: string | null | undefined): string {
    return date ? this.formatter.format(new Date(`${date}T00:00:00Z`)) : '';
  }
}
