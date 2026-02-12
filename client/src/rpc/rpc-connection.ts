import { Observable } from 'rxjs';
import { ConnectionState } from './models';

export interface RpcConnection {
  readonly state$: Observable<ConnectionState>;
  readonly messages$: Observable<string>;
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  send(data: string): Promise<void>;
}
