interface PolledModelArgs<T> {
    getValue(): T;
    intervalMs: number;
}

export class PolledModel<T> {
    value: T;
    constructor(args: PolledModelArgs<T>) {
        this.value = args.getValue();
        setInterval(() => {
            this.setValue(args.getValue());
        }, args.intervalMs);
    }

    setValue(value: T) {
        this.value = value;
    }
}
