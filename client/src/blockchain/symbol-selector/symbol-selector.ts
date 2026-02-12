import { Component, input, output } from '@angular/core';
import { Symbol } from '../models/trade-update';

@Component({
  selector: 'app-symbol-selector',
  templateUrl: './symbol-selector.html',
  styleUrl: './symbol-selector.css',
})
export class SymbolSelector {
  activeSymbols = input<string[]>([]);
  symbolSelected = output<string>();

  symbols: Symbol[] = ['BTC-USD', 'ETH-USD'];

  isActive(symbol: string): boolean {
    return this.activeSymbols().includes(symbol);
  }

  onSelect(symbol: string): void {
    if (!this.isActive(symbol)) {
      this.symbolSelected.emit(symbol);
    }
  }
}
