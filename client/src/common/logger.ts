export type LogFunction = (message?: unknown, ...optionalParams: unknown[]) => void;

export interface LoggerOptions {
  enabled?: boolean;
  logFn?: LogFunction;
}

export class Logger {
  private readonly enabled: boolean;
  private readonly logFn: LogFunction;

  constructor(options?: LoggerOptions) {
    this.enabled = options?.enabled ?? false;
    this.logFn = options?.logFn ?? ((...args: unknown[]) => console.log(...args));
  }

  debug(message?: unknown, ...optionalParams: unknown[]): void {
    if (this.enabled) {
      this.logFn(message, ...optionalParams);
    }
  }
}

export const SILENT_LOGGER = new Logger();
