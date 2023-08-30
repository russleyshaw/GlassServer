import { observer } from "mobx-react";
import { Outlet } from "react-router-dom";
import styled, { DefaultTheme, ThemeProvider, createGlobalStyle, css } from "styled-components";
import { Navbar } from "../partials/Navbar";
import { SettingsDrawer } from "../partials/SettingsDrawer";
import { AppSettings, AppSettingsContext } from "../models/app_settings";
import { useState } from "react";
import { ChangelogDialog } from "../partials/ChangelogDialog";
import { EulaDialog } from "../partials/EulaDialog";

import { useDarkModeClasses } from "../lib/theme";
import { GlassServerManager, SignalManagerContext } from "../models/glass_server_manager";

const DARK_MODE_CSS = css`
    background-color: #2f343c;
`;

const GlobalStyle = createGlobalStyle`
    html, body, #root {
        height: 100%;
        width: 100%;

        margin: 0;
        padding: 0;

        ${p => p.theme.isDarkMode && DARK_MODE_CSS}
    }


    h1, h2, h3, h4, h5, h6 {
        margin: 0;
        padding: 0;
    }

`;

const RootDiv = styled.div`
    display: flex;
    flex-direction: column;
    align-items: stretch;
    height: 100%;
    width: 100%;
`;

export const RootLayout = observer(() => {
    const [appSettings] = useState(() => new AppSettings());
    const [signalManager] = useState(() => new GlassServerManager());

    const styledTheme: DefaultTheme = {
        isDarkMode: appSettings.isDarkMode,
    };

    useDarkModeClasses(appSettings.isDarkMode);

    return (
        <AppSettingsContext.Provider value={appSettings}>
            <SignalManagerContext.Provider value={signalManager}>
                <ThemeProvider theme={styledTheme}>
                    <GlobalStyle />
                    <RootDiv>
                        <Navbar />
                        <Outlet />
                        <SettingsDrawer />
                        <ChangelogDialog />
                        <EulaDialog />
                    </RootDiv>
                </ThemeProvider>
            </SignalManagerContext.Provider>
        </AppSettingsContext.Provider>
    );
});
