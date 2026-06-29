import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'dateVn'
})
export class DateVnPipe implements PipeTransform {

  transform(value: string | Date | null | undefined, format: string = 'dd/MM/yyyy'): string {
    if (!value) return '—';

    const date = typeof value === 'string' ? new Date(value) : value;
    if (isNaN(date.getTime())) return '—';

    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');

    if (format === 'dd/MM/yy') {
      return `${day}/${month}/${String(year).slice(-2)}`;
    }

    if (format === 'dd/MM/yyyy HH:mm') {
      return `${day}/${month}/${year} ${hours}:${minutes}`;
    }

    return `${day}/${month}/${year}`;
  }

}
