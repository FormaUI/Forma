import { describe, it, expect, beforeEach } from 'vitest';
import { CapabilityCounts } from '#browser/CapabilityCounts.mjs';

describe('CapabilityCounts', () => {
    /** @type {CapabilityCounts} */
    let counts;
    /** @type {HTMLElement} */
    let target;

    beforeEach(() => {
        counts = new CapabilityCounts();
        target = document.createElement('div');
    });

    it('lists each capability established on an element once, in the order first established', () => {
        counts.add(target, 'focus');
        counts.add(target, 'scroll');
        counts.add(target, 'focus');

        expect(counts.capabilitiesOf(target)).toEqual(['focus', 'scroll']);
    });

    it('keeps a capability until every establishment of it is removed', () => {
        counts.add(target, 'focus');
        counts.add(target, 'focus');

        counts.remove(target, 'focus');
        const remaining = counts.capabilitiesOf(target);
        counts.remove(target, 'focus');

        expect(remaining).toEqual(['focus']);
        expect(counts.capabilitiesOf(target)).toEqual([]);
    });

    it('keeps each element\'s capabilities apart', () => {
        const other = document.createElement('div');

        counts.add(target, 'focus');

        expect(counts.capabilitiesOf(other)).toEqual([]);
    });

    it('lists nothing for what is not an element', () => {
        expect(counts.capabilitiesOf(null)).toEqual([]);
        expect(counts.capabilitiesOf(document.createTextNode('text'))).toEqual([]);
    });
});
