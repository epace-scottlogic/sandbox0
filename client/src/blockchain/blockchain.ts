import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { Subscription } from 'rxjs';
import { BlockchainRpcService } from './blockchain-rpc.service';
import { SymbolSelector } from './symbol-selector/symbol-selector';
import { SubscriptionContainer } from './subscription-container/subscription-container';
import { ConnectionStatus } from './connection-status/connection-status';
import { TradeUpdate, Symbol } from './models/trade-update';
import { ConnectionState } from '../rpc';

type SubscriptionState = 'active' | 'paused';

interface SymbolSubscription {
  symbol: string;
  trades: TradeUpdate[];
  loading: boolean;
  state: SubscriptionState;
}

@Component({
  selector: 'app-blockchain',
  imports: [SymbolSelector, SubscriptionContainer, ConnectionStatus],
  templateUrl: './blockchain.html',
  styleUrl: './blockchain.css',
})

export class Blockchain implements OnInit, OnDestroy {
  subscriptions: SymbolSubscription[] = [];
  connectionError = '';
  connectionState: ConnectionState = 'disconnected';
  backendConnected = true;

  private tradeSubscription: Subscription | null = null;
  private stateSubscription: Subscription | null = null;
  private backendConnectionSubscription: Subscription | null = null;

  constructor(
    private readonly rpcService: BlockchainRpcService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  get activeSymbols(): string[] {
    return this.subscriptions.filter((s) => s.state === 'active').map((s) => s.symbol);
  }

  ngOnInit(): void {
    this.stateSubscription = this.rpcService.connectionState$.subscribe((state) => {
      this.connectionState = state;
      if (state === 'connected') {
        this.connectionError = '';
      }
      this.cdr.detectChanges();
    });

    this.rpcService
      .connect()
      .then(() => {
        this.listenForTrades();
        this.listenForBackendConnection();
      })
      .catch((err: unknown) => {
        this.connectionError =
          err instanceof Error ? err.message : 'Failed to connect to server';
        this.cdr.detectChanges();
      });
  }

  ngOnDestroy(): void {
    this.tradeSubscription?.unsubscribe();
    this.stateSubscription?.unsubscribe();
    this.backendConnectionSubscription?.unsubscribe();

    for (const sub of this.subscriptions.filter((s) => s.state === 'active')) {
      this.rpcService.unsubscribe(sub.symbol as Symbol).subscribe();
    }

    this.rpcService.disconnect().catch(() => {});
  }

  onSymbolSelected(symbol: string): void {
    const existing = this.subscriptions.find((s) => s.symbol === symbol);
    if (existing) {
      if (existing.state === 'paused') {
        this.onResubscribe(symbol);
      }
      return;
    }

    const entry: SymbolSubscription = { symbol, trades: [], loading: true, state: 'active' };
    this.subscriptions = [...this.subscriptions, entry];

    this.rpcService.subscribe(symbol as Symbol).subscribe({
      next: () => {},
      error: (err: unknown) => {
        this.subscriptions = this.subscriptions.filter((s) => s.symbol !== symbol);
        this.connectionError =
          err instanceof Error ? err.message : `Failed to subscribe to ${symbol}`;
        this.cdr.detectChanges();
      },
    });
  }

  onUnsubscribe(symbol: string): void {
    this.updateSubscription(symbol, { state: 'paused' });
    this.rpcService.unsubscribe(symbol as Symbol).subscribe();
  }

  onResubscribe(symbol: string): void {
    this.updateSubscription(symbol, { loading: true, state: 'active' });
    this.rpcService.subscribe(symbol as Symbol).subscribe({
      next: () => {
        this.updateSubscription(symbol, { loading: false });
        this.cdr.detectChanges();
      },
      error: (err: unknown) => {
        this.updateSubscription(symbol, { state: 'paused', loading: false });
        this.connectionError =
          err instanceof Error ? err.message : `Failed to resubscribe to ${symbol}`;
        this.cdr.detectChanges();
      },
    });
  }

  onDismiss(symbol: string): void {
    this.subscriptions = this.subscriptions.filter((s) => s.symbol !== symbol);
  }

  dismissError(): void {
    this.connectionError = '';
  }

  private listenForTrades(): void {
    this.tradeSubscription = this.rpcService.onTradeUpdate().subscribe((trade) => {
      this.subscriptions = this.subscriptions.map((s) =>
        s.symbol === trade.symbol && s.state === 'active'
          ? { ...s, trades: [trade, ...s.trades], loading: false }
          : s,
      );
      this.cdr.detectChanges();
    });
  }

  private listenForBackendConnection(): void {
    this.backendConnectionSubscription = this.rpcService
      .onBackendConnectionEvent()
      .subscribe((event) => {
        this.backendConnected = event === 'restored';
        this.cdr.detectChanges();
      });
  }

  private updateSubscription(
    symbol: string,
    updates: Partial<Omit<SymbolSubscription, 'symbol'>>,
  ): void {
    this.subscriptions = this.subscriptions.map((s) =>
      s.symbol === symbol ? { ...s, ...updates } : s,
    );
  }
}
