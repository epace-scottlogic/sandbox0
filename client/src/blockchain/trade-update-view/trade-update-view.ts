import { Component, input } from '@angular/core';
import { TradeUpdate } from '../models/trade-update';

@Component({
  selector: 'app-trade-update-view',
  templateUrl: './trade-update-view.html',
  styleUrl: './trade-update-view.css',
})
export class TradeUpdateView {
  trade = input.required<TradeUpdate>();
}
