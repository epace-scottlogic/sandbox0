export const JSON_RPC_VERSION = '2.0';

export interface JsonRpcRequest<TParams = unknown> {
  jsonrpc: string;
  method: string;
  params?: TParams;
  id: string;
}

export interface JsonRpcResponse<TResult = unknown> {
  jsonrpc: string;
  result?: TResult;
  error?: JsonRpcError;
  id: string;
}

export interface JsonRpcError {
  code: number;
  message: string;
  data?: unknown;
}

export interface JsonRpcNotification<TParams = unknown> {
  jsonrpc: string;
  method: string;
  params?: TParams;
}

export interface RpcMethodDefinition<TParams = unknown, TResult = unknown> {
  params: TParams;
  result: TResult;
}

export type RpcMethodMap = { [K: string]: RpcMethodDefinition };

export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

let requestCounter = 0;

export function resetRequestCounter(): void {
  requestCounter = 0;
}

export function createJsonRpcRequest<TParams>(
  method: string,
  params?: TParams,
  id?: string,
): JsonRpcRequest<TParams> {
  return {
    jsonrpc: JSON_RPC_VERSION,
    method,
    params,
    id: id ?? String(++requestCounter),
  };
}

export function isJsonRpcResponse(message: unknown): message is JsonRpcResponse {
  return (
    typeof message === 'object' &&
    message !== null &&
    'jsonrpc' in message &&
    'id' in message &&
    (message as Record<string, unknown>)['id'] != null
  );
}

export function isJsonRpcNotification(message: unknown): message is JsonRpcNotification {
  return (
    typeof message === 'object' &&
    message !== null &&
    'jsonrpc' in message &&
    'method' in message &&
    !('id' in message && (message as Record<string, unknown>)['id'] != null)
  );
}

export function isJsonRpcSuccess<TResult>(response: JsonRpcResponse<TResult>): boolean {
  return response.error == null;
}
