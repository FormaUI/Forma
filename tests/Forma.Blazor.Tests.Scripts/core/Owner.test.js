import { describe, it, expect, vi } from 'vitest';
import { tell } from '#core/Owner.js';
import { fakeOwner } from '#support/owner.js';

describe('Owner', () => {
    it('passes the report and its arguments to the owner', () => {
        const owner = fakeOwner();

        tell(owner, 'HandleThingAsync', 1, 'two');

        expect(owner.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('HandleThingAsync', 1, 'two');
    });

    it('lets go of the rejection a disposed owner answers with', async () => {
        const owner = { invokeMethodAsync: vi.fn().mockRejectedValue(new Error('There is no tracked object with id 1.')) };

        tell(owner, 'HandleThingAsync');
        await Promise.resolve();

        expect(owner.invokeMethodAsync).toHaveBeenCalledOnce();
    });

    it('tells no one when there is no owner', () => {
        expect(() => tell(null, 'HandleThingAsync')).not.toThrow();
        expect(() => tell(undefined, 'HandleThingAsync')).not.toThrow();
    });
});
