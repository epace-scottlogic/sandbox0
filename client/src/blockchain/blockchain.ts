import { Component } from '@angular/core';
import { TradeUpdateList } from './trade-update-list/trade-update-list';
import { TradeUpdate } from './models/trade-update';

@Component({
  selector: 'app-blockchain',
  imports: [TradeUpdateList],
  templateUrl: './blockchain.html',
  styleUrl: './blockchain.css',
})
export class Blockchain {
  trades: TradeUpdate[] = [];
}
