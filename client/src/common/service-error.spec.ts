import { of, throwError, firstValueFrom } from 'rxjs';
import { ServiceError, wrapServiceError } from './service-error';

describe('ServiceError', () => {
  it('should set the name to ServiceError', () => {
    const error = new ServiceError('test');
    expect(error.name).toBe('ServiceError');
  });

  it('should set the message', () => {
    const error = new ServiceError('something failed');
    expect(error.message).toBe('something failed');
  });

  it('should preserve the cause', () => {
    const original = new Error('original');
    const error = new ServiceError('wrapped', original);
    expect(error.originalCause).toBe(original);
  });

  it('should allow undefined cause', () => {
    const error = new ServiceError('no cause');
    expect(error.originalCause).toBeUndefined();
  });

  it('should be an instance of Error', () => {
    const error = new ServiceError('test');
    expect(error).toBeInstanceOf(Error);
  });
});

describe('wrapServiceError', () => {
  it('should pass through successful values', async () => {
    const result = await firstValueFrom(of('ok').pipe(wrapServiceError('context')));
    expect(result).toBe('ok');
  });

  it('should wrap errors in a ServiceError with context', async () => {
    const original = new Error('boom');
    const result$ = throwError(() => original).pipe(wrapServiceError('MyService.method() failed'));

    await expect(firstValueFrom(result$)).rejects.toThrow(ServiceError);
    await expect(firstValueFrom(result$)).rejects.toThrow('MyService.method() failed');
  });

  it('should preserve the original error as cause', async () => {
    const original = new Error('original');
    const result$ = throwError(() => original).pipe(wrapServiceError('context'));

    try {
      await firstValueFrom(result$);
    } catch (err) {
      expect(err).toBeInstanceOf(ServiceError);
      expect((err as ServiceError).originalCause).toBe(original);
    }
  });

  it('should handle non-Error causes', async () => {
    const result$ = throwError(() => 'string error').pipe(wrapServiceError('context'));

    try {
      await firstValueFrom(result$);
    } catch (err) {
      expect(err).toBeInstanceOf(ServiceError);
      expect((err as ServiceError).originalCause).toBe('string error');
    }
  });
});
