import { defineConfig } from 'vitest/config';

// Vite matches aliases as forward-slash paths, and a checkout under a path with a space
// arrives percent-encoded, so the URL is decoded rather than converted by fileURLToPath.
const resolved = path => decodeURIComponent(new URL(path, import.meta.url).pathname);

// The scripts root of the interop assembly named for a concept, under the Framework family folder.
const getInteropScriptsPath = name => resolved(`../../src/Forma.Framework/Forma.Framework.${name}.Interop/wwwroot/scripts/`);

const scripts = {
    core: getInteropScriptsPath('JavaScript'),
    common: getInteropScriptsPath('Common'),
    composer: getInteropScriptsPath('Composer'),
    input: getInteropScriptsPath('Input'),
};

// One alias per assembly that ships scripts, named for the root it is served under and pointing at its scripts root. The
// assemblies are served side by side under _content/forma/, so a script reaches another assembly
// by climbing to that root, `../../../core/scripts/FrameScheduler.js`; here each such climb is resolved to
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
