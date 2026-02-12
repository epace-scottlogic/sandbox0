import {
  JSON_RPC_VERSION,
  createJsonRpcRequest,
  isJsonRpcResponse,
  isJsonRpcNotification,
  isJsonRpcSuccess,
  resetRequestCounter,
} from './models';

describe('models', () => {
  describe('createJsonRpcRequest', () => {
    beforeEach(() => {
      resetRequestCounter();
    });

    it('should create a request with auto-incremented id', () => {
      const request = createJsonRpcRequest('subscribe', { channel: 'trades' });

      expect(request).toEqual({
        jsonrpc: JSON_RPC_VERSION,
        method: 'subscribe',
        params: { channel: 'trades' },
        id: '1',
      });
    });

    it('should increment ids across multiple calls', () => {
      const first = createJsonRpcRequest('method1');
      const second = createJsonRpcRequest('method2');

      expect(first.id).toBe('1');
      expect(second.id).toBe('2');
    });

    it('should use a custom id when provided', () => {
      const request = createJsonRpcRequest('subscribe', undefined, 'custom-id');

      expect(request.id).toBe('custom-id');
    });

    it('should set jsonrpc version to 2.0', () => {
      const request = createJsonRpcRequest('test');

      expect(request.jsonrpc).toBe('2.0');
    });

    it('should handle undefined params', () => {
      const request = createJsonRpcRequest('test');

      expect(request.params).toBeUndefined();
    });

    it('should preserve typed params', () => {
      interface MyParams {
        symbol: string;
        count: number;
      }
      const params: MyParams = { symbol: 'BTC-USD', count: 10 };
      const request = createJsonRpcRequest<MyParams>('query', params);

      expect(request.params).toEqual({ symbol: 'BTC-USD', count: 10 });
    });
  });

  describe('isJsonRpcResponse', () => {
    it('should return true for a valid success response', () => {
      const response = { jsonrpc: '2.0', result: { data: 'test' }, id: '1' };

      expect(isJsonRpcResponse(response)).toBe(true);
    });

    it('should return true for a valid error response', () => {
      const response = {
        jsonrpc: '2.0',
        error: { code: -32600, message: 'Invalid Request' },
        id: '1',
      };

      expect(isJsonRpcResponse(response)).toBe(true);
    });

    it('should return false for a notification (no id)', () => {
      const notification = { jsonrpc: '2.0', method: 'update', params: {} };

      expect(isJsonRpcResponse(notification)).toBe(false);
    });

    it('should return false for a message with null id', () => {
      const message = { jsonrpc: '2.0', result: {}, id: null };

      expect(isJsonRpcResponse(message)).toBe(false);
    });

    it('should return false for null', () => {
      expect(isJsonRpcResponse(null)).toBe(false);
    });

    it('should return false for undefined', () => {
      expect(isJsonRpcResponse(undefined)).toBe(false);
    });

    it('should return false for a primitive', () => {
      expect(isJsonRpcResponse('string')).toBe(false);
    });
  });

  describe('isJsonRpcNotification', () => {
    it('should return true for a valid notification', () => {
      const notification = { jsonrpc: '2.0', method: 'trades.update', params: { price: 100 } };

      expect(isJsonRpcNotification(notification)).toBe(true);
    });

    it('should return true for a notification without params', () => {
      const notification = { jsonrpc: '2.0', method: 'heartbeat' };

      expect(isJsonRpcNotification(notification)).toBe(true);
    });

    it('should return false for a response with an id', () => {
      const response = { jsonrpc: '2.0', method: 'test', id: '1' };

      expect(isJsonRpcNotification(response)).toBe(false);
    });

    it('should return true when id is null', () => {
      const message = { jsonrpc: '2.0', method: 'test', id: null };

      expect(isJsonRpcNotification(message)).toBe(true);
    });

    it('should return false for null', () => {
      expect(isJsonRpcNotification(null)).toBe(false);
    });

    it('should return false for an object without method', () => {
      const message = { jsonrpc: '2.0', params: {} };

      expect(isJsonRpcNotification(message)).toBe(false);
    });
  });

  describe('isJsonRpcSuccess', () => {
    it('should return true when no error is present', () => {
      const response = { jsonrpc: '2.0', result: { data: 'ok' }, id: '1' };

      expect(isJsonRpcSuccess(response)).toBe(true);
    });

    it('should return false when error is present', () => {
      const response = {
        jsonrpc: '2.0',
        error: { code: -32600, message: 'Invalid Request' },
        id: '1',
      };

      expect(isJsonRpcSuccess(response)).toBe(false);
    });

    it('should return true when error is undefined', () => {
      const response = { jsonrpc: '2.0', result: null, id: '1' };

      expect(isJsonRpcSuccess(response)).toBe(true);
    });
  });
});
