// Brings to life, on a statically rendered page, what a component draws for a script to run or measure: a share menu's
// commands and a stepper's compact form; and shows a control's tooltip on every page, in a web view's too.
import { install as installShareMenus } from './scripts/components/ShareMenu.js';
import { install as installSteppers } from './scripts/components/Stepper.js';

export function afterWebStarted(blazor) {
    installShareMenus(blazor);
    installSteppers(blazor);
    installTooltips();
}

// A web view starts Blazor through the initializers that came before the Web App's, and renders every page interactively.
export function afterStarted() {
    installTooltips();
}

// Loaded apart, so a host that can't serve a module the tooltip's script reaches still starts Blazor, the failure reported;
// and as the initializer loads, while Blazor starts, so the script is installed as Blazor starts, ready for a first tap.
const tooltips = import('./scripts/components/Tooltip.js');

function installTooltips() {
    tooltips.then(module => module.install());
}
