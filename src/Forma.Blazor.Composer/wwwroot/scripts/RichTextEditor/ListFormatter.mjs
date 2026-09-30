/**
 * ListFormatter — building, dissolving, and nesting lists, and indenting whatever is not one.
 */

export class ListFormatter {
    /** How far one indent step moves a non-list block. */
    static #IndentStep = 40;

    #canvas;

    constructor(canvas) {
        this.#canvas = canvas;
    }

    /** Toggles the touched blocks into or out of a list of the given kind. */
    toggle(ordered) {
        const tag = ordered ? 'OL' : 'UL';
        const blocks = this.#canvas.selectedBlocks();

        if (blocks.length === 0) {
            return;
        }

        const allInKind = blocks.every(block => block.tagName === 'LI' && block.parentElement?.tagName === tag);

        if (allInKind) {
            const last = this.#dissolveItems(blocks);
            if (last) {
                this.#canvas.placeCaretAtEnd(last);
            }
            return;
        }

        let last = null;
        for (const group of ListFormatter.#groupConsecutive(blocks)) {
            last = this.#convertGroup(group, tag) ?? last;
        }
        if (last) {
            this.#canvas.placeCaretAtEnd(last);
        }
    }

    /** Nests each touched list item under its predecessor, or pushes any other block one step inward. */
    indent() {
        let moved = null;

        for (const block of this.#canvas.selectedBlocks()) {
            if (block.tagName === 'LI') {
                moved = this.#nest(block) ?? moved;
            } else {
                ListFormatter.#stepMargin(block, ListFormatter.#IndentStep);
            }
        }

        // Moving a node collapses any live range that pointed into it, so the caret is placed back inside.
        if (moved) {
            this.#canvas.placeCaretAtEnd(moved);
        }
    }

    /** Lifts each touched list item out one level, or pulls any other block one step back. */
    outdent() {
        let moved = null;

        for (const block of this.#canvas.selectedBlocks()) {
            if (block.tagName === 'LI') {
                moved = this.#lift(block) ?? moved;
            } else {
                ListFormatter.#stepMargin(block, -ListFormatter.#IndentStep);
            }
        }

        if (moved) {
            this.#canvas.placeCaretAtEnd(moved);
        }
    }

    /** Splits blocks into runs of adjacent siblings, so each run becomes one list rather than one list per block. */
    static #groupConsecutive(blocks) {
        const groups = [];
        let current = [];

        for (const block of blocks) {
            if (current.length > 0 && block.previousElementSibling === current[current.length - 1]) {
                current.push(block);
            } else {
                if (current.length > 0) {
                    groups.push(current);
                }
                current = [block];
            }
        }

        if (current.length > 0) {
            groups.push(current);
        }

        return groups;
    }

    /** Turns one run of adjacent blocks into a list of the given kind, re-typing items already in another kind. */
    #convertGroup(group, tag) {
        const first = group[0];

        // Items already in a list of the other kind: re-type the whole list when the run covers it,
        // otherwise carve the run out into its own list of the wanted kind.
        if (first.tagName === 'LI') {
            const source = first.parentElement;

            if (group.length === source.children.length) {
                const replacement = document.createElement(tag);
                while (source.firstChild) {
                    replacement.appendChild(source.firstChild);
                }
                source.replaceWith(replacement);
                return replacement.lastElementChild;
            }

            const carved = document.createElement(tag);
            const trailing = [...source.children].slice([...source.children].indexOf(group[group.length - 1]) + 1);

            source.after(carved);
            for (const item of group) {
                carved.appendChild(item);
            }

            if (trailing.length > 0) {
                const rest = source.cloneNode(false);
                for (const item of trailing) {
                    rest.appendChild(item);
                }
                carved.after(rest);
            }

            if (source.children.length === 0) {
                source.remove();
            }

            return carved.lastElementChild;
        }

        const list = document.createElement(tag);
        first.before(list);

        for (const block of group) {
            const item = document.createElement('li');
            list.appendChild(item);

            // A paragraph dissolves into its item; a heading or quote keeps its identity inside one.
            if (block.tagName === 'P' || block.tagName === 'DIV') {
                while (block.firstChild) {
                    item.appendChild(block.firstChild);
                }
                block.remove();
            } else {
                item.appendChild(block);
            }
        }

        return list.lastElementChild;
    }

    /** Dissolves list items back into paragraphs, splitting their list around the gap they leave. */
    #dissolveItems(items) {
        const byList = new Map();
        for (const item of items) {
            const list = item.parentElement;
            if (!byList.has(list)) {
                byList.set(list, []);
            }
            byList.get(list).push(item);
        }

        let firstBlock = null;
        let lastBlock = null;

        for (const [list, selected] of byList) {
            const children = [...list.children];
            const trailing = children.slice(children.indexOf(selected[selected.length - 1]) + 1);
            let anchor = list;

            for (const item of selected) {
                const paragraph = document.createElement('p');
                while (item.firstChild) {
                    paragraph.appendChild(item.firstChild);
                }
                item.remove();

                // A nested list cannot live inside a paragraph, so it steps out right after it.
                anchor.after(paragraph);
                anchor = paragraph;
                for (const sublist of [...paragraph.querySelectorAll('ul, ol')]) {
                    anchor.after(sublist);
                    anchor = sublist;
                }

                firstBlock ??= paragraph;
                lastBlock = paragraph;
            }

            if (trailing.length > 0) {
                const rest = list.cloneNode(false);
                for (const item of trailing) {
                    rest.appendChild(item);
                }
                anchor.after(rest);
            }

            if (list.children.length === 0) {
                list.remove();
            }
        }

        return lastBlock;
    }

    /** Moves an item into a sublist under the item before it, the way outlines nest. */
    #nest(item) {
        const previous = item.previousElementSibling;
        if (!previous || previous.tagName !== 'LI') {
            return null;
        }

        const kind = item.parentElement.tagName;
        let sublist = previous.lastElementChild;

        if (!sublist || sublist.tagName !== kind) {
            sublist = document.createElement(kind);
            previous.appendChild(sublist);
        }

        sublist.appendChild(item);

        return item;
    }

    /** Moves an item up beside the item its list nests under, or out of the list entirely at the top level. */
    #lift(item) {
        const list = item.parentElement;
        const holder = list.parentElement;

        if (holder && holder.tagName === 'LI') {
            // Items after the lifted one would otherwise read above it; they nest under it instead.
            const followers = [];
            for (let sibling = item.nextElementSibling; sibling; sibling = sibling.nextElementSibling) {
                followers.push(sibling);
            }

            holder.after(item);

            if (followers.length > 0) {
                const sublist = list.cloneNode(false);
                for (const follower of followers) {
                    sublist.appendChild(follower);
                }
                item.appendChild(sublist);
            }

            if (list.children.length === 0) {
                list.remove();
            }
            return item;
        }

        return this.#dissolveItems([item]);
    }

    /** Adjusts a block's reading-direction-aware indentation, dropping the style entirely at zero. */
    static #stepMargin(block, step) {
        const current = Number.parseFloat(block.style.marginInlineStart) || 0;
        const next = Math.max(0, current + step);

        if (next === 0) {
            block.style.removeProperty('margin-inline-start');
        } else {
            block.style.marginInlineStart = `${next}px`;
        }
    }
}
