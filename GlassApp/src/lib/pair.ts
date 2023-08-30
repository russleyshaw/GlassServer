export type Pair<T, U> = [T, U];
export function makePair<T, U>(t: T, u: U): Pair<T, U> {
    return [t, u];
}
