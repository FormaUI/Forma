import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { FocusInteraction } from '#browser/focus/FocusInteraction.mjs';
import { FocusSurface } from '#browser/focus/FocusSurface.mjs';
import { configure, resolve } from '#input/InputRouter.js';

function tab(shiftKey = false) {
    const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey, bubbles: true, cancelable: true });
    document.activeElement.dispatchEvent(event);

    return event;
}

const focus = id => document.querySelector(`#${id}`).focus();

// The interactions attached in a test, whose registrations the router holds until they are disposed after it.
const handles = [];
const attachSurface = (target, options) => {
    const interaction = new FocusInteraction(target, options);
    handles.push(interaction);

    return interaction;
};

afterEach(() => {
    handles.splice(0).forEach(handle => handle.dispose());
    configure({ bindings: {} });
});

describe('FocusInteraction wrapping inside a surface', () => {
    beforeEach(() => {
        document.body.innerHTML = '<div id="surface"><button id="one">One</button><button id="two" disabled>Two</button><button id="three">Three</button></div><button id="after">After</button>';
    });

    const start = () => attachSurface(document.querySelector('#surface'), { wrap: true });

    it('moves Tab to the next enabled control and wraps at the end', () => {
        start();
        focus('one');

        const first = tab();
        expect(document.activeElement.id).toBe('three');

        tab();
        expect(document.activeElement.id).toBe('one');
        expect(first.defaultPrevented).toBe(true);
    });

    it('moves Shift+Tab back and wraps at the start', () => {
        start();
        focus('one');

        tab(true);

        expect(document.activeElement.id).toBe('three');
    });

    it('declares Tab as a route on the input router', () => {
        start();
        focus('one');

        const event = tab();

        expect(resolve(event)).toMatchObject({ owner: document.querySelector('#surface'), action: 'focus.next', precedence: 'structural' });
    });

    it('takes the host\'s remap of a move', () => {
        configure({ bindings: { 'focus.next': [{ type: 'keydown', key: 'j' }] } });
        start();
        focus('one');

        const remapped = new KeyboardEvent('keydown', { key: 'j', bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(remapped);
        expect(document.activeElement.id).toBe('three');

        const unbound = tab();
        expect(unbound.defaultPrevented).toBe(false);
    });

    it('leaves Tab with a modifier other than Shift alone', () => {
        start();
        focus('one');

        const event = new KeyboardEvent('keydown', { key: 'Tab', ctrlKey: true, bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(false);
        expect(document.activeElement.id).toBe('one');
    });

    it('leaves a key other than Tab alone', () => {
        start();
        focus('one');

        const event = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(false);
        expect(document.activeElement.id).toBe('one');
    });

    it('stops once disposed', () => {
        const handle = start();
        focus('one');
        handle.dispose();

        const event = tab();

        expect(event.defaultPrevented).toBe(false);
    });
});

describe('FocusInteraction arranged inside a surface', () => {
    beforeEach(() => {
        document.body.innerHTML = `
            <button id="before">Before</button>
            <div id="composer">
                <div class="toolbar"><button id="bold">Bold</button><button id="italic">Italic</button></div>
                <div class="canvas"><div id="editor" tabindex="0"></div><div class="bar" style="display: none"><button id="hidden">Hidden</button></div></div>
                <button id="status">Status</button>
            </div>
            <button id="after">After</button>
        `;
    });

    const start = () => attachSurface(document.querySelector('#composer'), { order: ['.canvas'] });

    it('visits the named region first, then the rest in document order', () => {
        start();
        focus('editor');

        tab();
        expect(document.activeElement.id).toBe('bold');
        tab();
        expect(document.activeElement.id).toBe('italic');
        tab();
        expect(document.activeElement.id).toBe('status');
    });

    it('leaves for the control after the surface past the last one', () => {
        start();
        focus('status');

        const event = tab();

        expect(document.activeElement.id).toBe('after');
        expect(event.defaultPrevented).toBe(true);
    });

    it('leaves for the control before the surface ahead of the first one', () => {
        start();
        focus('editor');

        tab(true);

        expect(document.activeElement.id).toBe('before');
    });

    it('walks back from the rest into the named region', () => {
        start();
        focus('bold');

        tab(true);

        expect(document.activeElement.id).toBe('editor');
    });

    it('skips controls that are not displayed', () => {
        expect(new FocusSurface(document.querySelector('#composer')).controls().map(control => control.id)).toEqual(['bold', 'italic', 'editor', 'status']);
    });

    it('skips a control parked at tabindex -1', () => {
        document.querySelector('#italic').tabIndex = -1;

        expect(new FocusSurface(document.querySelector('#composer')).controls().map(control => control.id)).toEqual(['bold', 'editor', 'status']);
    });

    it('leaves a Tab to a nested surface that routes it', () => {
        document.querySelector('.canvas').insertAdjacentHTML('beforeend', '<div id="bar"><button id="first">First</button><button id="second">Second</button></div>');
        start();
        attachSurface(document.querySelector('#bar'), { wrap: true });
        focus('second');

        const event = tab();

        expect(document.activeElement.id).toBe('first');
        expect(resolve(event).owner).toBe(document.querySelector('#bar'));
    });

    it('leaves Tab to the page past the last control when nothing follows', () => {
        document.querySelector('#after').remove();
        start();
        focus('status');

        const event = tab();

        expect(event.defaultPrevented).toBe(false);
        expect(resolve(event).action).toBeNull();
    });

    it('moves nothing from a remapped key when there is nowhere to go', () => {
        configure({ bindings: { 'focus.next': [{ type: 'keydown', key: 'j' }] } });
        document.querySelector('#after').remove();
        start();
        focus('status');

        const event = new KeyboardEvent('keydown', { key: 'j', bubbles: true, cancelable: true });
        expect(() => document.activeElement.dispatchEvent(event)).not.toThrow();

        expect(document.activeElement.id).toBe('status');
    });

    it('stops arranging once disposed', () => {
        const handle = start();
        focus('editor');
        handle.dispose();

        const event = tab();

        expect(event.defaultPrevented).toBe(false);
    });
});

describe('FocusInteraction roving inside a surface', () => {
    beforeEach(() => {
        document.body.innerHTML = `
            <button id="before">Before</button>
            <div id="toolbar">
                <button id="bold">Bold</button>
                <button id="italic">Italic</button>
                <button id="underline" disabled>Underline</button>
                <span id="custom" tabindex="0">Custom</span>
                <input id="search" type="text">
                <div popover><button id="option">Option</button></div>
            </div>
            <button id="after">After</button>
        `;
    });

    const press = (key, init = {}) => {
        const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...init });
        document.activeElement.dispatchEvent(event);

        return event;
    };

    const tabindexOf = id => document.querySelector(`#${id}`).getAttribute('tabindex');
    const start = (rovingOrientation = 'horizontal') => attachSurface(document.querySelector('#toolbar'), { rovingOrientation });
    const settle = () => new Promise(resolve => setTimeout(resolve));

    it('leaves the first control as the one Tab stop and parks the rest', () => {
        start();

        expect(tabindexOf('bold')).toBe('0');
        expect(tabindexOf('italic')).toBe('-1');
        expect(tabindexOf('custom')).toBe('-1');
        expect(tabindexOf('search')).toBe('-1');
        expect(tabindexOf('option')).toBeNull();
    });

    it('moves the stop with the arrow keys and wraps at either end', () => {
        start();
        focus('bold');

        const first = press('ArrowRight');
        expect(document.activeElement.id).toBe('italic');
        expect(first.defaultPrevented).toBe(true);
        expect(tabindexOf('italic')).toBe('0');
        expect(tabindexOf('bold')).toBe('-1');

        press('ArrowLeft');
        press('ArrowLeft');
        expect(document.activeElement.id).toBe('search');
    });

    it('reaches either end with Home and End', () => {
        start();
        focus('italic');

        press('Home');
        expect(document.activeElement.id).toBe('bold');

        press('End');
        expect(document.activeElement.id).toBe('search');
    });

    it('mirrors the arrow keys on a surface laid out right to left', () => {
        document.querySelector('#toolbar').style.direction = 'rtl';
        start();
        focus('bold');

        press('ArrowLeft');

        expect(document.activeElement.id).toBe('italic');
    });

    it('roves up and down on a vertical surface and leaves the sideways keys alone', () => {
        start('vertical');
        focus('bold');

        const sideways = press('ArrowRight');
        expect(sideways.defaultPrevented).toBe(false);
        expect(document.activeElement.id).toBe('bold');

        press('ArrowDown');
        expect(document.activeElement.id).toBe('italic');
    });

    it('leaves the up and down keys alone on a horizontal surface', () => {
        start();
        focus('bold');

        const event = press('ArrowDown');

        expect(event.defaultPrevented).toBe(false);
        expect(document.activeElement.id).toBe('bold');
    });

    it('leaves a caret key to a text field', () => {
        start();
        focus('search');

        const event = press('ArrowLeft');

        expect(event.defaultPrevented).toBe(false);
        expect(document.activeElement.id).toBe('search');
    });

    it('leaves a key with a modifier alone', () => {
        start();
        focus('bold');

        const event = press('ArrowRight', { ctrlKey: true });

        expect(event.defaultPrevented).toBe(false);
        expect(document.activeElement.id).toBe('bold');
    });

    it('leaves a key pressed inside a popover alone', () => {
        start();
        focus('option');

        const event = press('ArrowRight');

        expect(event.defaultPrevented).toBe(false);
        expect(document.activeElement.id).toBe('option');
    });

    it('moves nothing from a remapped key while none of its controls holds focus', () => {
        configure({ bindings: { 'focus.next': [{ type: 'keydown', key: 'j' }] } });
        start();
        focus('option');

        expect(() => press('j')).not.toThrow();

        expect(document.activeElement.id).toBe('option');
    });

    it('leaves Tab to the page, with the stop the only control it can reach', () => {
        start();
        focus('italic');

        const event = tab();

        expect(event.defaultPrevented).toBe(false);
        expect(new FocusSurface(document.querySelector('#toolbar')).controls().filter(control => !control.closest('[popover]')).map(control => control.id)).toEqual(['italic']);
    });

    it('counts as one stop inside an arranged surface around it', () => {
        document.body.innerHTML = `
            <div id="composer">
                <div id="toolbar"><button id="bold">Bold</button><button id="italic">Italic</button></div>
                <div id="canvas" tabindex="0"></div>
                <button id="status">Status</button>
            </div>
        `;
        attachSurface(document.querySelector('#composer'), { order: ['#canvas'] });
        start();
        focus('bold');

        tab();
        expect(document.activeElement.id).toBe('status');

        tab(true);
        expect(document.activeElement.id).toBe('bold');

        tab(true);
        expect(document.activeElement.id).toBe('canvas');
    });

    it('parks a control added later and keeps the stop where it was', async () => {
        start();
        focus('italic');
        const added = document.createElement('button');
        added.id = 'added';
        document.querySelector('#toolbar').append(added);

        await settle();

        expect(tabindexOf('added')).toBe('-1');
        expect(tabindexOf('italic')).toBe('0');
    });

    it('moves the stop to the first control once the one holding it is gone', async () => {
        start();
        focus('italic');
        document.querySelector('#italic').remove();
        document.querySelector('#before').focus();

        await settle();

        expect(tabindexOf('bold')).toBe('0');
    });

    it('hands every control its own tab stop back once disposed', () => {
        const handle = start();
        focus('bold');

        handle.dispose();
        const event = press('ArrowRight');

        expect(event.defaultPrevented).toBe(false);
        expect(tabindexOf('bold')).toBeNull();
        expect(tabindexOf('italic')).toBeNull();
        expect(tabindexOf('custom')).toBe('0');
    });
});

describe('FocusInteraction handing on from an element that goes away', () => {
    /** @type {HTMLElement} */
    let surface;

    beforeEach(() => {
        document.body.innerHTML = `
            <button id="outside">Outside</button>
            <div id="surface" tabindex="-1">
                <div id="first" tabindex="0">First</div>
                <div id="second" tabindex="-1">Second</div>
            </div>
            <button id="after">After</button>`;
        surface = document.querySelector('#surface');
    });

    // A render's removal and its mark are seen together once the mutation observer has run.
    const settle = () => new Promise(resolve => setTimeout(resolve, 0));
    const start = () => attachSurface(surface, { heir: 'data-heir' });

    it('hands the focus to the marked element when the element holding it goes away', async () => {
        start();
        focus('first');

        document.querySelector('#second').setAttribute('data-heir', '');
        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement?.id).toBe('second');
    });

    it('hands the focus to the surface itself where the surface is marked', async () => {
        start();
        focus('first');

        surface.setAttribute('data-heir', '');
        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement).toBe(surface);
    });

    it('leaves a focus the reader took out of the surface where it went', async () => {
        start();
        focus('first');
        focus('outside');

        document.querySelector('#second').setAttribute('data-heir', '');
        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement?.id).toBe('outside');
    });

    it('hands nothing on while no element is marked', async () => {
        start();
        focus('first');

        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement).toBe(document.body);
    });

    it('hands nothing on while the element holding the focus stays', async () => {
        start();
        focus('first');

        document.querySelector('#second').setAttribute('data-heir', '');
        await settle();

        expect(document.activeElement?.id).toBe('first');
    });

    it('hands nothing on without an heir', async () => {
        attachSurface(surface, { wrap: true });
        focus('first');

        document.querySelector('#second').setAttribute('data-heir', '');
        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement).toBe(document.body);
    });

    it('stops handing on once disposed', async () => {
        start().dispose();
        focus('first');

        document.querySelector('#second').setAttribute('data-heir', '');
        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement).toBe(document.body);
    });

    it('leaves Tab to the page where the surface only hands on', () => {
        start();
        focus('first');

        expect(tab().defaultPrevented).toBe(false);
    });
});
