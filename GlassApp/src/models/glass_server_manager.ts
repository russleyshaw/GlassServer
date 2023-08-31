import { makeAutoObservable } from "mobx";
import * as signalR from "@microsoft/signalr";
import { createContext, useContext } from "react";
import { SimVarModel } from "./sim_var_model";

export class GlassServerManager {
    connection: signalR.HubConnection;

    allSimVars: Map<string, SimVarModel> = new Map();
    addSimVarQueue: SimVarModel[] = [];

    planeLatitude = new SimVarModel({
        name: "PLANE LATITUDE",
        units: "degrees",
        intervalMs: 200,

        manager: this,
    });

    planeLongitude = new SimVarModel({
        name: "PLANE LONGITUDE",
        units: "degrees",
        intervalMs: 200,

        manager: this,
    });

    planeHeading = new SimVarModel({
        name: "PLANE HEADING DEGREES TRUE",
        units: "degrees",

        manager: this,
    });

    apHeading = new SimVarModel({
        name: "AUTOPILOT HEADING LOCK DIR",
        units: "degrees",

        manager: this,
    });

    apAltitude = new SimVarModel({
        name: "AUTOPILOT ALTITUDE LOCK VAR",
        units: "feet",

        manager: this,
    });

    fuelTankLeftQty = new SimVarModel({
        name: "FUEL LEFT QUANTITY",
        units: "gallon",
        intervalMs: 5000,

        manager: this,
    });

    fuelTankRightQty = new SimVarModel({
        name: "FUEL RIGHT QUANTITY",
        units: "gallon",
        intervalMs: 5000,

        manager: this,
    });

    fuelTankLeftCap = new SimVarModel({
        name: "FUEL LEFT CAPACITY",
        units: "gallon",
        intervalMs: 1000 * 60,

        manager: this,
    });

    fuelTankRightCap = new SimVarModel({
        name: "FUEL RIGHT CAPACITY",
        units: "gallon",
        intervalMs: 1000 * 60,

        manager: this,
    });

    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("https://localhost:7136/hub", {
                transport: signalR.HttpTransportType.WebSockets,
                skipNegotiation: true,
            })
            .build();

        this.connection.on("UpdatedSimVar", (message: string, value: number) => {
            const model = this.getSimVar(message);
            if (model) {
                model.receiveValue(value);
            }
        });

        this.connection
            .start()
            .then(() => {
                console.log("Connected!");
                this.onConnected();
            })
            .catch(e => {
                console.log("Connection failed: ", e);
            });

        makeAutoObservable(this, {}, { autoBind: true });
    }

    getSimVar(name: string): SimVarModel | undefined {
        return this.addSimVarQueue.find(model => model.name === name);
    }

    onConnected() {
        for (const model of this.addSimVarQueue) {
            this.sendAddSimVarMsg(model.name, model.units, model.intervalMs);
        }
    }

    sendAddSimVarMsg(name: string, units: string, updateIntervalMs: number) {
        console.log(`Adding simvar ${name} (${units})`);

        this.connection.send("addSimVar", name, units, updateIntervalMs);
    }

    sendSetSimVarMsg(name: string, value: number) {
        console.log(`Setting simvar ${name} to ${value}`);

        this.connection.send("setSimVar", name, value);
    }

    addSimVarModel(model: SimVarModel) {
        this.addSimVarQueue.push(model);
        this.allSimVars.set(model.name, model);
    }
}

export const SignalManagerContext = createContext<GlassServerManager>(undefined!);

export function useGlassServer(): GlassServerManager {
    return useContext(SignalManagerContext);
}
