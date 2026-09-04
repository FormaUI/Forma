import { defineConfig } from 'vitest/config';

// Vite matches aliases as forward-slash paths, and a checkout under a path with a space
// arrives percent-encoded, so the URL is decoded rather than converted by fileURLToPath.
const resolved = path => decodeURIComponent(new URL(path, import.meta.url).pathname);

// One alias per assembly that ships scripts, each pointing at that assembly's scripts root.
export default defineConfig({
    resolve: {
        alias: {
            '#interop/': resolved('../../src/Forma.Framework.Interop/wwwroot/scripts/'),
            '#composer/': resolved('../../src/Forma.Framework.Composer.Interop/wwwroot/scripts/'),
            '#input/': resolved('../../src/Forma.Framework.Input.Interop/wwwroot/scripts/'),
            '#support/': resolved('./support/'),
        },
    },
    test: {
        environment: 'happy-dom',
        include: ['**/*.test.js'],
    },
});
