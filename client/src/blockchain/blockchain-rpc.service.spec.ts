import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { RpcClient } from '../rpc';
import { MockRpcConnection } from '../rpc/testing/mock-rpc-connection';
import { RPC_CONNECTION } from '../rpc/rpc-client.service';
import { ServiceError } from '../common/service-error';
import { BlockchainMethods } from './models/blockchain-methods';
import { BlockchainRpcService, BackendConnectionEvent } from './blockchain-rpc.service';
import { TradeUpdate } from './models/trade-update';

function createService() {
  const connection = new MockRpcConnection();
  const client = new RpcClient<BlockchainMethods>(connection, { timeout: 1000 });
  const service = new BlockchainRpcService(client);
  return { connection, client, service };
}

function tick(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

describe('BlockchainRpcService', () => {
  describe('unit tests', () => {
    let connection: MockRpcConnection;
    let client: RpcClient<BlockchainMethods>;
    let service: BlockchainRpcService;

    beforeEach(() => {
      ({ connection, client, service } = createService());
    });

    afterEach(() => {
      service.ngOnDestroy();
      connection.dispose();
    });

    describe('connectionState$', () => {
      it('should emit the initial disconnected state', async () => {
        const state = await firstValueFrom(service.connectionState$);
        expect(state).toBe('disconnected');
      });

      it('should reflect connected state after connect', async () => {
        await service.connect();
        const state = await firstValueFrom(service.connectionState$);
        expect(state).toBe('connected');
      });

      it('should reflect reconnecting state', async () => {
        const states: string[] = [];
        const sub = service.connectionState$.subscribe((s) => states.push(s));

        connection.simulateState('reconnecting');

        sub.unsubscribe();
        expect(states).toContain('reconnecting');
      });
    });

    describe('connect', () => {
      it('should delegate to the RPC client', async () => {
        await service.connect();
        expect(connection.connectCalls).toBe(1);
      });

      it('should propagate connection errors', async () => {
        connection.shouldFailConnect = true;
        await expect(service.connect()).rejects.toThrow('Connection failed');
      });
    });

    describe('disconnect', () => {
      it('should delegate to the RPC client', async () => {
        await service.connect();
        await service.disconnect();
        expect(connection.disconnectCalls).toBe(1);
      });
    });

    describe('subscribe', () => {
      it('should send a subscribe RPC request with correct params', async () => {
        await service.connect();

        const sub = service.subscribe('BTC-USD').subscribe({ error: () => {} });
        await tick();

        expect(connection.sentMessages).toHaveLength(1);
        const sent = JSON.parse(connection.sentMessages[0]);
        expect(sent.method).toBe('subscribe');
        expect(sent.params).toEqual({ channel: 'trades', symbol: 'BTC-USD' });

        sub.unsubscribe();
      });

      it('should resolve with the server response on success', async () => {
        await service.connect();

        const resultPromise = firstValueFrom(service.subscribe('ETH-USD'));
        await tick();

        const sent = JSON.parse(connection.sentMessages[0]);
        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', result: { event: 'subscribed' }, id: sent.id }),
        );

        const result = await resultPromise;
        expect(result).toEqual({ event: 'subscribed' });
      });

      it('should wrap RPC errors in a ServiceError with cause preserved', async () => {
        await service.connect();

        const resultPromise = firstValueFrom(service.subscribe('BTC-USD'));
        await tick();

        const sent = JSON.parse(connection.sentMessages[0]);
        connection.simulateMessage(
          JSON.stringify({
            jsonrpc: '2.0',
            error: { code: -32602, message: 'Invalid params' },
            id: sent.id,
          }),
        );

        try {
          await resultPromise;
          expect.unreachable('should have thrown');
        } catch (err) {
          expect(err).toBeInstanceOf(ServiceError);
          expect((err as ServiceError).message).toBe('BlockchainRpcService.subscribe() failed');
          expect((err as ServiceError).originalCause).toBeDefined();
        }
      });
    });

    describe('unsubscribe', () => {
      it('should send an unsubscribe RPC request with correct params', async () => {
        await service.connect();

        const sub = service.unsubscribe('BTC-USD').subscribe({ error: () => {} });
        await tick();

        expect(connection.sentMessages).toHaveLength(1);
        const sent = JSON.parse(connection.sentMessages[0]);
        expect(sent.method).toBe('unsubscribe');
        expect(sent.params).toEqual({ channel: 'trades', symbol: 'BTC-USD' });

        sub.unsubscribe();
      });

      it('should resolve with the server response on success', async () => {
        await service.connect();

        const resultPromise = firstValueFrom(service.unsubscribe('ETH-USD'));
        await tick();

        const sent = JSON.parse(connection.sentMessages[0]);
        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', result: { event: 'unsubscribed' }, id: sent.id }),
        );

        const result = await resultPromise;
        expect(result).toEqual({ event: 'unsubscribed' });
      });

      it('should wrap RPC errors in a ServiceError with cause preserved', async () => {
        await service.connect();

        const resultPromise = firstValueFrom(service.unsubscribe('BTC-USD'));
        await tick();

        const sent = JSON.parse(connection.sentMessages[0]);
        connection.simulateMessage(
          JSON.stringify({
            jsonrpc: '2.0',
            error: { code: -32601, message: 'Method not found' },
            id: sent.id,
          }),
        );

        try {
          await resultPromise;
          expect.unreachable('should have thrown');
        } catch (err) {
          expect(err).toBeInstanceOf(ServiceError);
          expect((err as ServiceError).message).toBe('BlockchainRpcService.unsubscribe() failed');
          expect((err as ServiceError).originalCause).toBeDefined();
        }
      });
    });

    describe('onTradeUpdate', () => {
      it('should emit trade updates from trades.update notifications', () => {
        const received: TradeUpdate[] = [];
        service.onTradeUpdate().subscribe((trade) => received.push(trade));

        const trade: TradeUpdate = {
          seqnum: 1,
          event: 'updated',
          channel: 'trades',
          symbol: 'BTC-USD',
          timestamp: '2026-01-01T00:00:00Z',
          side: 'buy',
          qty: 0.5,
          price: 50000,
          tradeId: 'trade-1',
        };

        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', method: 'trades.update', params: trade }),
        );

        expect(received).toHaveLength(1);
        expect(received[0].symbol).toBe('BTC-USD');
        expect(received[0].price).toBe(50000);
      });

      it('should not emit for other notification methods', () => {
        const received: TradeUpdate[] = [];
        service.onTradeUpdate().subscribe((trade) => received.push(trade));

        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', method: 'other.update', params: {} }),
        );

        expect(received).toHaveLength(0);
      });

      it('should support multiple subscribers', () => {
        const received1: TradeUpdate[] = [];
        const received2: TradeUpdate[] = [];
        service.onTradeUpdate().subscribe((t) => received1.push(t));
        service.onTradeUpdate().subscribe((t) => received2.push(t));

        const trade: TradeUpdate = {
          seqnum: 2,
          event: 'updated',
          channel: 'trades',
          symbol: 'ETH-USD',
          timestamp: '2026-01-01T00:00:01Z',
          side: 'sell',
          qty: 1.0,
          price: 3000,
          tradeId: 'trade-2',
        };

        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', method: 'trades.update', params: trade }),
        );

        expect(received1).toHaveLength(1);
        expect(received2).toHaveLength(1);
      });
    });

    describe('onBackendConnectionEvent', () => {
      it('should emit lost when connection.lost notification is received', () => {
        const events: BackendConnectionEvent[] = [];
        service.onBackendConnectionEvent().subscribe((e) => events.push(e));

        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', method: 'connection.lost', params: {} }),
        );

        expect(events).toHaveLength(1);
        expect(events[0]).toBe('lost');
      });

      it('should emit restored when connection.restored notification is received', () => {
        const events: BackendConnectionEvent[] = [];
        service.onBackendConnectionEvent().subscribe((e) => events.push(e));

        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', method: 'connection.restored', params: {} }),
        );

        expect(events).toHaveLength(1);
        expect(events[0]).toBe('restored');
      });

      it('should emit both lost and restored events in sequence', () => {
        const events: BackendConnectionEvent[] = [];
        service.onBackendConnectionEvent().subscribe((e) => events.push(e));

        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', method: 'connection.lost', params: {} }),
        );
        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', method: 'connection.restored', params: {} }),
        );

        expect(events).toEqual(['lost', 'restored']);
      });

      it('should not emit for unrelated notifications', () => {
        const events: BackendConnectionEvent[] = [];
        service.onBackendConnectionEvent().subscribe((e) => events.push(e));

        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', method: 'trades.update', params: {} }),
        );

        expect(events).toHaveLength(0);
      });

      it('should support multiple subscribers', () => {
        const events1: BackendConnectionEvent[] = [];
        const events2: BackendConnectionEvent[] = [];
        service.onBackendConnectionEvent().subscribe((e) => events1.push(e));
        service.onBackendConnectionEvent().subscribe((e) => events2.push(e));

        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', method: 'connection.lost', params: {} }),
        );

        expect(events1).toHaveLength(1);
        expect(events2).toHaveLength(1);
      });
    });

    describe('ngOnDestroy', () => {
      it('should dispose the RPC client', () => {
        const disposeSpy = vi.spyOn(client, 'dispose');
        service.ngOnDestroy();
        expect(disposeSpy).toHaveBeenCalled();
      });
    });
  });

  describe('Angular DI integration', () => {
    it('should be injectable with provideRpcClient providers', () => {
      const mockConnection = new MockRpcConnection();

      TestBed.configureTestingModule({
        providers: [
          { provide: RPC_CONNECTION, useValue: mockConnection },
          { provide: RpcClient, useFactory: () => new RpcClient<BlockchainMethods>(mockConnection, { timeout: 1000 }) },
          BlockchainRpcService,
        ],
      });

      const service = TestBed.inject(BlockchainRpcService);
      expect(service).toBeInstanceOf(BlockchainRpcService);

      service.ngOnDestroy();
      mockConnection.dispose();
    });
  });
});
