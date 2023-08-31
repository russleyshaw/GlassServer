import { useEffect, useRef, useState, useCallback } from "react";
import { delayMs } from "./async";

/**
 * Delays the closing of a component by a specified amount of time.
 * @param isOpen
 * @param delayMs
 * @returns
 */
export function useClosingDelay(isOpen: boolean, delayMs: number = 1000): boolean {
    const [value, setValue] = useState(isOpen);
    const timeout = useRef<number>();

    useEffect(() => {
        if (isOpen) {
            // Open immediately
            setValue(true);
        } else {
            if (timeout.current) {
                clearTimeout(timeout.current);
            }
            // Close after delay
            timeout.current = setTimeout(() => setValue(false), delayMs);
            return () => {
                clearTimeout(timeout.current);
            };
        }
    }, [isOpen]);

    return value;
}

export function useLocalStorageBool(key: string) {
    const [value, setValue] = useState(() => {
        const value = localStorage.getItem(key);
        return value === "true";
    });

    useEffect(() => {
        const value = localStorage.getItem(key);
        setValue(value === "true");
    }, [key]);

    useEffect(() => {
        window.addEventListener("storage", e => {
            if (e.key === key) {
                const value = localStorage.getItem(key);
                setValue(value === "true");
            }
        });

        return () => {
            window.removeEventListener("storage", () => {});
        };
    }, []);

    const setBoolValue = useCallback(
        (newValue: boolean) => {
            setValue(newValue);
            localStorage.setItem(key, newValue ? "true" : "false");
        },
        [key],
    );

    return [value, setBoolValue] as const;
}

export function useAsyncIntervalMemo<T>(
    callback: () => Promise<T>,
    intervalMs: number,
): { value: T | undefined; loading: boolean } {
    const [value, setValue] = useState<T | undefined>();
    const callbackRef = useRef(callback);
    callbackRef.current = callback;

    const [loading, setLoading] = useState(true);

    useEffect(() => {
        let timeout: number;

        async function doCallback() {
            try {
                const value = await callback();
                setValue(value);
            } catch (e) {
                console.log(e);
                await delayMs(intervalMs);
            }
            setLoading(false);

            timeout = setTimeout(doCallback, intervalMs);
        }

        doCallback();

        return () => {
            clearTimeout(timeout);
        };
    }, [intervalMs]);

    return { value, loading };
}
