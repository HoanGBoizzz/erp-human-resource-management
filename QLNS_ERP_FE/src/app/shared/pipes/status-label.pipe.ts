import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'statusLabel'
})
export class StatusLabelPipe implements PipeTransform {

  transform(value: unknown): string {
    switch (value) {
      case 'Chờ duyệt':
      case 'CHO_DUYET':
        return 'bg-warning text-dark';
      case 'Đã duyệt':
      case 'DA_DUYET':
        return 'bg-success text-white';
      case 'Từ chối':
      case 'TU_CHOI':
        return 'bg-danger text-white';
      case 'Hủy':
      case 'HUY':
        return 'bg-secondary text-white';
      default:
        return 'bg-light text-dark';
    }
  }

}
