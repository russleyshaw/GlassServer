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
    sValue: string = "";
    dValue: number = 0;
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

    setValue(dValue: number, sValue: string) {
        this.dValue = dValue;
        this.sValue = sValue;
    }
}
