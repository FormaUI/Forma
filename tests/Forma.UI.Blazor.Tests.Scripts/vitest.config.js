import { defineConfig } from 'vitest/config';

// Vite matches aliases as forward-slash paths, and a checkout under a path with a space
// arrives percent-encoded, so the URL is decoded rather than converted by fileURLToPath.
const resolved = path => decodeURIComponent(new URL(path, import.meta.url).pathname);

const scripts = '../../src/Forma.UI.Blazor/wwwroot/scripts/';

export default defineConfig({
    resolve: {
        alias: {
            '#behaviours/': resolved(`${scripts}behaviours/`),
            '#operations/': resolved(`${scripts}operations/`),
            '#components/': resolved(`${scripts}components/`),
            '#support/': resolved('./support/'),
        },
    },
    test: {
        environment: 'happy-dom',
        include: ['**/*.test.js'],
    },
});
