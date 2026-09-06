import { defineConfig } from 'vitest/config';

// Vite matches aliases as forward-slash paths, and a checkout under a path with a space
// arrives percent-encoded, so the URL is decoded rather than converted by fileURLToPath.
const resolved = path => decodeURIComponent(new URL(path, import.meta.url).pathname);

const scripts = {
    core: resolved('../../src/Forma.Framework.Core.Interop/wwwroot/scripts/'),
    common: resolved('../../src/Forma.Framework.Common.Interop/wwwroot/scripts/'),
    composer: resolved('../../src/Forma.Framework.Composer.Interop/wwwroot/scripts/'),
    input: resolved('../../src/Forma.Framework.Input.Interop/wwwroot/scripts/'),
};

// One alias per assembly that ships scripts, each pointing at that assembly's scripts root. The
// assemblies are served side by side under _content/forma/, so a script reaches another assembly
// by climbing to that root, `../../../core/scripts/Frame.js`; here each such climb is resolved to
// the assembly's source folder instead.
export default defineConfig({
    resolve: {
        alias: [
            ...Object.entries(scripts).map(([name, root]) => ({ find: `#${name}/`, replacement: root })),
            ...Object.entries(scripts).map(([name, root]) => ({ find: new RegExp(`^(?:\\.\\./)+${name}/scripts/(.+)$`), replacement: `${root}$1` })),
            { find: '#support/', replacement: resolved('./support/') },
        ],
    },
    test: {
        environment: 'happy-dom',
        include: ['**/*.test.js'],
    },
});
