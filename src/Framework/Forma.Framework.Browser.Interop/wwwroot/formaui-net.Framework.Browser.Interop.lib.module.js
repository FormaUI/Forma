// Brings to life, on a statically rendered page, what a component draws for a script to run or measure: a share menu's
// commands and a stepper's compact form.
import { install as installShareMenus } from './scripts/components/ShareMenu.js';
import { install as installSteppers } from './scripts/components/Stepper.js';

export function afterWebStarted(blazor) {
    installShareMenus(blazor);
    installSteppers(blazor);
}
