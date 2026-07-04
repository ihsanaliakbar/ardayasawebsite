import { Pipe, PipeTransform } from '@angular/core';

/** Formats a number as Indonesian Rupiah, e.g. 330000 → "Rp330.000". */
@Pipe({ name: 'idr' })
export class IdrPipe implements PipeTransform {
  private readonly formatter = new Intl.NumberFormat('id-ID', {
    style: 'currency',
    currency: 'IDR',
    maximumFractionDigits: 0,
  });

  transform(value: number | null | undefined): string {
    return value == null ? '' : this.formatter.format(value);
  }
}
