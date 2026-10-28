import { describe, it, expect, afterEach, vi } from 'vitest';

import { write } from '#culture-router/CultureCookie.js';

describe('CultureCookie', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        document.cookie = 'forma-culture-route=; path=/; max-age=0';
    });

    describe('write', () => {
        it('writes the cookie at its path for its lifetime, sent with top-level navigations', () => {
            // Arrange
            const cookie = vi.spyOn(document, 'cookie', 'set');

            // Act
            write('forma-culture-route%2Fapp', 'he', '/app', 31536000, false);

            // Assert
            expect(cookie).toHaveBeenCalledExactlyOnceWith('forma-culture-route%2Fapp=he; path=/app; max-age=31536000; samesite=lax');
        });

        it('marks the cookie secure where the application is served over HTTPS', () => {
            // Arrange
            const cookie = vi.spyOn(document, 'cookie', 'set');

            // Act
            write('forma-culture-route', 'he', '/', 31536000, true);

            // Assert
            expect(cookie).toHaveBeenCalledExactlyOnceWith('forma-culture-route=he; path=/; max-age=31536000; samesite=lax; secure');
        });

        it('escapes the token as a request reads it', () => {
            // Arrange
            const cookie = vi.spyOn(document, 'cookie', 'set');

            // Act
            write('forma-culture-route', 'es 419', '/', 31536000, false);

            // Assert
            expect(cookie).toHaveBeenCalledExactlyOnceWith('forma-culture-route=es%20419; path=/; max-age=31536000; samesite=lax');
        });

        it('leaves the browser holding the token', () => {
            // Act
            write('forma-culture-route', 'he', '/', 31536000, false);

            // Assert
            expect(document.cookie).toContain('forma-culture-route=he');
        });
    });
});
