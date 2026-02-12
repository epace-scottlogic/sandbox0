import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { ConnectionState } from '../models';
import { RpcConnection } from '../rpc-connection';

export class MockRpcConnection implements RpcConnection {
  private readonly stateSubject = new BehaviorSubject<ConnectionState>('disconnected');
  private readonly messagesSubject = new Subject<string>();
  readonly sentMessages: string[] = [];
  connectCalls = 0;
  disconnectCalls = 0;
  shouldFailConnect = false;
  shouldFailSend = false;

  get state$(): Observable<ConnectionState> {
    return this.stateSubject.asObservable();
  }

  get messages$(): Observable<string> {
    return this.messagesSubject.asObservable();
  }

  async connect(): Promise<void> {
    if (this.shouldFailConnect) {
      throw new Error('Connection failed');
    }
    this.connectCalls++;
    this.stateSubject.next('connected');
  }

  async disconnect(): Promise<void> {
    this.disconnectCalls++;
    this.stateSubject.next('disconnected');
  }

  async send(data: string): Promise<void> {
    if (this.shouldFailSend) {
      throw new Error('Send failed');
    }
    this.sentMessages.push(data);
  }

  simulateMessage(message: string): void {
    this.messagesSubject.next(message);
  }

  simulateState(state: ConnectionState): void {
    this.stateSubject.next(state);
  }

  dispose(): void {
    this.messagesSubject.complete();
    this.stateSubject.complete();
  }
}
