# DataServer Architecture

This document describes the architecture for the DataServer backend, which provides real-time market data from external exchanges (starting with Blockchain.com Exchange).

## Overview

The backend follows Clean Architecture principles with clear separation of concerns across layers. Data flows from external WebSocket APIs through the application and out to consumers (REST API, SignalR).

## Project Structure

```
server/
├── DataServer.Api/              # HTTP API and SignalR endpoints
├── DataServer.Application/      # Business logic, interfaces, services
├── DataServer.Connectors/       # External data source implementations
├── DataServer.Domain/           # Core entities and enums
├── DataServer.Infrastructure/   # Caching, logging, cross-cutting concerns
└── DataServer.Tests/            # Unit and integration tests
```

## Layer Dependencies

```
                    ┌─────────────────┐
                    │  DataServer.Api │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
┌─────────────────┐ ┌───────────────┐ ┌──────────────────────┐
│ DataServer.     │ │ DataServer.   │ │ DataServer.          │
│ Connectors      │ │ Infrastructure│ │ Application          │
└────────┬────────┘ └───────┬───────┘ └──────────┬───────────┘
         │                  │                    │
         └──────────────────┼────────────────────┘
                            │
                            ▼
                  ┌──────────────────┐
                  │ DataServer.Domain│
                  └──────────────────┘
```

- **Domain** has no dependencies (core entities)
- **Application** depends on Domain (defines interfaces, services)
- **Connectors** depends on Domain (implements data hhjsource interfaces)
- **Infrastructure** depends on Domain and Application (implements repository interfaces)
- **Api** depends on Application, Connectors, Infrastructure (wires everything together)

## Data Flow

```
External WebSocket ──► IBlockchainDataSource ──► BlockchainDataService ──► IBlockchainDataRepository
                            (Connectors)             (Application)              (Infrastructure)
                                 │                        │                           │
                                 │                        │                           │
                            TradeReceived            TradeReceived                    │
                               event                   event                          │
                                 │                        │                           │
                                 └────────────────────────┼───────────────────────────┘
                                                          │
                                                          ▼
                                                    API / SignalR
                                                    (real-time push)

```

## Domain Layer

Contains core entities for the Blockchain.com Exchange WebSocket API. This includes enums for channels, events, actions, symbols, and trade sides, as well as request/response records for WebSocket communication. All enums use `[EnumMember]` attributes for JSON serialization mapping.

## Application Layer

Defines interfaces that outer layers implement and provides services that orchestrate data flow. The `BlockchainDataService` connects to the data source, subscribes to trade events, stores incoming trades in the repository, and re-emits events for real-time consumers.

## Connectors Layer (TODO)

Will implement `IBlockchainDataSource` with:
- WebSocket client connecting to `wss://ws.blockchain.info/mercury-gateway/v1/ws`
- Required header: `Origin: https://exchange.blockchain.com`
- JSON message serialization using `EnumMember` attributes
- Automatic reconnection logic
- Subscription management

## Infrastructure Layer (TODO)

Will implement `IBlockchainDataRepository` with:
- In-memory cache using `ConcurrentDictionary`
- Circular buffer for recent trades (configurable size per symbol)
- Thread-safe operations

## API Layer (TODO)

Will provide:
- REST endpoints for querying cached data
- SignalR hub for real-time trade updates
- Background service to host `BlockchainDataService` lifecycle

## External API Reference

Blockchain.com Exchange WebSocket API: https://exchange.blockchain.com/api

### Key Channels

| Channel | Description |
|---------|-------------|
| `trades` | Real-time trade executions |
| `l2` | Level 2 order book (aggregated) |
| `l3` | Level 3 order book (individual orders) |
| `ticker` | Ticker updates |
| `prices` | Candlestick/OHLC data |

## Implementation Roadmap

1. [x] Domain entities and enums
2. [x] Application layer interfaces and service
3. [ ] Infrastructure layer (in-memory repository)
4. [ ] Connectors layer (WebSocket client)
5. [ ] API layer (REST endpoints, SignalR hub)
6. [ ] Integration testing with live API
