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
    toggle(isOrdered) {
        const tag = isOrdered ? 'OL' : 'UL';
        const blocks = this.#canvas.selectedBlocks();

        if (blocks.length === 0) {
            return;
        }

        const allInKind = blocks.every(block => block.tagName === 'LI' && block.parentElement?.tagName === tag);

        if (allInKind) {
            const lastBlock = this.#dissolveItems(blocks);
            if (lastBlock) {
                this.#canvas.placeCaretAtEnd(lastBlock);
            }
            return;
        }

        let lastBlock = null;
        for (const group of ListFormatter.#groupConsecutive(blocks)) {
            lastBlock = this.#convertGroup(group, tag) ?? lastBlock;
        }
        if (lastBlock) {
            this.#canvas.placeCaretAtEnd(lastBlock);
        }
    }

    /** Nests each touched list item under its predecessor, or pushes any other block one step inward. */
    indent() {
        let movedBlock = null;

        for (const block of this.#canvas.selectedBlocks()) {
            if (block.tagName === 'LI') {
                movedBlock = this.#nest(block) ?? movedBlock;
            } else {
                ListFormatter.#stepMargin(block, ListFormatter.#IndentStep);
            }
        }

        // Moving a node collapses any live range that pointed into it, so the caret is placed back inside.
        if (movedBlock) {
            this.#canvas.placeCaretAtEnd(movedBlock);
        }
    }

    /** Lifts each touched list item out one level, or pulls any other block one step back. */
    outdent() {
        let movedBlock = null;

        for (const block of this.#canvas.selectedBlocks()) {
            if (block.tagName === 'LI') {
                movedBlock = this.#lift(block) ?? movedBlock;
            } else {
                ListFormatter.#stepMargin(block, -ListFormatter.#IndentStep);
            }
        }

        if (movedBlock) {
            this.#canvas.placeCaretAtEnd(movedBlock);
        }
    }

    /** Splits blocks into runs of adjacent siblings, each run becoming one list. */
    static #groupConsecutive(blocks) {
        const groups = [];
        let currentGroup = [];

        for (const block of blocks) {
            if (currentGroup.length > 0 && block.previousElementSibling === currentGroup[currentGroup.length - 1]) {
                currentGroup.push(block);
            } else {
                if (currentGroup.length > 0) {
                    groups.push(currentGroup);
                }
                currentGroup = [block];
            }
        }

        if (currentGroup.length > 0) {
            groups.push(currentGroup);
        }

        return groups;
    }

    /** Turns one run of adjacent blocks into a list of the given kind, re-typing items already in another kind. */
    #convertGroup(group, tag) {
        const firstBlock = group[0];

        // Items already in a list of the other kind: re-type the whole list when the run covers it,
        // otherwise carve the run out into its own list of the wanted kind.
        if (firstBlock.tagName === 'LI') {
            const source = firstBlock.parentElement;

            if (group.length === source.children.length) {
                const replacement = document.createElement(tag);
                while (source.firstChild) {
                    replacement.appendChild(source.firstChild);
                }
                source.replaceWith(replacement);
                return replacement.lastElementChild;
            }

            const carvedList = document.createElement(tag);
            const trailingItems = [...source.children].slice([...source.children].indexOf(group[group.length - 1]) + 1);

            source.after(carvedList);
            for (const item of group) {
                carvedList.appendChild(item);
            }

            if (trailingItems.length > 0) {
                const restList = source.cloneNode(false);
                for (const item of trailingItems) {
                    restList.appendChild(item);
                }
                carvedList.after(restList);
            }

            if (source.children.length === 0) {
                source.remove();
            }

            return carvedList.lastElementChild;
        }

        const list = document.createElement(tag);
        firstBlock.before(list);

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

        for (const [list, selectedItems] of byList) {
            const children = [...list.children];
            const trailingItems = children.slice(children.indexOf(selectedItems[selectedItems.length - 1]) + 1);
            let anchor = list;

            for (const item of selectedItems) {
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

            if (trailingItems.length > 0) {
                const restList = list.cloneNode(false);
                for (const item of trailingItems) {
                    restList.appendChild(item);
                }
                anchor.after(restList);
            }

            if (list.children.length === 0) {
                list.remove();
            }
        }

        return lastBlock;
    }

    /** Moves an item into a sublist under the item before it. */
    #nest(item) {
        const previousItem = item.previousElementSibling;
        if (!previousItem || previousItem.tagName !== 'LI') {
            return null;
        }

        const kind = item.parentElement.tagName;
        let sublist = previousItem.lastElementChild;

        if (!sublist || sublist.tagName !== kind) {
            sublist = document.createElement(kind);
            previousItem.appendChild(sublist);
        }

        sublist.appendChild(item);

        return item;
    }

    /** Moves an item up beside the item its list nests under, or out of the list entirely at the top level. */
    #lift(item) {
        const list = item.parentElement;
        const holder = list.parentElement;

        if (holder && holder.tagName === 'LI') {
            // Items after the lifted one nest under it.
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
        const currentIndent = Number.parseFloat(block.style.marginInlineStart) || 0;
        const nextIndent = Math.max(0, currentIndent + step);

        if (nextIndent === 0) {
            block.style.removeProperty('margin-inline-start');
        } else {
            block.style.marginInlineStart = `${nextIndent}px`;
        }
    }
}
