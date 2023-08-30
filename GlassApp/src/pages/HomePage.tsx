import { Button, Tag } from "@blueprintjs/core";
import { observer } from "mobx-react";
import { styled } from "styled-components";
import { useGlassServer } from "../models/glass_server_manager";
import { FlightMap } from "../partials/FlightMap";

const RootDiv = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    flex-grow: 1;

    gap: 1rem;

    margin-top: 1rem;
`;

const MapDiv = styled.div`
    flex-grow: 1;
    align-self: stretch;
`;

export const HomePage = observer(() => {
    const glassServer = useGlassServer();

    return (
        <RootDiv>
            <MapDiv>
                <FlightMap />
            </MapDiv>
        </RootDiv>
    );
});
