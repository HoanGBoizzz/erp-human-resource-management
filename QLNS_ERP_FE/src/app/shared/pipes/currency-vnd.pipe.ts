import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'currencyVnd'
})
export class CurrencyVndPipe implements PipeTransform {

  transform(value: unknown, ...args: unknown[]): unknown {
    return null;
  }

}
