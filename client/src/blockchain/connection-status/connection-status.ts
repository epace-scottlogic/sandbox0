import { Component, input } from '@angular/core';
import { ConnectionState } from '../../rpc';

export type OverallConnectionStatus = 'connected' | 'reconnecting' | 'disconnected';

@Component({
  selector: 'app-connection-status',
  templateUrl: './connection-status.html',
  styleUrl: './connection-status.css',
})
export class ConnectionStatus {
  signalRState = input.required<ConnectionState>();
  backendConnected = input<boolean>(true);

  get status(): OverallConnectionStatus {
    const signalR = this.signalRState();
    if (signalR === 'disconnected') return 'disconnected';
    if (signalR === 'reconnecting') return 'reconnecting';
    if (signalR === 'connecting') return 'reconnecting';
    if (!this.backendConnected()) return 'disconnected';
    return 'connected';
  }

  get label(): string {
    switch (this.status) {
      case 'connected':
        return 'Connected';
      case 'reconnecting':
        return 'Reconnecting';
      case 'disconnected':
        return this.signalRState() === 'disconnected'
          ? 'Disconnected'
          : 'Server lost connection';
    }
  }

  get statusClass(): string {
    return `status-${this.status}`;
  }
}
