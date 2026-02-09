export type Symbol = 'ETH-USD' | 'BTC-USD';

export type Event = 'subscribed' | 'unsubscribed' | 'rejected' | 'snapshot' | 'updated';

export type Channel =
  | 'heartbeat'
  | 'l2'
  | 'l3'
  | 'prices'
  | 'symbols'
  | 'ticker'
  | 'trades'
  | 'auth'
  | 'balances'
  | 'trading';

export type Side = 'buy' | 'sell';

export interface TradeUpdate {
  seqnum: number;
  event: Event;
  channel: Channel;
  symbol: Symbol;
  timestamp: string;
  side: Side;
  qty: number;
  price: number;
  tradeId: string;
}
