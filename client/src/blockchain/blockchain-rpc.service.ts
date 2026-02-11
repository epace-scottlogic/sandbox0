import { Injectable, OnDestroy } from '@angular/core';
import { Observable } from 'rxjs';
import { RpcClient, ConnectionState } from '../rpc';
import { wrapServiceError } from '../common/service-error';
import { BlockchainMethods, SubscribeResult } from './models/blockchain-methods';
import { Symbol, TradeUpdate } from './models/trade-update';

@Injectable({ providedIn: 'root' })
export class BlockchainRpcService implements OnDestroy {
  constructor(private readonly rpcClient: RpcClient<BlockchainMethods>) {}

  get connectionState$(): Observable<ConnectionState> {
    return this.rpcClient.connectionState$;
  }

  connect(): Promise<void> {
    return this.rpcClient.connect();
  }

  disconnect(): Promise<void> {
    return this.rpcClient.disconnect();
  }

  subscribe(symbol: Symbol): Observable<SubscribeResult> {
    return this.rpcClient
      .invoke('subscribe', { channel: 'trades', symbol })
      .pipe(wrapServiceError<SubscribeResult>('BlockchainRpcService.subscribe() failed'));
  }

  unsubscribe(symbol: Symbol): Observable<SubscribeResult> {
    return this.rpcClient
      .invoke('unsubscribe', { channel: 'trades', symbol })
      .pipe(wrapServiceError<SubscribeResult>('BlockchainRpcService.unsubscribe() failed'));
  }

  onTradeUpdate(): Observable<TradeUpdate> {
    return this.rpcClient
      .onNotification<TradeUpdate>('trades.update')
      .pipe(wrapServiceError<TradeUpdate>('BlockchainRpcService.onTradeUpdate() failed'));
  }

  ngOnDestroy(): void {
    this.rpcClient.dispose();
  }
}
