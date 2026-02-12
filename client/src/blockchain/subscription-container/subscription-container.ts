import { Component, input, output } from '@angular/core';
import { TradeUpdate } from '../models/trade-update';
import { TradeUpdateList } from '../trade-update-list/trade-update-list';

@Component({
  selector: 'app-subscription-container',
  imports: [TradeUpdateList],
  templateUrl: './subscription-container.html',
  styleUrl: './subscription-container.css',
})
export class SubscriptionContainer {
  symbol = input.required<string>();
  trades = input<TradeUpdate[]>([]);
  loading = input<boolean>(false);
  state = input<'active' | 'paused'>('active');
  unsubscribed = output<string>();
  resubscribed = output<string>();
  dismissed = output<string>();

  onUnsubscribe(): void {
    this.unsubscribed.emit(this.symbol());
  }

  onResubscribe(): void {
    this.resubscribed.emit(this.symbol());
  }

  onDismiss(): void {
    this.dismissed.emit(this.symbol());
  }
}
