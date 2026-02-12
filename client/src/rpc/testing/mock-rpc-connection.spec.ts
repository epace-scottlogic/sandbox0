import { MockRpcConnection } from './mock-rpc-connection';

describe('MockRpcConnection', () => {
  let connection: MockRpcConnection;

  beforeEach(() => {
    connection = new MockRpcConnection();
  });

  afterEach(() => {
    connection.dispose();
  });

  describe('connect', () => {
    it('should transition to connected state', async () => {
      let state: string | undefined;
      connection.state$.subscribe((s) => (state = s));

      await connection.connect();

      expect(state).toBe('connected');
      expect(connection.connectCalls).toBe(1);
    });

    it('should throw when shouldFailConnect is true', async () => {
      connection.shouldFailConnect = true;

      await expect(connection.connect()).rejects.toThrow('Connection failed');
    });
  });

  describe('disconnect', () => {
    it('should transition to disconnected state', async () => {
      await connection.connect();

      let state: string | undefined;
      connection.state$.subscribe((s) => (state = s));

      await connection.disconnect();

      expect(state).toBe('disconnected');
      expect(connection.disconnectCalls).toBe(1);
    });
  });

  describe('send', () => {
    it('should record sent messages', async () => {
      await connection.send('message1');
      await connection.send('message2');

      expect(connection.sentMessages).toEqual(['message1', 'message2']);
    });

    it('should throw when shouldFailSend is true', async () => {
      connection.shouldFailSend = true;

      await expect(connection.send('data')).rejects.toThrow('Send failed');
    });
  });

  describe('simulateMessage', () => {
    it('should emit messages on messages$', () => {
      const received: string[] = [];
      connection.messages$.subscribe((m) => received.push(m));

      connection.simulateMessage('test-message');

      expect(received).toEqual(['test-message']);
    });
  });

  describe('simulateState', () => {
    it('should emit the given state', () => {
      let state: string | undefined;
      connection.state$.subscribe((s) => (state = s));

      connection.simulateState('reconnecting');

      expect(state).toBe('reconnecting');
    });
  });

  describe('initial state', () => {
    it('should start disconnected', () => {
      let state: string | undefined;
      connection.state$.subscribe((s) => (state = s));

      expect(state).toBe('disconnected');
    });
  });
});
