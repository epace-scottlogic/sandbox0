import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SubscriptionContainer } from './subscription-container';
import { Component } from '@angular/core';
import { TradeUpdate } from '../models/trade-update';

@Component({
  template: `
    <app-subscription-container
      [symbol]="symbol"
      [trades]="trades"
      [loading]="loading"
      [state]="state"
      (unsubscribed)="onUnsubscribed($event)"
      (resubscribed)="onResubscribed($event)"
      (dismissed)="onDismissed($event)"
    />
  `,
  imports: [SubscriptionContainer],
})
class TestHost {
  symbol = 'BTC-USD';
  trades: TradeUpdate[] = [];
  loading = false;
  state: 'active' | 'paused' = 'active';
  unsubscribedSymbol = '';
  resubscribedSymbol = '';
  dismissedSymbol = '';

  onUnsubscribed(symbol: string): void {
    this.unsubscribedSymbol = symbol;
  }

  onResubscribed(symbol: string): void {
    this.resubscribedSymbol = symbol;
  }

  onDismissed(symbol: string): void {
    this.dismissedSymbol = symbol;
  }
}

describe('SubscriptionContainer', () => {
  let fixture: ComponentFixture<TestHost>;
  let host: TestHost;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHost],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHost);
    host = fixture.componentInstance;
  });

  it('should create the component', () => {
    fixture.detectChanges();
    const container = fixture.nativeElement.querySelector('.subscription-container');
    expect(container).toBeTruthy();
  });

  describe('active state', () => {
    it('should show the Unsubscribe button when active', () => {
      host.state = 'active';
      fixture.detectChanges();

      const unsubBtn = fixture.nativeElement.querySelector('.unsubscribe-btn');
      const resubBtn = fixture.nativeElement.querySelector('.resubscribe-btn');
      const dismissBtn = fixture.nativeElement.querySelector('.dismiss-btn');

      expect(unsubBtn).toBeTruthy();
      expect(unsubBtn.textContent.trim()).toBe('Unsubscribe');
      expect(resubBtn).toBeNull();
      expect(dismissBtn).toBeNull();
    });

    it('should not have paused class when active', () => {
      host.state = 'active';
      fixture.detectChanges();

      const container = fixture.nativeElement.querySelector('.subscription-container');
      expect(container.classList.contains('paused')).toBe(false);
    });

    it('should emit unsubscribed event when Unsubscribe is clicked', () => {
      host.state = 'active';
      fixture.detectChanges();

      const unsubBtn: HTMLButtonElement = fixture.nativeElement.querySelector('.unsubscribe-btn');
      unsubBtn.click();

      expect(host.unsubscribedSymbol).toBe('BTC-USD');
    });
  });

  describe('paused state', () => {
    it('should show Resubscribe and Dismiss buttons when paused', () => {
      host.state = 'paused';
      fixture.detectChanges();

      const unsubBtn = fixture.nativeElement.querySelector('.unsubscribe-btn');
      const resubBtn = fixture.nativeElement.querySelector('.resubscribe-btn');
      const dismissBtn = fixture.nativeElement.querySelector('.dismiss-btn');

      expect(unsubBtn).toBeNull();
      expect(resubBtn).toBeTruthy();
      expect(resubBtn.textContent.trim()).toBe('Resubscribe');
      expect(dismissBtn).toBeTruthy();
      expect(dismissBtn.textContent.trim()).toBe('Dismiss');
    });

    it('should have paused class when paused', () => {
      host.state = 'paused';
      fixture.detectChanges();

      const container = fixture.nativeElement.querySelector('.subscription-container');
      expect(container.classList.contains('paused')).toBe(true);
    });

    it('should emit resubscribed event when Resubscribe is clicked', () => {
      host.state = 'paused';
      fixture.detectChanges();

      const resubBtn: HTMLButtonElement = fixture.nativeElement.querySelector('.resubscribe-btn');
      resubBtn.click();

      expect(host.resubscribedSymbol).toBe('BTC-USD');
    });

    it('should emit dismissed event when Dismiss is clicked', () => {
      host.state = 'paused';
      fixture.detectChanges();

      const dismissBtn: HTMLButtonElement = fixture.nativeElement.querySelector('.dismiss-btn');
      dismissBtn.click();

      expect(host.dismissedSymbol).toBe('BTC-USD');
    });
  });

  it('should display the symbol name', () => {
    fixture.detectChanges();
    const symbolEl = fixture.nativeElement.querySelector('.subscription-symbol');
    expect(symbolEl.textContent.trim()).toBe('BTC-USD');
  });
});
