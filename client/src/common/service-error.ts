import { catchError } from 'rxjs';

export class ServiceError extends Error {
  constructor(message: string, cause?: unknown) {
    super(message);
    this.name = 'ServiceError';
    this.cause = cause;
  }

  get originalCause(): unknown {
    return this.cause;
  }
}

export function wrapServiceError<T>(context: string) {
  return catchError<T, never>((err: unknown) => {
    throw new ServiceError(context, err);
  });
}
