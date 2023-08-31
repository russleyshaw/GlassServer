import { Icon } from "@blueprintjs/core";
import { observer } from "mobx-react";

interface HeadingIconProps {
    heading: number;
}

export const HeadingIcon = observer((props: HeadingIconProps) => {
    const { heading } = props;

    const transform = `rotate(${heading - 90}deg)`;

    return <Icon icon="direction-right" style={{ transform }} />;
});
