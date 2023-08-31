export function average(nums: number[]) {
    let sum = 0;
    for (let i = 0; i < nums.length; i++) {
        sum += nums[i];
    }

    return sum / nums.length;
}

export const METERS_TO_FEET = 3.28084;
export const METERS_TO_MILES = 0.000621371;
export const METERS_TO_NM = 0.000539957;

export function radToDeg(rad: number) {
    return rad * (180 / Math.PI);
}

export function calculateHeading(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const lat1Rad = (lat1 * Math.PI) / 180;
    const lon1Rad = (lon1 * Math.PI) / 180;
    const lat2Rad = (lat2 * Math.PI) / 180;
    const lon2Rad = (lon2 * Math.PI) / 180;

    const deltaLon = lon2Rad - lon1Rad;

    const y = Math.sin(deltaLon) * Math.cos(lat2Rad);
    const x =
        Math.cos(lat1Rad) * Math.sin(lat2Rad) -
        Math.sin(lat1Rad) * Math.cos(lat2Rad) * Math.cos(deltaLon);

    const initialBearingRad = Math.atan2(y, x);
    let initialBearingDeg = (initialBearingRad * 180) / Math.PI;

    // Ensure bearing is within 0 to 360 degrees
    initialBearingDeg = (initialBearingDeg + 360) % 360;

    return initialBearingDeg;
}
