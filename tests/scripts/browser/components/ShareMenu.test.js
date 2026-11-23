import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { install, prepare } from '#browser/components/ShareMenu.js';

describe('ShareMenu', () => {
    const originalShare = navigator.share;
    const originalClipboard = navigator.clipboard;
    const originalPrint = window.print;
    const originalSecureContext = window.isSecureContext;

    // A static share sheet: the device's share hidden, Copy Link and Print disabled, as the menu draws them.
    function drawSheet() {
        document.body.innerHTML = `
            <div id="sheet" popover>
                <ul>
                    <li hidden><button type="button" id="share" data-forma-share-command="share" data-forma-share-title="Stage 12" data-forma-share-url="https://example.com/stage-12" disabled>Share</button></li>
                    <li><button type="button" id="copy" data-forma-share-command="copy" data-forma-share-url="https://example.com/stage-12" disabled>Copy Link</button></li>
                    <li><button type="button" id="print" data-forma-share-command="print" disabled>Print</button></li>
                </ul>
            </div>`;
    }

    beforeEach(() => {
        drawSheet();
        Object.defineProperty(window, 'isSecureContext', { value: true, configurable: true });
        Object.defineProperty(navigator, 'clipboard', { value: { writeText: vi.fn(() => Promise.resolve()) }, configurable: true });
        Object.defineProperty(navigator, 'share', { value: undefined, configurable: true });
        window.print = vi.fn();
    });

    afterEach(() => {
        Object.defineProperty(window, 'isSecureContext', { value: originalSecureContext, configurable: true });
        Object.defineProperty(navigator, 'clipboard', { value: originalClipboard, configurable: true });
        Object.defineProperty(navigator, 'share', { value: originalShare, configurable: true });
        window.print = originalPrint;
    });

    it('enables the commands the browser runs and keeps the device\'s share hidden where it offers none', () => {
        prepare(document);

        expect(document.querySelector('#copy').disabled).toBe(false);
        expect(document.querySelector('#print').disabled).toBe(false);
        expect(document.querySelector('#share').closest('li').hidden).toBe(true);
    });

    it('shows the device\'s share where the browser offers one, and hides Copy Link outside a secure context', () => {
        Object.defineProperty(navigator, 'share', { value: vi.fn(() => Promise.resolve()), configurable: true });
        Object.defineProperty(window, 'isSecureContext', { value: false, configurable: true });

        prepare(document);

        expect(document.querySelector('#share').disabled).toBe(false);
        expect(document.querySelector('#share').closest('li').hidden).toBe(false);
        expect(document.querySelector('#copy').closest('li').hidden).toBe(true);
    });

    it('runs the command pressed, closing its sheet first, and answers a sheet an enhanced update replaced', () => {
        const listeners = {};
        install({ addEventListener: (name, listener) => { listeners[name] = listener; } });
        const sheet = document.querySelector('#sheet');
        sheet.matches = selector => selector === ':popover-open';
        sheet.hidePopover = vi.fn();
        document.querySelector('#copy').click();
        drawSheet();
        listeners.enhancedload();

        document.querySelector('#print').click();

        expect(navigator.clipboard.writeText).toHaveBeenCalledWith('https://example.com/stage-12');
        expect(sheet.hidePopover).toHaveBeenCalled();
        expect(window.print).toHaveBeenCalledOnce();
    });
});
