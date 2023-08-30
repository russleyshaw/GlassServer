import { createHashRouter as createMyRouter } from "react-router-dom";
import { RootLayout } from "./layouts/RootLayout";
import { HomePage } from "./pages/HomePage";

export enum QueryParamKey {
    EULA_DIALOG = "eulaDg",
    CHANGELOG_DIALOG = "clgDg",
    SETTINGS_DRAWER = "stgDr",
}

export function createRouter() {
    return createMyRouter([
        {
            element: <RootLayout />,
            children: [
                {
                    path: "/",
                    element: <HomePage />,
                },
                {
                    path: "/foo",
                    element: <HomePage />,
                },
                {
                    path: "/bar",
                    element: <HomePage />,
                },
            ],
        },
    ]);
}
