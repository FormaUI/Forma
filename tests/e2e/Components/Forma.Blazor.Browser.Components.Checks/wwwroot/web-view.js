// What a check host's web view page records for the checks, run ahead of Blazor by the page of each check host with a web view.

// What the document saw of its history, for the check to tell a traversal from a navigation the web view makes.
window.formaProbe = { history: [] };
addEventListener('popstate', () => formaProbe.history.push('popstate'));
if (window.navigation) {
    navigation.addEventListener('navigate', event => formaProbe.history.push(`navigate ${event.navigationType}`));
}

// A web view hands a link to another site to the system's browser; the check host records its address in the browser's place,
// once the page handled the click, so a check reads it and never opens the system's browser.
window.formaOpenedExternally = [];
addEventListener('click', event => {
    const link = event.target instanceof Element ? event.target.closest('a[href]') : null;

    if (link !== null && !event.defaultPrevented && new URL(link.href, location.href).origin !== location.origin) {
        event.preventDefault();
        formaOpenedExternally.push(link.href);
    }
});
