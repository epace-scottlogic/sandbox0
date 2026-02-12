import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { ConnectionState } from './models';
import { RpcConnection } from './rpc-connection';
import { Logger, SILENT_LOGGER } from '../common/logger';

export interface SignalRConnectionOptions {
  sendMethodName?: string;
  receiveMethodName?: string;
  logger?: Logger;
}

const DEFAULT_SEND_METHOD = 'SendMessage';
const DEFAULT_RECEIVE_METHOD = 'ReceiveMessage';

export class SignalRRpcConnection implements RpcConnection {
  private readonly hubConnection: HubConnection;
  private readonly stateSubject = new BehaviorSubject<ConnectionState>('disconnected');
  private readonly messagesSubject = new Subject<string>();
  private readonly sendMethod: string;
  private readonly logger: Logger;

  constructor(hubConnection: HubConnection, options?: SignalRConnectionOptions) {
    this.hubConnection = hubConnection;
    this.sendMethod = options?.sendMethodName ?? DEFAULT_SEND_METHOD;
    const receiveMethod = options?.receiveMethodName ?? DEFAULT_RECEIVE_METHOD;
    this.logger = options?.logger ?? SILENT_LOGGER;

    this.hubConnection.on(receiveMethod, (message: string) => {
      this.logger.debug('[SignalR] message received', { method: receiveMethod, message });
      this.messagesSubject.next(message);
    });

    this.hubConnection.onclose((error) => {
      this.logger.debug('[SignalR] connection closed', { error });
      this.stateSubject.next('disconnected');
    });
    this.hubConnection.onreconnecting((error) => {
      this.logger.debug('[SignalR] reconnecting', { error });
      this.stateSubject.next('reconnecting');
    });
    this.hubConnection.onreconnected((connectionId) => {
      this.logger.debug('[SignalR] reconnected', { connectionId });
      this.stateSubject.next('connected');
    });
  }

  static create(hubUrl: string, options?: SignalRConnectionOptions): SignalRRpcConnection {
    const hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();
    return new SignalRRpcConnection(hubConnection, options);
  }

  get state$(): Observable<ConnectionState> {
    return this.stateSubject.asObservable();
  }

  get messages$(): Observable<string> {
    return this.messagesSubject.asObservable();
  }

  async connect(): Promise<void> {
    this.stateSubject.next('connecting');
    this.logger.debug('[SignalR] starting connection');
    await this.hubConnection.start().catch((err) => {
      this.logger.debug('[SignalR] start failed', err);
      throw err;
    });
    this.logger.debug('[SignalR] connected');
    this.stateSubject.next('connected');
  }

  async disconnect(): Promise<void> {
    this.logger.debug('[SignalR] stopping connection');
    await this.hubConnection.stop().catch((err) => {
      this.logger.debug('[SignalR] stop failed', err);
      throw err;
    });
    this.logger.debug('[SignalR] disconnected');
    this.stateSubject.next('disconnected');
  }

  async send(data: string): Promise<void> {
    this.logger.debug('[SignalR] send', { method: this.sendMethod, data });
    await this.hubConnection.invoke(this.sendMethod, data).catch((err) => {
      this.logger.debug('[SignalR] send failed', err);
      throw err;
    });
  }
}
