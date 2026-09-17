/**
 * InputRouter — which component owns an input, and which must defer
 *
 * A component registers routes on a scope — the element it owns — each route naming a
 * semantic action, the bindings that raise it by default, and the handler; the host remaps
 * bindings by action. Several behaviors register on one element and share its scope, and
 * each registration is disposed on its own. The router resolves every input to one owner.
 * Targeted input — a key, a wheel turn, a press, a drop — belongs to the innermost scope on
 * the event's composed path that routes it, and never crosses a boundary outward. Ambient
 * input — Escape, a press outside, a platform close request — belongs to the foremost
 * active registration alone. The router owns the decisions; it installs whichever listener
 * each input class needs and no more.
 */

const isApplePlatform = /Mac|iPhone|iPad|iPod/.test(globalThis.navigator?.platform ?? '');

/** What a consumed input does by default, per input class. */
const ClassDefaults = {
    keydown: { preventDefault: true, propagation: 'stop' },
    keyup: { preventDefault: false, propagation: 'stop' },
    wheel: { preventDefault: true, propagation: 'stop' },
    pointerdown: { preventDefault: false, propagation: 'continue' },
    pointerup: { preventDefault: false, propagation: 'continue' },
    pointermove: { preventDefault: false, propagation: 'continue' },
    drop: { preventDefault: true, propagation: 'continue' },
    dragover: { preventDefault: true, propagation: 'continue' },
};

/** The input family an event type belongs to, deciding how a binding is matched and how the input is described. */
const Families = {
    keydown: 'keyboard',
    keyup: 'keyboard',
    wheel: 'wheel',
    pointerdown: 'pointer',
    pointerup: 'pointer',
    pointermove: 'pointer',
    drop: 'drop',
    dragover: 'drop',
    dragenter: 'drop',
    dragleave: 'drop',
};

/** The fields of a binding that narrow what it matches. */
const BindingFields = ['key', 'axis', 'button', 'buttons', 'pointerType', 'data', 'when'];

/** The modifier keys a binding's `modifiers` can state. */
const ModifierFields = ['control', 'meta', 'alt', 'shift'];

/** How long a click can follow the press away that dismissed a scope and still be its click. */
const ClickSuppressionWindow = 1000;

/** An action identifier: a component or application name, a dot, and the action, so host overrides key one thing. */
const ActionIdentifier = /^[a-z0-9-]+(\.[a-z0-9-]+)+$/;

/** Text-like input types, whose caret the arrow and Home/End keys move. */
const TextInputTypes = new Set(['', 'text', 'search', 'url', 'tel', 'email', 'password', 'number']);

/** Segmented date and time input types, whose segments the arrows move and change. */
const SegmentedInputTypes = new Set(['date', 'time', 'datetime-local', 'month', 'week']);

/** Shortcuts every native editing control answers itself. */
const EditingShortcuts = new Set(['a', 'c', 'v', 'x', 'z', 'y']);

/** Shortcuts an editable region answers itself, beyond those of every editing control. */
const RichEditingShortcuts = new Set(['b', 'i', 'u']);

const HorizontalKeys = new Set(['ArrowLeft', 'ArrowRight', 'Home', 'End']);
const VerticalKeys = new Set(['ArrowUp', 'ArrowDown']);
const PagingKeys = new Set(['PageUp', 'PageDown']);
const EditingKeys = new Set(['Enter', 'Backspace', 'Delete']);

/**
 * @typedef {{
 *   type: string, when?: (event: Event) => boolean,
 *   modifiers?: { control?: boolean, meta?: boolean, alt?: boolean, shift?: boolean, primary?: boolean },
 *   key?: string, axis?: 'vertical' | 'horizontal', button?: number, buttons?: number, pointerType?: 'mouse' | 'pen' | 'touch', data?: string[],
 * }} Binding
 * @typedef {{ event: Event, action: string, owner: Element, binding: Binding }} Routing
 * @typedef {{ action: string, bindings?: Binding[], handle?: (event: Event, routing: Routing) => void, priority?: number, preventDefault?: boolean, propagation?: 'stop' | 'continue' }} Route
 * @typedef {{
 *   modal?: boolean,
 *   escape?: 'dismiss' | 'cancel' | 'ignore',
 *   closeRequest?: 'dismiss' | 'cancel' | 'ignore',
 *   pressAway?: 'dismiss' | 'ignore' | 'pass-through',
 *   boundary?: string[],
 *   parent?: Element,
 *   within?: Element[],
 *   routes?: Route[],
 *   preemptiveRoutes?: Route[],
 *   onDismiss?: (action: string, reason: string) => void,
 * }} Profile
 * @typedef {{ invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }} Owner
 */

const scopes = new Map();
const activeEntries = [];
let policy = { escape: 'dismiss', closeRequest: null, pressAway: 'dismiss', bindings: new Map() };
let registrations = 0;
let activations = 0;
let documentListeners = null;
let suppressedClick = null;
const dispatched = new WeakSet();

/**
 * Sets the host's policy: the ambient defaults a scope inherits, and the bindings that replace,
 * extend, or remove an action's defaults, keyed by action. A `bindings` given replaces the whole
 * set, so an action left out of it returns to its defaults.
 */
export function configure(options = {}) {
    policy = {
        escape: options.escape ?? policy.escape,
        closeRequest: options.closeRequest === undefined ? policy.closeRequest : options.closeRequest,
        pressAway: options.pressAway ?? policy.pressAway,
        bindings: options.bindings == null ? policy.bindings : new Map(Object.entries(options.bindings)),
    };

    for (const scope of scopes.values()) {
        scope.refresh();
    }
}

/**
 * Registers routes and ambient behavior on an element's scope, creating the scope on its first registration.
 * @param {Element} element The scope's root.
 * @param {Profile} profile What this registration contributes.
 * @param {Owner | null} owner
 *   A .NET reference that receives actions no route handles in script, through HandleInputActionAsync, and dismissals through HandleDismissAsync.
 * @param {{ parent?: Element | null, within?: Element[] } | null} placement
 *   The parent and the elements counted as inside, given apart from the profile when the profile is shared between instances.
 */
export function register(element, profile = {}, owner = null, placement = null) {
    // A reference that resolved to nothing names an element already gone, which no scope can stand on.
    if (!(element instanceof Element)) {
        return null;
    }

    // The parent and the elements counted as inside are per instance, so a .NET caller passes them beside its reusable profile.
    const contributed = placement ? { ...profile, ...placement } : profile;
    let scope = scopes.get(element);

    if (!scope) {
        scope = new Scope(element);
        scope.validate(contributed);
        scopes.set(element, scope);
        ensureDocumentListeners();
    }

    return scope.add(contributed, owner);
}

/**
 * Registers the host's own routes on the document: `routes` are resolved after every component scope
 * has declined; `preemptiveRoutes` take the input from every local claim, native controls and modal
 * boundaries included, and only ambient input is resolved before them.
 */
export function registerHost(profile = {}, owner = null) {
    return register(document.documentElement, { routes: profile.routes ?? [], preemptiveRoutes: profile.preemptiveRoutes ?? [] }, owner);
}

/** Resolves an event to its owner without dispatching it. */
export function resolve(event) {
    if (event.type === 'keydown' && isComposing(event)) {
        return unowned(event, 'composition');
    }

    if (event.type === 'keydown' && event.key === 'Escape' && activeEntries.length > 0) {
        return resolveAmbient(event, 'escape');
    }

    if (event.type === 'pointerdown' && activeEntries.length > 0) {
        const foremost = foremostEntry();

        if (foremost && !foremost.contains(event) && foremost.pressAway !== 'ignore') {
            return resolveAmbient(event, 'press-away');
        }
    }

    return resolvePreemptive(event) ?? resolveStructural(event);
}

/**
 * Offers a platform close request raised at the element's active scope to the foremost active registration,
 * which is dismissed as its profile says; the decision says whether the request was forwarded past the element.
 */
export function requestDismiss(element, reason = 'close-request') {
    const scope = scopes.get(element);

    if (!scope) {
        return null;
    }

    if (!scope.isActive) {
        return { ...unowned({ type: reason }, 'source not active'), owner: element, scope };
    }

    const foremost = foremostEntry();
    const action = foremost.closeRequest === 'ignore' ? null : foremost.closeRequest;
    const forwarded = foremost.scope !== scope;

    if (action) {
        foremost.dismiss(action, reason);
    }

    return {
        owner: foremost.element,
        scope: foremost.scope,
        registration: foremost,
        route: null,
        input: { family: 'request', type: reason },
        action,
        handler: null,
        preventDefault: true,
        propagation: 'stop',
        precedence: 'active',
        tie: null,
        reason: forwarded ? `${reason} forwarded to the foremost registration` : reason,
    };
}

/** Whether the element's scope is the resolved owner of the event. */
export function owns(element, event) {
    return resolve(event).owner === element;
}

/** The element whose scope owns the event, or null. */
export function ownerOf(event) {
    return resolve(event).owner;
}

/** A scope's routes with their effective bindings, its active registrations, and its place in the active ordering. */
export function describe(element) {
    const scope = scopes.get(element);

    if (!scope) {
        return null;
    }

    const ordered = orderedActive();
    const foremost = ordered.at(-1) ?? null;
    const layers = ordered.map(entry => entry.scope);

    return {
        boundary: [...scope.boundary],
        parent: scope.parent?.element ?? null,
        registrations: scope.registrations.length,
        routes: scope.routes.map(describeRoute),
        preemptiveRoutes: scope.preemptiveRoutes.map(describeRoute),
        conflicts: scope.conflicts(),
        active: scope.isActive,
        layer: layers.includes(scope) ? layers.lastIndexOf(scope) + 1 : null,
        foremost: foremost?.scope === scope,
        entries: scope.registrations.filter(entry => entry.activation !== null).map(entry => ({
            activation: entry.activation,
            modal: entry.modal,
            escape: { value: entry.escape, source: entry.profile.escape != null ? 'registration' : 'host' },
            closeRequest: { value: entry.closeRequest, source: entry.profile.closeRequest != null ? 'registration' : policy.closeRequest != null ? 'host' : 'escape' },
            pressAway: { value: entry.pressAway, source: entry.profile.pressAway != null ? 'registration' : 'host' },
            parent: entry.parentScope?.element ?? null,
            foremost: entry === foremost,
        })),
    };
}

/** The active ordering, foremost last, with the rule that placed each registration above the one before. */
export function stack() {
    const ordered = orderedActive();
    const chains = ancestorChains(ordered);
    const barrier = activeEntries.filter(entry => entry.modal).at(-1) ?? null;

    return ordered.map((entry, index) => ({
        element: entry.element,
        layer: index + 1,
        modal: entry.modal,
        parent: entry.parentScope?.element ?? null,
        activation: entry.activation,
        reason: placement(entry, ordered[index - 1], barrier, ordered, chains),
    }));
}

function placement(entry, below, barrier, ordered, chains) {
    if (!below) {
        return entry === barrier ? 'modal barrier' : 'first eligible';
    }

    if (depthOf(entry, ordered, chains) > depthOf(below, ordered, chains)) {
        return 'inside its parent';
    }

    return entry.activation > below.activation ? 'activated later' : 'registered later';
}

function describeRoute(route) {
    return {
        action: route.action,
        bindings: route.bindings,
        defaultBindings: route.defaultBindings,
        source: route.source,
        priority: route.priority,
        bindingsSuppression: route.bindings.map(binding => ({ type: binding.type, ...suppressionOf(route, binding) })),
    };
}

class Registration {
    #scope;
    #disposed = false;
    profile;
    owner;
    order;
    activation = null;

    constructor(scope, profile, owner, order) {
        this.#scope = scope;
        this.profile = profile;
        this.owner = owner;
        this.order = order;
    }

    get disposed() {
        return this.#disposed;
    }

    get scope() {
        return this.#scope;
    }

    get element() {
        return this.#scope.element;
    }

    get active() {
        return this.activation !== null;
    }

    get modal() {
        return this.profile.modal === true;
    }

    get escape() {
        return this.profile.escape ?? policy.escape;
    }

    /** What a platform close request does; unstated, it does what Escape does. */
    get closeRequest() {
        return this.profile.closeRequest ?? policy.closeRequest ?? this.escape;
    }

    get pressAway() {
        return this.profile.pressAway ?? policy.pressAway;
    }

    /** The scope this registration stands inside: the parent it declares, else its scope's. */
    get parentScope() {
        return this.profile.parent != null ? scopes.get(this.profile.parent) ?? null : this.#scope.parent;
    }

    ancestors() {
        const chain = [];

        for (let current = this.parentScope; current && !chain.includes(current); current = current.parent) {
            chain.push(current);
        }

        return chain;
    }

    contains(event) {
        const path = event.composedPath();

        return path.includes(this.element) || (this.profile.within ?? []).some(inside => inside && path.includes(inside));
    }

    // A disposed registration answers no call; it is gone from its scope and cannot come back.
    activate() {
        if (this.activation === null && !this.#disposed) {
            this.activation = activations++;
            activeEntries.push(this);
            refreshLayers();
        }
    }

    release() {
        if (this.activation !== null) {
            this.activation = null;
            activeEntries.splice(activeEntries.indexOf(this), 1);
            refreshLayers();
        }
    }

    update(profile) {
        if (this.#disposed) {
            return;
        }

        const next = { ...this.profile, ...profile };
        this.#scope.validate(next);
        this.profile = next;
        this.#scope.refresh();
    }

    dispose() {
        if (this.#disposed) {
            return;
        }

        this.#disposed = true;
        this.release();
        this.#scope.remove(this);
    }

    dismiss(action, reason) {
        if (this.profile.onDismiss) {
            this.profile.onDismiss(action, reason);
        } else {
            observe(this.owner?.invokeMethodAsync('HandleDismissAsync', action, reason));
        }
    }

    // The handler runs from whichever listener resolved the event, so the owner is passed rather than read from the event.
    run(route, binding, event) {
        if (route.handle) {
            route.handle(event, { event, action: route.action, owner: this.element, binding });
        } else {
            observe(this.owner?.invokeMethodAsync('HandleInputActionAsync', route.action, describeInput(event)));
        }
    }
}

class Scope {
    element;
    registrations = [];
    #controller = new AbortController();
    #routes = [];
    #preemptiveRoutes = [];
    #boundary = new Set();

    constructor(element) {
        this.element = element;
    }

    /** Rejects a profile with an unqualified action, an impossible binding, itself as parent, or a parent that would close a cycle through it. */
    validate(profile) {
        for (const route of [...(profile.routes ?? []), ...(profile.preemptiveRoutes ?? [])]) {
            normalizeRoute(route, null);
        }

        if (profile.parent == null) {
            return;
        }

        if (profile.parent === this.element) {
            throw new Error('An input scope cannot be its own parent.');
        }

        for (let current = scopes.get(profile.parent); current; current = current.parent) {
            if (current === this) {
                throw new Error('An input scope cannot be inside a scope that is inside it.');
            }
        }
    }

    get isActive() {
        return this.registrations.some(registration => registration.activation !== null);
    }

    get isModal() {
        return this.registrations.some(registration => registration.modal);
    }

    /** The input types this scope is a boundary for, gathered from every registration. */
    get boundary() {
        return this.#boundary;
    }

    /** The parent a registration declares, else the nearest registered scope containing this one. */
    get parent() {
        const declared = this.registrations.find(registration => registration.profile.parent != null)?.profile.parent;

        if (declared) {
            return scopes.get(declared) ?? null;
        }

        let nearest = null;

        for (const candidate of scopes.values()) {
            if (candidate !== this && candidate.element.contains(this.element) && (!nearest || nearest.element.contains(candidate.element))) {
                nearest = candidate;
            }
        }

        return nearest;
    }

    get routes() {
        return this.#routes;
    }

    get preemptiveRoutes() {
        return this.#preemptiveRoutes;
    }

    /** Bindings two routes of this scope both match, which registration order alone would decide between. */
    conflicts() {
        const seen = [];
        const conflicts = [];

        for (const route of [...this.#routes, ...this.#preemptiveRoutes]) {
            for (const binding of route.bindings) {
                const rival = seen.find(candidate => sameBinding(candidate.binding, binding) && candidate.route.priority === route.priority);

                if (rival) {
                    conflicts.push({ binding, actions: [rival.route.action, route.action] });
                } else {
                    seen.push({ route, binding });
                }
            }
        }

        return conflicts;
    }

    add(profile, owner) {
        this.validate(profile);
        const registration = new Registration(this, profile, owner, registrations++);
        this.registrations.push(registration);
        this.refresh();

        return registration;
    }

    remove(registration) {
        const position = this.registrations.indexOf(registration);

        if (position < 0) {
            return;
        }

        this.registrations.splice(position, 1);

        if (this.registrations.length > 0) {
            this.refresh();
            return;
        }

        this.#controller.abort();
        scopes.delete(this.element);
        refreshLayers();

        if (scopes.size === 0) {
            documentListeners?.abort();
            documentListeners = null;
        }
    }

    // Keys ride the one document listener; the classes whose default must be prevented at the
    // element, or that must not be passive, listen on the scope itself.
    refresh() {
        this.#routes = this.registrations.flatMap(registration => (registration.profile.routes ?? []).map(route => normalizeRoute(route, registration)));
        this.#preemptiveRoutes = this.registrations.flatMap(registration => (registration.profile.preemptiveRoutes ?? []).map(route => normalizeRoute(route, registration)));
        this.#boundary = new Set(this.registrations.flatMap(registration => registration.profile.boundary ?? []));

        this.#controller.abort();
        this.#controller = new AbortController();
        const { signal } = this.#controller;
        const types = new Set([...this.#routes, ...this.#preemptiveRoutes].flatMap(route => route.bindings.map(binding => binding.type)).filter(type => Families[type] !== 'keyboard'));

        for (const type of types) {
            this.element.addEventListener(type, event => dispatch(event), { signal, capture: true, passive: false });
        }

        refreshLayers();
    }

    /** The best route for the event among the given ones, and the rule that decided between rivals. */
    match(event, routes = this.#routes) {
        const candidates = routes
            .flatMap(route => route.bindings.filter(binding => matches(binding, event)).map(binding => ({ route, binding, specificity: specificity(binding) })))
            .sort((left, right) => right.specificity - left.specificity || right.route.priority - left.route.priority || left.route.registration.order - right.route.registration.order);

        if (candidates.length === 0) {
            return null;
        }

        const [best, rival] = candidates;
        const tie = !rival ? null
            : best.specificity !== rival.specificity ? 'specificity'
            : best.route.priority !== rival.route.priority ? 'priority'
            : 'registration-order';

        return { route: best.route, binding: best.binding, tie };
    }
}

// A route's effective bindings are the host's override for its action when one exists, else its own.
function normalizeRoute(route, registration) {
    if (!ActionIdentifier.test(route.action ?? '')) {
        throw new Error(`The action '${route.action}' must be named '{owner}.{action}', such as 'composer.bold', so a host override keys one thing.`);
    }

    const defaults = route.bindings ?? [];
    const override = policy.bindings.get(route.action);
    const bindings = override === undefined ? defaults : applyOverride(defaults, override);

    for (const binding of bindings) {
        validateModifiers(binding);
    }

    return {
        action: route.action,
        handle: route.handle ?? null,
        registration,
        defaultBindings: defaults,
        bindings,
        source: override === undefined ? 'default' : 'host',
        priority: route.priority ?? 0,
        preventDefault: route.preventDefault ?? null,
        propagation: route.propagation ?? null,
    };
}

// What a route leaves unsaid comes from the class of the binding that matched.
function suppressionOf(route, binding) {
    return {
        preventDefault: route.preventDefault ?? ClassDefaults[binding.type]?.preventDefault ?? false,
        propagation: route.propagation ?? ClassDefaults[binding.type]?.propagation ?? 'continue',
    };
}

function applyOverride(defaults, override) {
    if (override === null) {
        return [];
    }

    if (Array.isArray(override)) {
        return override;
    }

    if (override.replace != null) {
        return override.replace;
    }

    const removed = override.remove ?? [];

    return [...defaults.filter(binding => !removed.some(candidate => sameBinding(candidate, binding))), ...(override.add ?? [])];
}

// `primary` names the platform's shortcut key, so a binding cannot also require that key absent.
function validateModifiers(binding) {
    const modifiers = binding.modifiers ?? {};

    if (modifiers.primary === true && (isApplePlatform ? modifiers.meta === false : modifiers.control === false)) {
        throw new Error('A binding cannot require the primary modifier and require it absent.');
    }
}

function sameBinding(left, right) {
    return left.type === right.type
        && BindingFields.every(field => (left[field] ?? null) === (right[field] ?? null) || (Array.isArray(left[field]) && JSON.stringify(left[field]) === JSON.stringify(right[field])))
        && [...ModifierFields, 'primary'].every(field => (left.modifiers?.[field] ?? null) === (right.modifiers?.[field] ?? null));
}

function ensureDocumentListeners() {
    if (documentListeners) {
        return;
    }

    documentListeners = new AbortController();
    const { signal } = documentListeners;

    document.addEventListener('keydown', event => dispatch(event), { signal, capture: true });
    document.addEventListener('keyup', event => dispatch(event), { signal, capture: true });
    document.addEventListener('pointerdown', event => dispatch(event), { signal, capture: true });
    document.addEventListener('pointercancel', event => forgetSuppressedClick(event.pointerId), { signal, capture: true });
    document.addEventListener('click', event => swallowSuppressedClick(event), { signal, capture: true });
    document.addEventListener('cancel', event => offerCloseRequest(event), { signal, capture: true });
}

// A platform close request does not bubble, so it is heard in the capture phase and offered from the element it was raised at.
function offerCloseRequest(event) {
    const decision = event.target instanceof Element ? requestDismiss(event.target) : null;

    if (decision?.preventDefault) {
        event.preventDefault();
    }
}

// An event reaches the router through more than one listener; the first decides for all of them.
function dispatch(event) {
    if (dispatched.has(event)) {
        return;
    }

    dispatched.add(event);

    if (event.type === 'pointerdown') {
        suppressedClick = null;
    }

    const resolution = resolve(event);
    apply(event, resolution);

    // A press passed through dismisses the foremost registration and then belongs to whatever was pressed.
    if (resolution.reason === 'press-away' && resolution.registration.pressAway === 'pass-through') {
        apply(event, resolvePreemptive(event) ?? resolveStructural(event));
    }
}

function apply(event, resolution) {
    if (resolution.action === null || resolution.precedence === 'native') {
        return;
    }

    if (resolution.preventDefault) {
        event.preventDefault();
    }

    if (resolution.propagation === 'stop') {
        event.stopPropagation();
    }

    if (resolution.route) {
        resolution.registration.run(resolution.route, resolution.binding, event);
        return;
    }

    if (resolution.reason === 'press-away' && resolution.registration.pressAway === 'dismiss' && event.target instanceof Element) {
        suppressedClick = { pointerId: event.pointerId, target: event.target, time: performance.now() };
    }

    resolution.registration.dismiss(resolution.action, resolution.reason);
}

// The click that follows the press is the one from the same pointer, on what was pressed, within the window;
// any other click ends the suppression without being swallowed.
function swallowSuppressedClick(event) {
    if (!suppressedClick) {
        return;
    }

    const { pointerId, target, time } = suppressedClick;
    suppressedClick = null;
    const samePointer = event.pointerId === undefined || event.pointerId === pointerId;

    if (samePointer && target.contains(event.target) && performance.now() - time <= ClickSuppressionWindow) {
        event.stopPropagation();
        event.preventDefault();
    }
}

function forgetSuppressedClick(pointerId) {
    if (suppressedClick?.pointerId === pointerId) {
        suppressedClick = null;
    }
}

function resolvePreemptive(event) {
    const host = scopes.get(document.documentElement);
    const match = host?.match(event, host.preemptiveRoutes);

    return match ? routed(event, host, match, 'preemptive') : null;
}

// The walk ends at the first route that matches, at a native control that consumes the input,
// or at a scope that is a boundary for the input's class; only a matching route yields an action.
// A shadow root on the path is stepped over, so the walk goes on to the host and its scopes.
function resolveStructural(event) {
    for (const node of event.composedPath()) {
        if (!(node instanceof Element)) {
            continue;
        }

        const scope = scopes.get(node);
        const match = scope?.match(event);

        if (match) {
            return routed(event, scope, match, 'structural');
        }

        if (nativeClaims(node, event)) {
            return { ...unowned(event, 'native control'), owner: node, precedence: 'native' };
        }

        if (scope && (scope.isActive || scope.isModal)) {
            return { ...unowned(event, 'active boundary'), owner: node, scope };
        }

        if (scope?.boundary.has(event.type)) {
            return { ...unowned(event, `boundary for ${event.type}`), owner: node, scope };
        }
    }

    return unowned(event, 'no scope routes it');
}

function routed(event, scope, { route, binding, tie }, precedence) {
    return {
        owner: scope.element,
        scope,
        registration: route.registration,
        route,
        binding,
        input: describeInput(event),
        action: route.action,
        handler: route.handle,
        ...suppressionOf(route, binding),
        precedence,
        source: route.source,
        tie,
        reason: tie ? `route ${route.action}, chosen by ${tie}` : `route ${route.action}`,
    };
}

function resolveAmbient(event, reason) {
    const entry = foremostEntry();
    const behavior = reason === 'escape' ? entry.escape : entry.pressAway;
    const action = behavior === 'ignore' ? null : reason === 'escape' ? behavior : 'dismiss';
    const consumes = action !== null && behavior !== 'pass-through';

    return {
        owner: entry.element,
        scope: entry.scope,
        registration: entry,
        route: null,
        binding: null,
        input: describeInput(event),
        action,
        handler: null,
        preventDefault: consumes,
        propagation: consumes ? 'stop' : 'continue',
        precedence: 'active',
        tie: null,
        reason,
    };
}

function unowned(event, reason) {
    return { owner: null, scope: null, registration: null, route: null, binding: null, input: describeInput(event), action: null, handler: null, preventDefault: false, propagation: 'continue', precedence: 'none', tie: null, reason };
}

/**
 * Foremost is decided by the modal barrier, then parentage, then activation order, then
 * registration order. A later rule never overrides an earlier one.
 */
function foremostEntry() {
    return orderedActive().at(-1) ?? null;
}

// Beneath the topmost modal registration, only that registration, the others on its scope, and
// the registrations whose parent chain reaches its scope are eligible.
function orderedActive() {
    const barrier = activeEntries.filter(entry => entry.modal).at(-1) ?? null;
    const chains = ancestorChains(activeEntries);
    const eligible = barrier
        ? activeEntries.filter(entry => entry.scope === barrier.scope || chains.get(entry).includes(barrier.scope))
        : [...activeEntries];

    return eligible.sort((left, right) =>
        rankOf(left, eligible, chains) - rankOf(right, eligible, chains)
        || depthOf(left, eligible, chains) - depthOf(right, eligible, chains)
        || left.activation - right.activation
        || left.order - right.order);
}

/** Each registration's ancestor scopes, walked once for a whole ordering. */
function ancestorChains(entries) {
    return new Map(entries.map(entry => [entry, entry.ancestors()]));
}

// A registration stands no lower than the ancestors it is inside, so a child outranks its own
// parent and nothing else that was activated after it.
function rankOf(entry, among, chains) {
    const ancestors = chains.get(entry);

    return Math.max(entry.activation, ...among.filter(other => ancestors.includes(other.scope)).map(other => other.activation));
}

function depthOf(entry, among, chains) {
    return chains.get(entry).filter(ancestor => among.some(other => other.scope === ancestor)).length;
}

function refreshLayers() {
    const ordered = orderedActive();

    for (const entry of activeEntries) {
        entry.element.removeAttribute('data-forma-layer');
    }

    ordered.forEach((entry, index) => entry.element.setAttribute('data-forma-layer', String(index + 1)));
}

function specificity(binding) {
    const modifiers = binding.modifiers ?? {};

    return BindingFields.filter(field => binding[field] != null).length
        + ModifierFields.filter(field => modifiers[field] != null).length
        + (modifiers.primary === true ? 1 : 0);
}

/** Whether a binding matches the event, by the rules of the event's family. */
function matches(binding, event) {
    if (binding.type !== event.type || (binding.when && !binding.when(event))) {
        return false;
    }

    switch (Families[event.type]) {
        case 'keyboard':
            return (binding.key == null || binding.key.toLowerCase() === event.key.toLowerCase()) && modifiersMatch(binding, event, true);
        case 'wheel':
            return axisMatches(binding.axis, event) && modifiersMatch(binding, event, false);
        case 'pointer':
            return (binding.button == null || binding.button === event.button)
                && (binding.buttons == null || (event.buttons & binding.buttons) === binding.buttons)
                && (binding.pointerType == null || binding.pointerType === event.pointerType)
                && modifiersMatch(binding, event, false);
        case 'drop':
            return binding.data == null || binding.data.some(kind => event.dataTransfer?.types?.includes(kind));
        default:
            return true;
    }
}

// A key binding requires Control, Meta and Alt absent unless stated, and Shift either way unless stated;
// a pointer or wheel binding cares only about the modifiers it states. `primary` stands for Meta on
// Apple platforms and Control elsewhere.
function modifiersMatch(binding, event, strict) {
    const modifiers = binding.modifiers ?? {};
    const primary = modifiers.primary === true;
    const expected = {
        control: modifiers.control ?? (primary && !isApplePlatform ? true : strict ? false : null),
        meta: modifiers.meta ?? (primary && isApplePlatform ? true : strict ? false : null),
        alt: modifiers.alt ?? (strict ? false : null),
        shift: modifiers.shift ?? null,
    };

    return (expected.control === null || expected.control === event.ctrlKey)
        && (expected.meta === null || expected.meta === event.metaKey)
        && (expected.alt === null || expected.alt === event.altKey)
        && (expected.shift === null || expected.shift === event.shiftKey);
}

function axisMatches(axis, event) {
    switch (axis) {
        case 'vertical':
            return Math.abs(event.deltaY) >= Math.abs(event.deltaX);
        case 'horizontal':
            return Math.abs(event.deltaX) > Math.abs(event.deltaY);
        default:
            return true;
    }
}

function isComposing(event) {
    return event.isComposing === true || event.key === 'Process' || event.key === 'Dead';
}

/** Whether a native control consumes the input itself, per element and per input. */
function nativeClaims(element, event) {
    if (element.matches(':disabled')) {
        return false;
    }

    if (element.matches('[contenteditable]:not([contenteditable="false"])')) {
        return event.type === 'keydown' ? editsText(event, { vertical: true, paging: true, rich: true }) : event.type === 'wheel' && scrolls(element);
    }

    if (element instanceof HTMLTextAreaElement) {
        return event.type === 'keydown' ? editsText(event, { vertical: true, paging: true }) : event.type === 'wheel' && scrolls(element);
    }

    if (element instanceof HTMLSelectElement) {
        return event.type === 'keydown' && (HorizontalKeys.has(event.key) || VerticalKeys.has(event.key) || PagingKeys.has(event.key) || event.key === 'Enter' || event.key === ' ' || isTyping(event));
    }

    if (element instanceof HTMLInputElement) {
        return inputClaims(element, event);
    }

    if (event.type === 'keydown' && (element instanceof HTMLButtonElement || element.matches('a[href], summary, [role="button"], [role="link"]'))) {
        return event.key === 'Enter' || event.key === ' ';
    }

    return false;
}

function inputClaims(input, event) {
    const { type } = input;

    if (TextInputTypes.has(type)) {
        const vertical = type === 'number';

        return event.type === 'keydown' ? editsText(event, { vertical }) : event.type === 'wheel' && vertical && document.activeElement === input;
    }

    if (event.type !== 'keydown') {
        return false;
    }

    if (SegmentedInputTypes.has(type)) {
        return HorizontalKeys.has(event.key) || VerticalKeys.has(event.key) || EditingKeys.has(event.key) || isTyping(event);
    }

    if (type === 'range') {
        return HorizontalKeys.has(event.key) || VerticalKeys.has(event.key) || PagingKeys.has(event.key);
    }

    if (type === 'radio') {
        return event.key.startsWith('Arrow') || event.key === ' ';
    }

    if (type === 'checkbox') {
        return event.key === ' ';
    }

    if (type === 'button' || type === 'submit' || type === 'reset' || type === 'file' || type === 'color' || type === 'image') {
        return event.key === 'Enter' || event.key === ' ';
    }

    return false;
}

function editsText(event, { vertical = false, paging = false, rich = false }) {
    if (isTyping(event) || HorizontalKeys.has(event.key) || EditingKeys.has(event.key)) {
        return true;
    }

    if ((vertical && VerticalKeys.has(event.key)) || (paging && PagingKeys.has(event.key))) {
        return true;
    }

    const key = event.key.toLowerCase();

    return (event.ctrlKey || event.metaKey) && (EditingShortcuts.has(key) || (rich && RichEditingShortcuts.has(key)));
}

// A character typed through AltGr arrives with Control and Alt both held.
function isTyping(event) {
    if (event.key.length !== 1) {
        return false;
    }

    if (event.getModifierState?.('AltGraph')) {
        return true;
    }

    return !event.ctrlKey && !event.metaKey && !event.altKey;
}

function scrolls(element) {
    return element.scrollHeight > element.clientHeight;
}

function observe(promise) {
    promise?.catch(error => console.error('InputRouter: the .NET owner rejected the call.', error));
}

/** The input as its family describes it, `family` first so a .NET owner can read it as the matching record. */
function describeInput(event) {
    const family = Families[event.type] ?? 'other';

    switch (family) {
        case 'keyboard':
            return { family, type: event.type, key: event.key, code: event.code, modifiers: describeModifiers(event) };
        case 'wheel':
            return { family, type: event.type, deltaX: event.deltaX, deltaY: event.deltaY, deltaMode: event.deltaMode, modifiers: describeModifiers(event) };
        case 'pointer':
            return { family, type: event.type, pointerId: event.pointerId, pointerType: event.pointerType || 'unknown', button: event.button, buttons: event.buttons, clientX: event.clientX, clientY: event.clientY, modifiers: describeModifiers(event) };
        case 'drop':
            return { family, type: event.type, data: [...(event.dataTransfer?.types ?? [])], clientX: event.clientX, clientY: event.clientY };
        default:
            return { family, type: event.type };
    }
}

function describeModifiers(event) {
    return { control: event.ctrlKey === true, meta: event.metaKey === true, alt: event.altKey === true, shift: event.shiftKey === true, primary: (isApplePlatform ? event.metaKey : event.ctrlKey) === true };
}
