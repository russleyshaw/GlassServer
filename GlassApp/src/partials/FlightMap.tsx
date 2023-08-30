import "leaflet";
import "leaflet/dist/leaflet.css";

import * as L from "leaflet";
import { observer } from "mobx-react";

import planeIcon from "../../assets/airplane.png";
import { useGlassServer } from "../models/glass_server_manager";
import { makeAutoObservable } from "mobx";
import { useEffect, useState } from "react";
import { styled } from "styled-components";
import { Button, NumericInput, ProgressBar } from "@blueprintjs/core";

import "../lib/leaflet_rotated_marker.js";
import { Label } from "@blueprintjs/core";

const RootDiv = styled.div`
    width: 100%;
    height: 100%;

    display: flex;
    flex-direction: column;
`;

const ControlsDiv = styled.div`
    display: flex;
    flex-direction: row;
    align-items: center;
    justify-content: center;
`;

const MapDiv = styled.div`
    width: 100%;
    height: 100%;
`;

const myIcon = L.icon({
    iconUrl: planeIcon,

    iconSize: [32, 32],
});

const myPlaneMarker = L.marker([0, 0], { icon: myIcon, rotationOrigin: "center center" });

const osmTileLayer = L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
    attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
});

const planePath = L.polyline([], { color: "red" });

class ViewModel {
    myMap?: L.Map;

    followPlane = true;

    constructor() {
        makeAutoObservable(this, {}, { autoBind: true });

        setInterval(() => {
            const path = planePath.getLatLngs();
            const myPlaneLatLng = myPlaneMarker.getLatLng();
            path.push(myPlaneLatLng as any);
            if (myPlaneLatLng.lat != 0 && myPlaneLatLng.lng != 0) {
                planePath.setLatLngs(path);
            }
        }, 1000);
    }

    createMap(ref: HTMLDivElement) {
        this.myMap = L.map(ref).setView([51.505, -0.09], 13);

        osmTileLayer.addTo(this.myMap);
        myPlaneMarker.addTo(this.myMap);
        planePath.addTo(this.myMap);
    }

    updatePlanePosition(lat: number, lon: number, heading: number) {
        myPlaneMarker.setLatLng([lat, lon]);
        myPlaneMarker.setRotationAngle(heading);

        if (this.followPlane) {
            this.myMap?.setView([lat, lon]);
        }
    }

    toggleFollowPlane() {
        this.followPlane = !this.followPlane;
    }

    onRef = (ref: HTMLDivElement) => {
        if (this.myMap) {
            this.myMap.remove();
        }

        this.createMap(ref);
    };
}

export const FlightMap = observer(() => {
    const glassServer = useGlassServer();

    const [view] = useState(() => new ViewModel());

    const planeLat = glassServer.planeLatitude.dValue;
    const planeLon = glassServer.planeLongitude.dValue;
    const heading = glassServer.planeHeading.dValue;
    console.log("plane lat lon:", planeLat, planeLon);
    useEffect(() => {
        view.updatePlanePosition(planeLat, planeLon, heading);
    }, [planeLat, planeLon, heading]);

    const openInOsm = () => {
        window.open(`https://www.openstreetmap.org/#map=13/${planeLat}/${planeLon}`);
    };

    const leftFuelPercent = glassServer.fuelTankLeftQty.dValue / glassServer.fuelTankLeftCap.dValue;
    const leftFuelText = `${glassServer.fuelTankLeftQty.dValue.toFixed(
        1,
    )} / ${glassServer.fuelTankLeftCap.dValue.toFixed(1)}`;
    const rightFuelPercent =
        glassServer.fuelTankRightQty.dValue / glassServer.fuelTankRightCap.dValue;
    const rightFuelText = `${glassServer.fuelTankRightQty.dValue.toFixed(
        1,
    )} / ${glassServer.fuelTankRightCap.dValue.toFixed(1)}`;

    return (
        <RootDiv>
            <Label>Left Fuel: {leftFuelText}</Label>
            <ProgressBar stripes={false} value={leftFuelPercent} />
            <Label>Right Fuel: {rightFuelText}</Label>
            <ProgressBar stripes={false} value={rightFuelPercent} />
            <ControlsDiv>
                <NumericInput value={glassServer.apHeading.dValue} />

                <NumericInput value={glassServer.apAltitude.dValue} />

                <Button text="Open In OSM" onClick={openInOsm} />
                <Button text="Follow" active={view.followPlane} onClick={view.toggleFollowPlane} />
            </ControlsDiv>
            <MapDiv ref={view.onRef}></MapDiv>;
        </RootDiv>
    );
});
