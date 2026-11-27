/**
 * A link to a theme's stylesheet naming the theme `name` and its `schemes`, holding the sheet the browser leaves for a
 * stylesheet in the state `state` names: `loaded` with its rules, `failed` without any, `refused` with rules script can't
 * read, and `foreign`, another origin's read without CORS, the same unreadable rules at an address of that origin.
 */
export function linkThemeStylesheet(name, schemes, state = 'loaded') {
    const link = document.createElement('link');
    link.setAttribute('rel', 'stylesheet');
    link.setAttribute('href', state === 'foreign' ? `https://cdn.example.test/${name}.css` : `/css/${name}.css`);
    link.setAttribute('data-forma-theme-stylesheet', name);
    link.setAttribute('data-forma-theme-schemes', schemes);
    Object.defineProperty(link, 'sheet', { value: createSheet(state) });

    return link;
}

function createSheet(state) {
    if (state === 'failed') {
        return { cssRules: [] };
    }

    if (state === 'refused' || state === 'foreign') {
        return {
            get cssRules() {
                throw new DOMException('The stylesheet\'s rules can\'t be read.', 'SecurityError');
            },
        };
    }

    return { cssRules: [{}] };
}
