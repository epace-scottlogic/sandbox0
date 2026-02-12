import { firstValueFrom } from 'rxjs';
import { RpcClient, RpcClientError } from './rpc-client';
import { MockRpcConnection } from './testing/mock-rpc-connection';
import { JSON_RPC_VERSION, RpcMethodDefinition } from './models';
import { Logger, LogFunction } from '../common/logger';

interface TestMethods {
  subscribe: RpcMethodDefinition<{ channel: string; symbol: string }, { event: string }>;
  unsubscribe: RpcMethodDefinition<{ channel: string; symbol: string }, { event: string }>;
  ping: RpcMethodDefinition<void, string>;
}

describe('RpcClient', () => {
  let connection: MockRpcConnection;
  let client: RpcClient<TestMethods>;

  beforeEach(() => {
    connection = new MockRpcConnection();
    client = new RpcClient<TestMethods>(connection, { timeout: 1000 });
  });

  afterEach(() => {
    subscriptions.forEach((s) => s.unsubscribe());
    subscriptions = [];
    client.dispose();
    connection.dispose();
  });

  let subscriptions: { unsubscribe: () => void }[] = [];
  function track<T>(obs: import('rxjs').Observable<T>) {
    const sub = obs.subscribe({ error: () => {} });
    subscriptions.push(sub);
  }

  describe('connect', () => {
    it('should delegate to the connection', async () => {
      await client.connect();

      expect(connection.connectCalls).toBe(1);
    });

    it('should propagate connection errors', async () => {
      connection.shouldFailConnect = true;

      await expect(client.connect()).rejects.toThrow('Connection failed');
    });
  });

  describe('disconnect', () => {
    it('should delegate to the connection', async () => {
      await client.connect();
      await client.disconnect();

      expect(connection.disconnectCalls).toBe(1);
    });

    it('should error pending requests on disconnect', async () => {
      await client.connect();

      const resultPromise = firstValueFrom(
        client.invoke('subscribe', { channel: 'trades', symbol: 'BTC-USD' }),
      );

      await client.disconnect();

      await expect(resultPromise).rejects.toThrow('Connection closed');
    });
  });

  describe('connectionState$', () => {
    it('should reflect the underlying connection state', async () => {
      const states: string[] = [];
      const sub = client.connectionState$.subscribe((s) => states.push(s));

      await client.connect();

      sub.unsubscribe();
      expect(states).toContain('connected');
    });

    it('should emit reconnecting state', () => {
      const states: string[] = [];
      const sub = client.connectionState$.subscribe((s) => states.push(s));

      connection.simulateState('reconnecting');

      sub.unsubscribe();
      expect(states).toContain('reconnecting');
    });
  });

  describe('invoke', () => {
    it('should send a JSON-RPC request through the connection', async () => {
      await client.connect();
      track(client.invoke('subscribe', { channel: 'trades', symbol: 'BTC-USD' }));

      await tick();

      expect(connection.sentMessages).toHaveLength(1);
      const sent = JSON.parse(connection.sentMessages[0]);
      expect(sent.jsonrpc).toBe(JSON_RPC_VERSION);
      expect(sent.method).toBe('subscribe');
      expect(sent.params).toEqual({ channel: 'trades', symbol: 'BTC-USD' });
      expect(sent.id).toBeDefined();
    });

    it('should resolve with the result on success response', async () => {
      await client.connect();
      const resultPromise = firstValueFrom(
        client.invoke('subscribe', { channel: 'trades', symbol: 'BTC-USD' }),
      );

      await tick();

      const sent = JSON.parse(connection.sentMessages[0]);
      connection.simulateMessage(
        JSON.stringify({
          jsonrpc: '2.0',
          result: { event: 'subscribed' },
          id: sent.id,
        }),
      );

      const result = await resultPromise;
      expect(result).toEqual({ event: 'subscribed' });
    });

    it('should throw RpcClientError on error response', async () => {
      await client.connect();
      const resultPromise = firstValueFrom(
        client.invoke('subscribe', { channel: 'trades', symbol: 'BTC-USD' }),
      );

      await tick();

      const sent = JSON.parse(connection.sentMessages[0]);
      connection.simulateMessage(
        JSON.stringify({
          jsonrpc: '2.0',
          error: { code: -32602, message: 'Invalid params' },
          id: sent.id,
        }),
      );

      await expect(resultPromise).rejects.toThrow(RpcClientError);
      await expect(resultPromise).rejects.toThrow('Invalid params');
    });

    it('should expose the rpcError on RpcClientError', async () => {
      await client.connect();
      const resultPromise = firstValueFrom(
        client.invoke('subscribe', { channel: 'trades', symbol: 'BTC-USD' }),
      );

      await tick();

      const sent = JSON.parse(connection.sentMessages[0]);
      connection.simulateMessage(
        JSON.stringify({
          jsonrpc: '2.0',
          error: { code: -32602, message: 'Invalid params', data: 'details' },
          id: sent.id,
        }),
      );

      try {
        await resultPromise;
      } catch (err) {
        expect(err).toBeInstanceOf(RpcClientError);
        expect((err as RpcClientError).rpcError.code).toBe(-32602);
        expect((err as RpcClientError).rpcError.data).toBe('details');
      }
    });

    it('should timeout if no response is received', async () => {
      await client.connect();
      const resultPromise = firstValueFrom(
        client.invoke('subscribe', { channel: 'trades', symbol: 'BTC-USD' }),
      );

      await expect(resultPromise).rejects.toThrow(/timed out/);
    });

    it('should handle methods with no params', async () => {
      await client.connect();
      track(client.invoke('ping'));

      await tick();

      expect(connection.sentMessages).toHaveLength(1);
      const sent = JSON.parse(connection.sentMessages[0]);
      expect(sent.method).toBe('ping');
      expect(sent.params).toBeUndefined();
    });

    it('should assign unique ids to concurrent requests', async () => {
      await client.connect();
      track(client.invoke('subscribe', { channel: 'trades', symbol: 'BTC-USD' }));
      track(client.invoke('subscribe', { channel: 'trades', symbol: 'ETH-USD' }));

      await tick();

      const ids = connection.sentMessages.map((m) => JSON.parse(m).id);
      expect(new Set(ids).size).toBe(2);
    });

    it('should correlate responses to the correct request', async () => {
      await client.connect();

      const firstPromise = firstValueFrom(
        client.invoke('subscribe', { channel: 'trades', symbol: 'BTC-USD' }),
      );
      const secondPromise = firstValueFrom(
        client.invoke('subscribe', { channel: 'trades', symbol: 'ETH-USD' }),
      );

      await tick();

      const firstId = JSON.parse(connection.sentMessages[0]).id;
      const secondId = JSON.parse(connection.sentMessages[1]).id;

      connection.simulateMessage(
        JSON.stringify({ jsonrpc: '2.0', result: { event: 'second' }, id: secondId }),
      );
      connection.simulateMessage(
        JSON.stringify({ jsonrpc: '2.0', result: { event: 'first' }, id: firstId }),
      );

      expect(await firstPromise).toEqual({ event: 'first' });
      expect(await secondPromise).toEqual({ event: 'second' });
    });

    it('should error when send fails', async () => {
      await client.connect();
      connection.shouldFailSend = true;

      const resultPromise = firstValueFrom(
        client.invoke('subscribe', { channel: 'trades', symbol: 'BTC-USD' }),
      );

      await expect(resultPromise).rejects.toThrow('Send failed');
    });
  });

  describe('onNotification', () => {
    it('should emit notifications matching the method', () => {
      const received: { price: number }[] = [];
      client.onNotification<{ price: number }>('trades.update').subscribe((params) => {
        received.push(params);
      });

      connection.simulateMessage(
        JSON.stringify({ jsonrpc: '2.0', method: 'trades.update', params: { price: 50000 } }),
      );

      expect(received).toHaveLength(1);
      expect(received[0].price).toBe(50000);
    });

    it('should not emit notifications for different methods', () => {
      const received: unknown[] = [];
      client.onNotification('trades.update').subscribe((params) => {
        received.push(params);
      });

      connection.simulateMessage(
        JSON.stringify({ jsonrpc: '2.0', method: 'other.event', params: {} }),
      );

      expect(received).toHaveLength(0);
    });

    it('should handle multiple subscribers for the same method', () => {
      const received1: unknown[] = [];
      const received2: unknown[] = [];

      client.onNotification('trades.update').subscribe((p) => received1.push(p));
      client.onNotification('trades.update').subscribe((p) => received2.push(p));

      connection.simulateMessage(
        JSON.stringify({ jsonrpc: '2.0', method: 'trades.update', params: { price: 100 } }),
      );

      expect(received1).toHaveLength(1);
      expect(received2).toHaveLength(1);
    });

    it('should not treat responses as notifications', async () => {
      await client.connect();
      const received: unknown[] = [];
      client.onNotification('subscribe').subscribe((p) => received.push(p));

      connection.simulateMessage(
        JSON.stringify({ jsonrpc: '2.0', result: {}, id: '999' }),
      );

      expect(received).toHaveLength(0);
    });
  });

  describe('dispose', () => {
    it('should error pending requests', async () => {
      await client.connect();
      const resultPromise = firstValueFrom(
        client.invoke('subscribe', { channel: 'trades', symbol: 'BTC-USD' }),
      );

      client.dispose();

      await expect(resultPromise).rejects.toThrow('Client disposed');
    });

    it('should complete the notification stream', () => {
      let completed = false;
      client.onNotification('test').subscribe({ complete: () => (completed = true) });

      client.dispose();

      expect(completed).toBe(true);
    });
  });

  describe('debug logging', () => {
    let logFn: LogFunction;
    let debugClient: RpcClient<TestMethods>;
    let debugConnection: MockRpcConnection;

    beforeEach(() => {
      logFn = vi.fn() as LogFunction;
      debugConnection = new MockRpcConnection();
      debugClient = new RpcClient<TestMethods>(debugConnection, { timeout: 1000, logger: new Logger({ enabled: true, logFn }) });
    });

    afterEach(() => {
      debugClient.dispose();
      debugConnection.dispose();
    });

    it('should log on connect', async () => {
      await debugClient.connect();

      expect(logFn).toHaveBeenCalledWith('[RpcClient] connect()');
    });

    it('should log on connect failure', async () => {
      debugConnection.shouldFailConnect = true;

      await expect(debugClient.connect()).rejects.toThrow();
      expect(logFn).toHaveBeenCalledWith('[RpcClient] connect failed', expect.any(Error));
    });

    it('should log on disconnect', async () => {
      await debugClient.connect();
      await debugClient.disconnect();

      expect(logFn).toHaveBeenCalledWith('[RpcClient] disconnect()');
    });

    it('should log on invoke', async () => {
      await debugClient.connect();
      track(debugClient.invoke('ping'));
      await tick();

      expect(logFn).toHaveBeenCalledWith('[RpcClient] invoke()', expect.objectContaining({ method: 'ping' }));
    });

    it('should log on response', async () => {
      await debugClient.connect();
      const resultPromise = firstValueFrom(debugClient.invoke('ping'));
      await tick();

      const sent = JSON.parse(debugConnection.sentMessages[0]);
      debugConnection.simulateMessage(
        JSON.stringify({ jsonrpc: '2.0', result: 'pong', id: sent.id }),
      );

      await resultPromise;
      expect(logFn).toHaveBeenCalledWith('[RpcClient] response', expect.objectContaining({ method: 'ping' }));
    });

    it('should log on dispose', () => {
      debugClient.dispose();

      expect(logFn).toHaveBeenCalledWith('[RpcClient] dispose()');
    });

    it('should log raw messages', () => {
      debugConnection.simulateMessage('{"jsonrpc":"2.0","method":"test","params":{}}');

      expect(logFn).toHaveBeenCalledWith('[RpcClient] raw message', expect.any(String));
    });

    it('should not log when logger is disabled', async () => {
      const silentLogFn = vi.fn() as LogFunction;
      const silentConn = new MockRpcConnection();
      const silentClient = new RpcClient<TestMethods>(silentConn, { timeout: 1000, logger: new Logger({ enabled: false, logFn: silentLogFn }) });

      await silentClient.connect();
      track(silentClient.invoke('ping'));
      await tick();
      silentClient.dispose();
      silentConn.dispose();

      expect(silentLogFn).not.toHaveBeenCalled();
    });
  });

  describe('malformed messages', () => {
    it('should ignore non-JSON messages', () => {
      expect(() => {
        connection.simulateMessage('not json');
      }).not.toThrow();
    });

    it('should ignore messages without id or method', () => {
      expect(() => {
        connection.simulateMessage(JSON.stringify({ jsonrpc: '2.0' }));
      }).not.toThrow();
    });

    it('should ignore responses with unknown ids', () => {
      expect(() => {
        connection.simulateMessage(
          JSON.stringify({ jsonrpc: '2.0', result: {}, id: 'unknown' }),
        );
      }).not.toThrow();
    });
  });
});

function tick(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0));
}
