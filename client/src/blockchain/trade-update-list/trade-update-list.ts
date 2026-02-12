import { Component, input } from '@angular/core';
import { TradeUpdate } from '../models/trade-update';
import { TradeUpdateView } from '../trade-update-view/trade-update-view';

@Component({
  selector: 'app-trade-update-list',
  imports: [TradeUpdateView],
  templateUrl: './trade-update-list.html',
  styleUrl: './trade-update-list.css',
})
export class TradeUpdateList {
  trades = input<TradeUpdate[]>([]);
}
