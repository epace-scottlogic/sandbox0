import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { Blockchain } from './blockchain';
import { BlockchainRpcService, BackendConnectionEvent } from './blockchain-rpc.service';
import { TradeUpdate } from './models/trade-update';
import { ConnectionState } from '../rpc';

function createMockRpcService() {
  const connectionStateSubject = new Subject<ConnectionState>();
  const tradeSubject = new Subject<TradeUpdate>();
  const backendConnectionSubject = new Subject<BackendConnectionEvent>();

  return {
    connectionState$: connectionStateSubject.asObservable(),
    connect: vi.fn().mockResolvedValue(undefined),
    disconnect: vi.fn().mockResolvedValue(undefined),
    subscribe: vi.fn().mockReturnValue(of({ event: 'subscribed' })),
    unsubscribe: vi.fn().mockReturnValue(of({ event: 'unsubscribed' })),
    onTradeUpdate: vi.fn().mockReturnValue(tradeSubject.asObservable()),
    onBackendConnectionEvent: vi.fn().mockReturnValue(backendConnectionSubject.asObservable()),
    _connectionStateSubject: connectionStateSubject,
    _tradeSubject: tradeSubject,
    _backendConnectionSubject: backendConnectionSubject,
  };
}

function tick(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

function createTrade(symbol: 'BTC-USD' | 'ETH-USD', overrides?: Partial<TradeUpdate>): TradeUpdate {
  return {
    seqnum: 1,
    event: 'updated',
    channel: 'trades',
    symbol,
    timestamp: '2026-01-01T00:00:00Z',
    side: 'buy',
    qty: 0.5,
    price: 50000,
    tradeId: `trade-${Math.random()}`,
    ...overrides,
  };
}

describe('Blockchain', () => {
  let component: Blockchain;
  let mockRpcService: ReturnType<typeof createMockRpcService>;

  beforeEach(async () => {
    mockRpcService = createMockRpcService();

    await TestBed.configureTestingModule({
      imports: [Blockchain],
      providers: [{ provide: BlockchainRpcService, useValue: mockRpcService }],
    }).compileComponents();

    const fixture = TestBed.createComponent(Blockchain);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    mockRpcService.disconnect();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  describe('onSymbolSelected', () => {
    it('should add a subscription with active state', async () => {
      component.onSymbolSelected('BTC-USD');

      expect(component.subscriptions).toHaveLength(1);
      expect(component.subscriptions[0].symbol).toBe('BTC-USD');
      expect(component.subscriptions[0].state).toBe('active');
    });

    it('should not add duplicate subscriptions', () => {
      component.onSymbolSelected('BTC-USD');
      component.onSymbolSelected('BTC-USD');

      expect(component.subscriptions).toHaveLength(1);
    });

    it('should resubscribe a paused subscription when selected again', () => {
      component.onSymbolSelected('BTC-USD');
      component.onUnsubscribe('BTC-USD');

      expect(component.subscriptions[0].state).toBe('paused');

      component.onSymbolSelected('BTC-USD');

      expect(component.subscriptions).toHaveLength(1);
      expect(component.subscriptions[0].state).toBe('active');
      expect(mockRpcService.subscribe).toHaveBeenCalledWith('BTC-USD');
    });
  });

  describe('activeSymbols', () => {
    it('should only return symbols with active state', () => {
      component.onSymbolSelected('BTC-USD');
      component.onSymbolSelected('ETH-USD');

      expect(component.activeSymbols).toEqual(['BTC-USD', 'ETH-USD']);

      component.onUnsubscribe('BTC-USD');

      expect(component.activeSymbols).toEqual(['ETH-USD']);
    });

    it('should not include paused subscriptions', () => {
      component.onSymbolSelected('BTC-USD');
      component.onUnsubscribe('BTC-USD');

      expect(component.activeSymbols).toEqual([]);
      expect(component.subscriptions).toHaveLength(1);
    });
  });

  describe('onUnsubscribe', () => {
    it('should pause the subscription instead of removing it', () => {
      component.onSymbolSelected('BTC-USD');
      component.onUnsubscribe('BTC-USD');

      expect(component.subscriptions).toHaveLength(1);
      expect(component.subscriptions[0].state).toBe('paused');
    });

    it('should preserve trade data when pausing', () => {
      component.onSymbolSelected('BTC-USD');
      component.subscriptions = [
        { symbol: 'BTC-USD', trades: [createTrade('BTC-USD')], loading: false, state: 'active' },
      ];

      component.onUnsubscribe('BTC-USD');

      expect(component.subscriptions[0].trades).toHaveLength(1);
      expect(component.subscriptions[0].state).toBe('paused');
    });

    it('should call rpcService.unsubscribe', () => {
      component.onSymbolSelected('BTC-USD');
      component.onUnsubscribe('BTC-USD');

      expect(mockRpcService.unsubscribe).toHaveBeenCalledWith('BTC-USD');
    });
  });

  describe('onResubscribe', () => {
    it('should set state to active and loading to true', () => {
      component.subscriptions = [
        { symbol: 'BTC-USD', trades: [createTrade('BTC-USD')], loading: false, state: 'paused' },
      ];

      component.onResubscribe('BTC-USD');

      expect(component.subscriptions[0].state).toBe('active');
      expect(mockRpcService.subscribe).toHaveBeenCalledWith('BTC-USD');
    });

    it('should set loading to false after successful resubscription', () => {
      component.subscriptions = [
        { symbol: 'BTC-USD', trades: [], loading: false, state: 'paused' },
      ];

      component.onResubscribe('BTC-USD');

      expect(component.subscriptions[0].loading).toBe(false);
      expect(component.subscriptions[0].state).toBe('active');
    });

    it('should revert to paused state on resubscription error', () => {
      mockRpcService.subscribe.mockReturnValue(throwError(() => new Error('Subscribe failed')));
      component.subscriptions = [
        { symbol: 'BTC-USD', trades: [], loading: false, state: 'paused' },
      ];

      component.onResubscribe('BTC-USD');

      expect(component.subscriptions[0].state).toBe('paused');
      expect(component.subscriptions[0].loading).toBe(false);
      expect(component.connectionError).toBe('Subscribe failed');
    });

    it('should preserve existing trades when resubscribing', () => {
      const existingTrade = createTrade('BTC-USD');
      component.subscriptions = [
        { symbol: 'BTC-USD', trades: [existingTrade], loading: false, state: 'paused' },
      ];

      component.onResubscribe('BTC-USD');

      expect(component.subscriptions[0].trades).toContain(existingTrade);
    });
  });

  describe('onDismiss', () => {
    it('should remove the subscription entirely', () => {
      component.subscriptions = [
        { symbol: 'BTC-USD', trades: [createTrade('BTC-USD')], loading: false, state: 'paused' },
      ];

      component.onDismiss('BTC-USD');

      expect(component.subscriptions).toHaveLength(0);
    });

    it('should only remove the dismissed subscription', () => {
      component.subscriptions = [
        { symbol: 'BTC-USD', trades: [], loading: false, state: 'paused' },
        { symbol: 'ETH-USD', trades: [], loading: false, state: 'active' },
      ];

      component.onDismiss('BTC-USD');

      expect(component.subscriptions).toHaveLength(1);
      expect(component.subscriptions[0].symbol).toBe('ETH-USD');
    });
  });

  describe('trade updates on paused subscriptions', () => {
    it('should not add trades to paused subscriptions', async () => {
      await component.ngOnInit();

      component.subscriptions = [
        { symbol: 'BTC-USD', trades: [], loading: false, state: 'paused' },
      ];

      mockRpcService._tradeSubject.next(createTrade('BTC-USD'));

      expect(component.subscriptions[0].trades).toHaveLength(0);
    });

    it('should add trades to active subscriptions', async () => {
      await component.ngOnInit();

      component.subscriptions = [
        { symbol: 'BTC-USD', trades: [], loading: false, state: 'active' },
      ];

      mockRpcService._tradeSubject.next(createTrade('BTC-USD'));

      expect(component.subscriptions[0].trades).toHaveLength(1);
    });
  });
});

describe('Blockchain loading state', () => {
  let component: Blockchain;
  let mockRpcService: ReturnType<typeof createMockRpcService>;

  beforeEach(async () => {
    mockRpcService = createMockRpcService();

    await TestBed.configureTestingModule({
      imports: [Blockchain],
      providers: [{ provide: BlockchainRpcService, useValue: mockRpcService }],
    }).compileComponents();

    const fixture = TestBed.createComponent(Blockchain);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await tick();
  });

  afterEach(() => {
    mockRpcService.disconnect();
  });

  it('should keep loading true after subscription succeeds until first trade arrives', () => {
    component.onSymbolSelected('BTC-USD');

    const sub = component.subscriptions.find((s) => s.symbol === 'BTC-USD');
    expect(sub).toBeDefined();
    expect(sub!.loading).toBe(true);
    expect(sub!.trades).toHaveLength(0);
  });

  it('should set loading to false when the first trade arrives', () => {
    component.onSymbolSelected('BTC-USD');

    mockRpcService._tradeSubject.next(createTrade('BTC-USD'));

    const sub = component.subscriptions.find((s) => s.symbol === 'BTC-USD');
    expect(sub).toBeDefined();
    expect(sub!.loading).toBe(false);
    expect(sub!.trades).toHaveLength(1);
  });

  it('should not affect loading of other symbols when a trade arrives', () => {
    component.onSymbolSelected('BTC-USD');
    component.onSymbolSelected('ETH-USD');

    mockRpcService._tradeSubject.next(createTrade('BTC-USD'));

    const btcSub = component.subscriptions.find((s) => s.symbol === 'BTC-USD');
    const ethSub = component.subscriptions.find((s) => s.symbol === 'ETH-USD');
    expect(btcSub!.loading).toBe(false);
    expect(ethSub!.loading).toBe(true);
  });
});
