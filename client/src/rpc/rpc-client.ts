import { Observable, Subject, Subscription, filter, map, take, throwError, timeout } from 'rxjs';
import {
  ConnectionState,
  JSON_RPC_VERSION,
  JsonRpcError,
  JsonRpcNotification,
  JsonRpcResponse,
  RpcMethodDefinition,
  RpcMethodMap,
} from './models';
import { RpcConnection } from './rpc-connection';
import { Logger, SILENT_LOGGER } from '../common/logger';

export interface RpcClientOptions {
  timeout?: number;
  logger?: Logger;
}

const DEFAULT_TIMEOUT = 30000;

export class RpcClientError extends Error {
  constructor(public readonly rpcError: JsonRpcError) {
    super(rpcError.message);
    this.name = 'RpcClientError';
  }
}

export class RpcClient<TMethods extends { [K in keyof TMethods]: RpcMethodDefinition } = RpcMethodMap> {
  private requestId = 0;
  private readonly pendingRequests = new Map<string, Subject<JsonRpcResponse>>();
  private readonly notificationsSubject = new Subject<JsonRpcNotification>();
  private readonly timeoutMs: number;
  private subscription: Subscription | null = null;
  private readonly logger: Logger;

  constructor(
    private readonly connection: RpcConnection,
    options?: RpcClientOptions,
  ) {
    this.timeoutMs = options?.timeout ?? DEFAULT_TIMEOUT;
    this.logger = options?.logger ?? SILENT_LOGGER;
    this.subscription = this.connection.messages$.subscribe((raw) => {
      this.logger.debug('[RpcClient] raw message', raw);
      this.handleMessage(raw);
    });
  }

  get connectionState$(): Observable<ConnectionState> {
    return this.connection.state$;
  }

  async connect(): Promise<void> {
    this.logger.debug('[RpcClient] connect()');
    return this.connection.connect().catch((err) => {
      this.logger.debug('[RpcClient] connect failed', err);
      throw err;
    });
  }

  async disconnect(): Promise<void> {
    this.logger.debug('[RpcClient] disconnect()');
    this.subscription?.unsubscribe();
    this.subscription = null;
    this.clearPendingRequests(new Error('Connection closed'));
    return this.connection.disconnect().catch((err) => {
      this.logger.debug('[RpcClient] disconnect failed', err);
      throw err;
    });
  }

  invoke<M extends keyof TMethods & string>(
    method: M,
    ...args: TMethods[M]['params'] extends void ? [] : [TMethods[M]['params']]
  ): Observable<TMethods[M]['result']> {
    const id = String(++this.requestId);
    const params = args.length > 0 ? args[0] : undefined;

    const request = {
      jsonrpc: JSON_RPC_VERSION,
      method,
      params,
      id,
    };

    const responseSubject = new Subject<JsonRpcResponse>();
    this.logger.debug('[RpcClient] invoke()', { id, method, params });
    this.pendingRequests.set(id, responseSubject);

    const result$ = responseSubject.pipe(
      take(1),
      map((response) => {
        this.logger.debug('[RpcClient] response', { id, method, response });
        if (response.error) {
          throw new RpcClientError(response.error);
        }
        return response.result as TMethods[M]['result'];
      }),
      timeout({
        each: this.timeoutMs,
        with: () => {
          this.pendingRequests.delete(id);
          this.logger.debug('[RpcClient] timeout', { id, method, timeoutMs: this.timeoutMs });
          return throwError(
            () => new Error(`RPC request '${method}' timed out after ${this.timeoutMs}ms`),
          );
        },
      }),
    );

    this.connection.send(JSON.stringify(request)).catch((err: unknown) => {
      this.logger.debug('[RpcClient] send failed', { id, method, err });
      responseSubject.error(err);
      this.pendingRequests.delete(id);
    });

    return result$;
  }

  onNotification<TParams = unknown>(method: string): Observable<TParams> {
    return this.notificationsSubject.pipe(
      filter((n) => n.method === method),
      map((n) => n.params as TParams),
    );
  }

  dispose(): void {
    this.logger.debug('[RpcClient] dispose()');
    this.subscription?.unsubscribe();
    this.subscription = null;
    // Error active pending requests so awaiting callers see rejection; otherwise complete silently
    const subjects = [...this.pendingRequests.values()];
    this.pendingRequests.clear();
    for (const subject of subjects) {
      if (!subject.closed) {
        const hasObservers = (subject as unknown as { observers?: unknown[] }).observers?.length;
        if (hasObservers) {
          subject.error(new Error('Client disposed'));
        } else {
          subject.complete();
        }
      }
    }
    this.notificationsSubject.complete();
  }

  private clearPendingRequests(error: Error): void {
    const subjects = [...this.pendingRequests.values()];
    this.pendingRequests.clear();
    for (const subject of subjects) {
      if (!subject.closed) {
        subject.error(error);
      }
    }
  }

  private handleMessage(raw: string): void {
    let message: Record<string, unknown>;
    try {
      message = JSON.parse(raw) as Record<string, unknown>;
    } catch {
      return;
    }

    if ('id' in message && message['id'] != null) {
      const subject = this.pendingRequests.get(String(message['id']));
      if (subject) {
        subject.next(message as unknown as JsonRpcResponse);
        subject.complete();
        this.pendingRequests.delete(String(message['id']));
      }
    } else if ('method' in message) {
      this.notificationsSubject.next(message as unknown as JsonRpcNotification);
    }
  }
}
