import { defineConfig } from 'vitest/config';

// A path in the forward slashes Vite matches aliases by, decoded from its URL so a space in the checkout path resolves.
const resolved = path => decodeURIComponent(new URL(path, import.meta.url).pathname);

// The scripts root of the interop assembly named for a concept, under the Framework family folder.
const getInteropScriptsPath = name => resolved(`../../src/Forma.Framework/Forma.Framework.${name}.Interop/wwwroot/scripts/`);

// The scripts root of a product that ships scripts of its own.
const getProductScriptsPath = name => resolved(`../../src/Forma.Blazor.${name}/wwwroot/scripts/`);

const scripts = {
    core: getInteropScriptsPath('JavaScript'),
    browser: getInteropScriptsPath('Browser'),
    input: getInteropScriptsPath('Input'),
    globalization: getInteropScriptsPath('Globalization'),
    composer: getProductScriptsPath('Composer'),
    dataview: getProductScriptsPath('DataView'),
    explorer: getProductScriptsPath('Explorer'),
    showcase: getProductScriptsPath('Showcase'),
};

// One alias per assembly that ships scripts, pointing at its scripts root, and one per assembly resolving a
// script's climb to it under _content/ to its source folder: `../../../core/scripts/FrameScheduler.js`
// between interop assemblies, `../../forma/core/scripts/FrameScheduler.js` from a product.
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
