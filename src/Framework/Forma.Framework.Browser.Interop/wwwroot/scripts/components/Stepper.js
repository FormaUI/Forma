/**
 * Stepper — a statically rendered stepper's compact form
 *
 * A stepper rendered statically has no renderer to measure whether its names fit, so it marks itself, and this measures
 * it as its component does: a stepper whose ruler, every name at its full width, reaches past its list is compact, its
 * steps not current shown by their numbers alone. Each marked stepper is measured as it is found and as its list or
 * its ruler resizes, and steppers are found again after each enhanced update. A stepper its renderer takes over drops
 * the mark, and with it this measuring.
 */

const StaticSelector = '.forma-stepper[data-forma-static-stepper]';

const measuredSteppers = new WeakSet();

let resizeObserver = null;

/**
 * Measures the document's statically rendered steppers, now and after each enhanced update the Blazor object reports.
 * @param {{ addEventListener?: (name: string, listener: () => void) => void } | null} blazor
 */
export function install(blazor = null) {
    if (resizeObserver !== null) {
        return;
    }

    resizeObserver = new ResizeObserver(entries => {
        for (const entry of entries) {
            const stepper = entry.target.closest('.forma-stepper');

            if (stepper !== null) {
                measure(stepper);
            }
        }
    });

    observe(document);
    blazor?.addEventListener?.('enhancedload', () => observe(document));
}

/**
 * Starts measuring each statically rendered stepper under the root it isn't measuring yet, and measures it at once.
 * @param {ParentNode} root
 */
export function observe(root) {
    for (const stepper of root.querySelectorAll(StaticSelector)) {
        if (!measuredSteppers.has(stepper)) {
            measuredSteppers.add(stepper);

            for (const part of stepper.querySelectorAll('.forma-stepper-list, .forma-stepper-ruler-steps')) {
                resizeObserver?.observe(part);
            }
        }

        measure(stepper);
    }
}

/**
 * Marks the stepper compact where its ruler reaches past its list, as long as it carries the static mark.
 * @param {Element} stepper
 */
export function measure(stepper) {
    if (!stepper.hasAttribute('data-forma-static-stepper')) {
        for (const part of stepper.querySelectorAll('.forma-stepper-list, .forma-stepper-ruler-steps')) {
            resizeObserver?.unobserve(part);
        }

        measuredSteppers.delete(stepper);

        return;
    }

    const list = stepper.querySelector('.forma-stepper-list');
    const ruler = stepper.querySelector('.forma-stepper-ruler-steps');

    if (list !== null && ruler !== null) {
        stepper.classList.toggle('is-compact', ruler.getBoundingClientRect().width > list.getBoundingClientRect().width);
    }
}
