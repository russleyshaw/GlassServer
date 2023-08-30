import { Navbar as BpNavbar, Alignment, Button } from "@blueprintjs/core";
import { observer } from "mobx-react";
import { useIsSettingsDrawerOpen } from "./SettingsDrawer";
import { NavButton } from "../components/NavButton";

export const Navbar = observer(() => {
    const [, setSettingsOpen] = useIsSettingsDrawerOpen();

    return (
        <BpNavbar>
            <BpNavbar.Group align={Alignment.LEFT}>
                <BpNavbar.Heading>Template</BpNavbar.Heading>
                <BpNavbar.Divider />
                <NavButton minimal icon="home" text="Home" to="/" />
                <NavButton minimal icon="document" text="Files" to="/foo" />
                <NavButton minimal icon="document" text="Files" to="/bar" />
            </BpNavbar.Group>

            <BpNavbar.Group align={Alignment.RIGHT}>
                <Button minimal icon="cog" onClick={() => setSettingsOpen(true)} />
            </BpNavbar.Group>
        </BpNavbar>
    );
});
