export function buildMap<T, K, V>(
    items: T[],
    key: (item: T) => K,
    value: (item: T) => V,
): Map<K, V> {
    const map = new Map<K, V>();

    for (const item of items) {
        map.set(key(item), value(item));
    }

    return map;
}
