import { RpcMethodDefinition } from '../../rpc';
import { Channel, Symbol } from './trade-update';

export interface SubscribeParams {
  channel: Channel;
  symbol: Symbol;
}

export interface SubscribeResult {
  event: string;
}

export interface BlockchainMethods {
  subscribe: RpcMethodDefinition<SubscribeParams, SubscribeResult>;
  unsubscribe: RpcMethodDefinition<SubscribeParams, SubscribeResult>;
}
