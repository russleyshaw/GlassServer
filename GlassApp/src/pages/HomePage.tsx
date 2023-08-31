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
import { BoundingBox } from "../lib/bbox.js";
import { useAsyncIntervalMemo } from "../lib/react.js";
import { getAirports } from "../lib/overpass.js";
import { METERS_TO_NM, calculateHeading, radToDeg } from "../lib/math.js";
import { IndirectNumericInput } from "../components/IndirectNumericInput.js";
import { HeadingIcon } from "../components/HeadingIcon.js";

const RootDiv = styled.div`
    width: 100%;
    height: 100%;

    display: grid;
    grid: "controls map" 1fr / 1fr 2fr;
`;

const ControlsDiv = styled.div`
    grid-area: controls;

    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
`;

const MapDiv = styled.div`
    grid-area: map;

    width: 100%;
    height: 100%;

    overflow: hidden;
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
            this.myMap?.setView([lat, lon], undefined, {
                animate: true,
            });
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

    getBbox(): BoundingBox {
        const bounds = this.myMap?.getBounds();
        if (!bounds) {
            return {
                bottom: 0,
                left: 0,
                right: 0,
                top: 0,
            };
        }

        return {
            bottom: bounds.getSouth(),
            left: bounds.getWest(),
            right: bounds.getEast(),
            top: bounds.getNorth(),
        };
    }

    getDistanceToPlane(lat: number, lon: number) {
        return this.myMap?.distance([lat, lon], myPlaneMarker.getLatLng()) ?? 0; // meters
    }

    getHeadingToPlane(lat: number, lon: number) {
        const planePos = myPlaneMarker.getLatLng();

        return calculateHeading(planePos.lat, planePos.lng, lat, lon);
    }
}

export const HomePage = observer(() => {
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

    const airportsResult = useAsyncIntervalMemo(() => getAirports(view.getBbox()), 1000 * 15);

    return (
        <RootDiv>
            <ControlsDiv>
                <Label>Left Fuel: {leftFuelText}</Label>
                <ProgressBar stripes={false} value={leftFuelPercent} />
                <Label>Right Fuel: {rightFuelText}</Label>
                <ProgressBar stripes={false} value={rightFuelPercent} />
                <IndirectNumericInput
                    isPending={glassServer.apHeading.isSendPending}
                    displayFormatter={formatHeading}
                    value={glassServer.apHeading.dValue}
                    precision={0}
                    onChange={v => glassServer.apHeading.sendValue(v)}
                />

                <IndirectNumericInput
                    displayFormatter={formatAltitude}
                    isPending={glassServer.apAltitude.isSendPending}
                    value={glassServer.apAltitude.dValue}
                    precision={0}
                    onChange={v => glassServer.apHeading.sendValue(v)}
                />

                <Button text="Open In OSM" onClick={openInOsm} />
                <Button text="Follow" active={view.followPlane} onClick={view.toggleFollowPlane} />
                <div>
                    <span>{airportsResult.loading ? "Loading" : "Finished"} </span>
                    <ul>
                        {airportsResult.value?.map(airport => (
                            <li key={airport.id}>
                                {airport.name}:
                                {(
                                    view.getDistanceToPlane(airport.lat, airport.lon) * METERS_TO_NM
                                ).toFixed(1)}
                                nm
                                <HeadingIcon
                                    heading={view.getHeadingToPlane(airport.lat, airport.lon)}
                                />
                            </li>
                        ))}
                    </ul>
                </div>
            </ControlsDiv>
            <MapDiv ref={view.onRef}></MapDiv>
        </RootDiv>
    );
});

function formatHeading(value: number) {
    return `${value.toFixed(0)}°`;
}

function formatAltitude(value: number) {
    return `${value.toFixed(0)} ft`;
}
