// The culture cookie, written where a culture changes in place and no request carries the change to the server.

/** Writes the cookie `name` holding `token` at `path` for `maxAge` seconds, sent over HTTPS alone where `isSecure`. */
export function write(name, token, path, maxAge, isSecure) {
    const attributes = [`path=${path}`, `max-age=${maxAge}`, 'samesite=lax'];

    if (isSecure) {
        attributes.push('secure');
    }

    document.cookie = [`${name}=${encodeURIComponent(token)}`, ...attributes].join('; ');
}
