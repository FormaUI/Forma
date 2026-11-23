import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { install, measure } from '#browser/components/Stepper.js';
import { stubResizeObserver } from '#support/platform.js';

describe('Stepper', () => {
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;

    // A stepper as its component draws it, its list and its ruler each the width given.
    function drawStepper(listWidth, rulerWidth, isStatic = true) {
        document.body.innerHTML = `
            <div class="forma-stepper" ${isStatic ? 'data-forma-static-stepper' : ''}>
                <ol class="forma-stepper-list"></ol>
                <div class="forma-stepper-ruler"><div class="forma-stepper-ruler-steps"></div></div>
            </div>`;
        document.querySelector('.forma-stepper-list').getBoundingClientRect = () => ({ width: listWidth });
        document.querySelector('.forma-stepper-ruler-steps').getBoundingClientRect = () => ({ width: rulerWidth });

        return document.querySelector('.forma-stepper');
    }

    beforeEach(() => {
        resizes = stubResizeObserver();
    });

    afterEach(() => {
        resizes.restore();
    });

    it('marks a static stepper compact where its names reach past its list, and full again where they fit', () => {
        const stepper = drawStepper(360, 520);
        install();
        const isCompactAtFirst = stepper.classList.contains('is-compact');
        stepper.querySelector('.forma-stepper-list').getBoundingClientRect = () => ({ width: 800 });

        resizes.latest().deliver([{ target: stepper.querySelector('.forma-stepper-list') }]);

        expect(isCompactAtFirst).toBe(true);
        expect(stepper.classList.contains('is-compact')).toBe(false);
    });

    it('leaves a stepper its renderer draws to the renderer', () => {
        const stepper = drawStepper(360, 520, false);

        measure(stepper);

        expect(stepper.classList.contains('is-compact')).toBe(false);
    });
});
