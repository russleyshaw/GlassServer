import { makeAutoObservable } from "mobx";
import { GlassServerManager } from "./glass_server_manager";

export interface SimVarModelArgs {
    name: string;
    units: string;
    intervalMs?: number;

    manager: GlassServerManager;
}

export class SimVarModel {
    readonly name: string;
    dValue: number = 0;

    isSendPending: boolean = false;

    readonly units: string;

    private manager: GlassServerManager;
    readonly intervalMs: number;

    constructor(args: SimVarModelArgs) {
        this.name = args.name;
        this.manager = args.manager;
        this.units = args.units;
        this.intervalMs = args.intervalMs ?? 1000;

        this.manager.addSimVarModel(this);

        makeAutoObservable(this, {}, { autoBind: true });
    }

    setValue(dValue: number) {
        this.dValue = dValue;
    }

    receiveValue(dValue: number) {
        this.isSendPending = false;
        this.setValue(dValue);
    }

    sendValue(dValue: number) {
        this.setValue(dValue);

        this.isSendPending = true;
        this.manager.sendSetSimVarMsg(this.name, dValue);
    }
}
