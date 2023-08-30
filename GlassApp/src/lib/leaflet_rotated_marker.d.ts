import { Marker } from "leaflet";

declare module "leaflet" {
    interface Marker extends Marker {
        rotationAngle: number;
        rotationOrigin: string;

        setRotationAngle: (angle: number) => void;
        setRotationOrigin: (origin: string) => void;
    }

    interface MarkerOptions extends MarkerOptions {
        rotationAngle?: number;
        rotationOrigin?: string;
    }
}
