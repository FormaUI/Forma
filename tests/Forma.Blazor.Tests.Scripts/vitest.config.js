import { defineConfig } from 'vitest/config';

// Vite matches aliases as forward-slash paths, and a checkout under a path with a space
// arrives percent-encoded, so the URL is decoded rather than converted by fileURLToPath.
const resolved = path => decodeURIComponent(new URL(path, import.meta.url).pathname);

// The scripts root of the interop assembly named for a concept, under the Framework family folder.
const getInteropScriptsPath = name => resolved(`../../src/Forma.Framework/Forma.Framework.${name}.Interop/wwwroot/scripts/`);

// The scripts root of a product that ships scripts of its own.
const getProductScriptsPath = name => resolved(`../../src/Forma.Blazor.${name}/wwwroot/scripts/`);

const scripts = {
    core: getInteropScriptsPath('JavaScript'),
    browser: getInteropScriptsPath('Browser'),
    input: getInteropScriptsPath('Input'),
    composer: getProductScriptsPath('Composer'),
    dataview: getProductScriptsPath('DataView'),
    explorer: getProductScriptsPath('Explorer'),
};

// One alias per assembly that ships scripts, pointing at its scripts root. Every assembly is served under
// _content/, so a script reaches another assembly by climbing to it, `../../../core/scripts/FrameScheduler.js`
// between interop assemblies, `../../forma/core/scripts/FrameScheduler.js` from a product; here each such
// climb is resolved to the assembly's source folder instead.
export default defineConfig({
    resolve: {
        alias: [
            ...Object.entries(scripts).map(([name, root]) => ({ find: `#${name}/`, replacement: root })),
            ...Object.entries(scripts).map(([name, root]) => ({ find: new RegExp(`^(?:\\.\\./)+(?:forma/)?${name}/scripts/(.+)$`), replacement: `${root}$1` })),
            { find: '#support/', replacement: resolved('./support/') },
        ],
    },
    test: {
        environment: 'happy-dom',
        include: ['**/*.test.js'],
    },
});
