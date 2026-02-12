import { HubConnection } from '@microsoft/signalr';
import { SignalRRpcConnection } from './signalr-rpc-connection';
import { Logger, LogFunction } from '../common/logger';

type HubCallback = (...args: string[]) => void;
type LifecycleCallback = (error?: Error) => void;
type ReconnectedCallback = (connectionId?: string) => void;

function createMockHubConnection() {
  const handlers = new Map<string, HubCallback>();
  let onCloseCallback: LifecycleCallback | null = null;
  let onReconnectingCallback: LifecycleCallback | null = null;
  let onReconnectedCallback: ReconnectedCallback | null = null;

  const mock = {
    on: vi.fn((methodName: string, callback: HubCallback) => {
      handlers.set(methodName, callback);
    }),
    off: vi.fn(),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined),
    onclose: vi.fn((callback: LifecycleCallback) => {
      onCloseCallback = callback;
    }),
    onreconnecting: vi.fn((callback: LifecycleCallback) => {
      onReconnectingCallback = callback;
    }),
    onreconnected: vi.fn((callback: ReconnectedCallback) => {
      onReconnectedCallback = callback;
    }),

    _simulateMessage(methodName: string, message: string) {
      handlers.get(methodName)?.(message);
    },
    _simulateClose() {
      onCloseCallback?.();
    },
    _simulateReconnecting() {
      onReconnectingCallback?.();
    },
    _simulateReconnected() {
      onReconnectedCallback?.();
    },
  };

  return mock;
}

describe('SignalRRpcConnection', () => {
  let mockHub: ReturnType<typeof createMockHubConnection>;
  let connection: SignalRRpcConnection;

  beforeEach(() => {
    mockHub = createMockHubConnection();
    connection = new SignalRRpcConnection(mockHub as unknown as HubConnection);
  });

  describe('constructor', () => {
    it('should register a handler for ReceiveMessage by default', () => {
      expect(mockHub.on).toHaveBeenCalledWith('ReceiveMessage', expect.any(Function));
    });

    it('should register lifecycle callbacks', () => {
      expect(mockHub.onclose).toHaveBeenCalled();
      expect(mockHub.onreconnecting).toHaveBeenCalled();
      expect(mockHub.onreconnected).toHaveBeenCalled();
    });

    it('should use custom method names when provided', () => {
      const customHub = createMockHubConnection();
      new SignalRRpcConnection(customHub as unknown as HubConnection, {
        receiveMethodName: 'CustomReceive',
        sendMethodName: 'CustomSend',
      });

      expect(customHub.on).toHaveBeenCalledWith('CustomReceive', expect.any(Function));
    });
  });

  describe('connect', () => {
    it('should call start on the hub connection', async () => {
      await connection.connect();

      expect(mockHub.start).toHaveBeenCalled();
    });

    it('should transition state from disconnected to connecting then connected', async () => {
      const states: string[] = [];
      connection.state$.subscribe((s) => states.push(s));

      await connection.connect();

      expect(states).toEqual(['disconnected', 'connecting', 'connected']);
    });

    it('should propagate errors from hub start', async () => {
      mockHub.start.mockRejectedValue(new Error('Hub start failed'));

      await expect(connection.connect()).rejects.toThrow('Hub start failed');
    });
  });

  describe('disconnect', () => {
    it('should call stop on the hub connection', async () => {
      await connection.connect();
      await connection.disconnect();

      expect(mockHub.stop).toHaveBeenCalled();
    });

    it('should set state to disconnected', async () => {
      await connection.connect();

      const states: string[] = [];
      connection.state$.subscribe((s) => states.push(s));

      await connection.disconnect();

      expect(states).toContain('disconnected');
    });
  });

  describe('send', () => {
    it('should invoke SendMessage on the hub by default', async () => {
      await connection.connect();
      await connection.send('{"test": true}');

      expect(mockHub.invoke).toHaveBeenCalledWith('SendMessage', '{"test": true}');
    });

    it('should use custom send method name', async () => {
      const customHub = createMockHubConnection();
      const customConn = new SignalRRpcConnection(customHub as unknown as HubConnection, {
        sendMethodName: 'CustomSend',
      });

      await customConn.connect();
      await customConn.send('data');

      expect(customHub.invoke).toHaveBeenCalledWith('CustomSend', 'data');
    });
  });

  describe('messages$', () => {
    it('should emit messages received from the hub', () => {
      const received: string[] = [];
      connection.messages$.subscribe((m) => received.push(m));

      mockHub._simulateMessage('ReceiveMessage', '{"jsonrpc":"2.0"}');

      expect(received).toEqual(['{"jsonrpc":"2.0"}']);
    });

    it('should emit multiple messages', () => {
      const received: string[] = [];
      connection.messages$.subscribe((m) => received.push(m));

      mockHub._simulateMessage('ReceiveMessage', 'msg1');
      mockHub._simulateMessage('ReceiveMessage', 'msg2');

      expect(received).toHaveLength(2);
    });
  });

  describe('connection lifecycle events', () => {
    it('should set state to disconnected on close', async () => {
      await connection.connect();

      const states: string[] = [];
      connection.state$.subscribe((s) => states.push(s));

      mockHub._simulateClose();

      expect(states).toContain('disconnected');
    });

    it('should set state to reconnecting', async () => {
      await connection.connect();

      const states: string[] = [];
      connection.state$.subscribe((s) => states.push(s));

      mockHub._simulateReconnecting();

      expect(states).toContain('reconnecting');
    });

    it('should set state to connected on reconnected', async () => {
      await connection.connect();

      mockHub._simulateReconnecting();

      const states: string[] = [];
      connection.state$.subscribe((s) => states.push(s));

      mockHub._simulateReconnected();

      expect(states).toContain('connected');
    });
  });

  describe('initial state', () => {
    it('should start in disconnected state', () => {
      let state: string | undefined;
      connection.state$.subscribe((s) => (state = s));

      expect(state).toBe('disconnected');
    });
  });

  describe('debug logging', () => {
    let logFn: LogFunction;
    let debugConn: SignalRRpcConnection;

    beforeEach(() => {
      logFn = vi.fn() as LogFunction;
      const hub = createMockHubConnection();
      mockHub = hub;
      debugConn = new SignalRRpcConnection(hub as unknown as HubConnection, {
        logger: new Logger({ enabled: true, logFn }),
      });
    });

    it('should log on connect', async () => {
      await debugConn.connect();

      expect(logFn).toHaveBeenCalledWith('[SignalR] starting connection');
      expect(logFn).toHaveBeenCalledWith('[SignalR] connected');
    });

    it('should log on connect failure', async () => {
      mockHub.start.mockRejectedValue(new Error('fail'));

      await expect(debugConn.connect()).rejects.toThrow('fail');
      expect(logFn).toHaveBeenCalledWith('[SignalR] start failed', expect.any(Error));
    });

    it('should log on disconnect', async () => {
      await debugConn.connect();
      await debugConn.disconnect();

      expect(logFn).toHaveBeenCalledWith('[SignalR] stopping connection');
      expect(logFn).toHaveBeenCalledWith('[SignalR] disconnected');
    });

    it('should log on send', async () => {
      await debugConn.connect();
      await debugConn.send('{"test":true}');

      expect(logFn).toHaveBeenCalledWith('[SignalR] send', expect.objectContaining({ data: '{"test":true}' }));
    });

    it('should log on message received', () => {
      mockHub._simulateMessage('ReceiveMessage', 'hello');

      expect(logFn).toHaveBeenCalledWith('[SignalR] message received', expect.objectContaining({ message: 'hello' }));
    });

    it('should log on close', async () => {
      await debugConn.connect();
      mockHub._simulateClose();

      expect(logFn).toHaveBeenCalledWith('[SignalR] connection closed', expect.any(Object));
    });

    it('should log on reconnecting', async () => {
      await debugConn.connect();
      mockHub._simulateReconnecting();

      expect(logFn).toHaveBeenCalledWith('[SignalR] reconnecting', expect.any(Object));
    });

    it('should log on reconnected', async () => {
      await debugConn.connect();
      mockHub._simulateReconnected();

      expect(logFn).toHaveBeenCalledWith('[SignalR] reconnected', expect.any(Object));
    });

    it('should not log when logger is disabled', async () => {
      const silentLogFn = vi.fn() as LogFunction;
      const hub = createMockHubConnection();
      const silentConn = new SignalRRpcConnection(hub as unknown as HubConnection, {
        logger: new Logger({ enabled: false, logFn: silentLogFn }),
      });

      await silentConn.connect();
      await silentConn.send('data');
      await silentConn.disconnect();

      expect(silentLogFn).not.toHaveBeenCalled();
    });
  });
});
