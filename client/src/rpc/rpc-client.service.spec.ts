import { TestBed } from '@angular/core/testing';
import { RpcClient } from './rpc-client';
import { RpcConnection } from './rpc-connection';
import { RPC_CONNECTION, provideRpcClient, createRpcClient } from './rpc-client.service';
import { MockRpcConnection } from './testing/mock-rpc-connection';
import { RpcMethodDefinition } from './models';

interface TestMethods {
  ping: RpcMethodDefinition<void, string>;
}

describe('rpc-client.service', () => {
  describe('provideRpcClient', () => {
    it('should provide RPC_CONNECTION and RpcClient tokens', () => {
      TestBed.configureTestingModule({
        providers: [
          provideRpcClient<TestMethods>({ hubUrl: 'http://localhost/test' }),
        ],
      });

      const connection = TestBed.inject(RPC_CONNECTION);
      const client = TestBed.inject(RpcClient);

      expect(connection).toBeDefined();
      expect(client).toBeInstanceOf(RpcClient);
    });
  });

  describe('createRpcClient', () => {
    it('should create an RpcClient with the given connection', () => {
      const mockConnection = new MockRpcConnection();
      const client = createRpcClient<TestMethods>(mockConnection);

      expect(client).toBeInstanceOf(RpcClient);

      client.dispose();
      mockConnection.dispose();
    });

    it('should pass options to the RpcClient', () => {
      const mockConnection = new MockRpcConnection();
      const client = createRpcClient<TestMethods>(mockConnection, { timeout: 5000 });

      expect(client).toBeInstanceOf(RpcClient);

      client.dispose();
      mockConnection.dispose();
    });
  });
});
