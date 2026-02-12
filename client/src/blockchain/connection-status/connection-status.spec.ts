import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { ConnectionStatus, OverallConnectionStatus } from './connection-status';
import { ConnectionState } from '../../rpc';

@Component({
  template: `<app-connection-status
    [signalRState]="signalRState"
    [backendConnected]="backendConnected"
  />`,
  imports: [ConnectionStatus],
})
class TestHost {
  signalRState: ConnectionState = 'disconnected';
  backendConnected = true;
}

describe('ConnectionStatus', () => {
  let fixture: ComponentFixture<TestHost>;
  let host: TestHost;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHost],
    }).compileComponents();
  });

  function createFixture(state: ConnectionState, backend = true): void {
    fixture = TestBed.createComponent(TestHost);
    host = fixture.componentInstance;
    host.signalRState = state;
    host.backendConnected = backend;
    fixture.detectChanges();
  }

  function getComponent(): ConnectionStatus {
    return fixture.debugElement.children[0].componentInstance as ConnectionStatus;
  }

  function getStatusElement(): HTMLElement {
    return fixture.nativeElement.querySelector('.connection-status') as HTMLElement;
  }

  function getLabelText(): string {
    const el = fixture.nativeElement.querySelector('.status-label') as HTMLElement;
    return el.textContent?.trim() ?? '';
  }

  describe('status computation', () => {
    it('should return disconnected when SignalR is disconnected', () => {
      createFixture('disconnected');
      expect(getComponent().status).toBe('disconnected');
    });

    it('should return reconnecting when SignalR is reconnecting', () => {
      createFixture('reconnecting');
      expect(getComponent().status).toBe('reconnecting');
    });

    it('should return reconnecting when SignalR is connecting', () => {
      createFixture('connecting');
      expect(getComponent().status).toBe('reconnecting');
    });

    it('should return connected when SignalR is connected and backend is connected', () => {
      createFixture('connected', true);
      expect(getComponent().status).toBe('connected');
    });

    it('should return disconnected when SignalR is connected but backend is disconnected', () => {
      createFixture('connected', false);
      expect(getComponent().status).toBe('disconnected');
    });
  });

  describe('label', () => {
    it('should display Connected when fully connected', () => {
      createFixture('connected', true);
      expect(getLabelText()).toBe('Connected');
    });

    it('should display Reconnecting when SignalR is reconnecting', () => {
      createFixture('reconnecting');
      expect(getLabelText()).toBe('Reconnecting');
    });

    it('should display Disconnected when SignalR is disconnected', () => {
      createFixture('disconnected');
      expect(getLabelText()).toBe('Disconnected');
    });

    it('should display Server lost connection when backend is disconnected', () => {
      createFixture('connected', false);
      expect(getLabelText()).toBe('Server lost connection');
    });
  });

  describe('CSS class', () => {
    const cases: { signalR: ConnectionState; backend: boolean; expected: OverallConnectionStatus }[] = [
      { signalR: 'connected', backend: true, expected: 'connected' },
      { signalR: 'reconnecting', backend: true, expected: 'reconnecting' },
      { signalR: 'connecting', backend: true, expected: 'reconnecting' },
      { signalR: 'disconnected', backend: true, expected: 'disconnected' },
      { signalR: 'connected', backend: false, expected: 'disconnected' },
    ];

    for (const { signalR, backend, expected } of cases) {
      it(`should apply status-${expected} for signalR=${signalR}, backend=${backend}`, () => {
        createFixture(signalR, backend);
        expect(getStatusElement().classList).toContain(`status-${expected}`);
      });
    }
  });

  describe('DOM structure', () => {
    it('should render a status dot', () => {
      createFixture('disconnected');
      const dot = fixture.nativeElement.querySelector('.status-dot');
      expect(dot).toBeTruthy();
    });

    it('should render a status label', () => {
      createFixture('disconnected');
      const label = fixture.nativeElement.querySelector('.status-label');
      expect(label).toBeTruthy();
    });
  });
});
