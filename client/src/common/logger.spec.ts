import { Logger, SILENT_LOGGER } from './logger';

describe('Logger', () => {
  describe('debug', () => {
    it('should call logFn when enabled', () => {
      const logFn = vi.fn();
      const logger = new Logger({ enabled: true, logFn });

      logger.debug('test message', { key: 'value' });

      expect(logFn).toHaveBeenCalledWith('test message', { key: 'value' });
    });

    it('should not call logFn when disabled', () => {
      const logFn = vi.fn();
      const logger = new Logger({ enabled: false, logFn });

      logger.debug('test message');

      expect(logFn).not.toHaveBeenCalled();
    });

    it('should default to disabled', () => {
      const logFn = vi.fn();
      const logger = new Logger({ logFn });

      logger.debug('test message');

      expect(logFn).not.toHaveBeenCalled();
    });

    it('should use console.log as default logFn when enabled', () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});
      const logger = new Logger({ enabled: true });

      logger.debug('hello', 42);

      expect(consoleSpy).toHaveBeenCalledWith('hello', 42);
      consoleSpy.mockRestore();
    });

    it('should handle calls with no arguments', () => {
      const logFn = vi.fn();
      const logger = new Logger({ enabled: true, logFn });

      logger.debug();

      expect(logFn).toHaveBeenCalledWith(undefined);
    });

    it('should handle calls with multiple arguments', () => {
      const logFn = vi.fn();
      const logger = new Logger({ enabled: true, logFn });

      logger.debug('prefix', 'arg1', 'arg2', 123);

      expect(logFn).toHaveBeenCalledWith('prefix', 'arg1', 'arg2', 123);
    });
  });

  describe('SILENT_LOGGER', () => {
    it('should not throw when debug is called', () => {
      expect(() => SILENT_LOGGER.debug('test')).not.toThrow();
    });

    it('should not call console.log', () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

      SILENT_LOGGER.debug('test');

      expect(consoleSpy).not.toHaveBeenCalled();
      consoleSpy.mockRestore();
    });
  });
});
