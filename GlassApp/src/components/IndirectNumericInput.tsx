import { Button, NumericInput } from "@blueprintjs/core";
import { observer } from "mobx-react";
import { useState } from "react";
import { styled } from "styled-components";

export interface IndirectNumericInputProps {
    value?: number | null | undefined;

    displayFormatter?: (value: number) => string;
    isPending?: boolean;
    onChange?: (value: number) => void;

    precision?: number;
}

const EditingDiv = styled.div`
    display: flex;
    flex-direction: row;
    align-items: center;
`;

export const IndirectNumericInput = observer((props: IndirectNumericInputProps) => {
    const { value, displayFormatter, onChange, isPending, precision } = props;
    const [editingValue, setEditingValue] = useState("");

    const [isEditing, setIsEditing] = useState(false);

    const text = value != null ? displayFormatter?.(value) ?? value.toString() : "N/A";

    const onSetEditing = () => {
        setIsEditing(true);
        setEditingValue((value ?? 0).toFixed(precision ?? 0.2));
    };

    const onSubmit = () => {
        if (onChange) {
            onChange(parseFloat(editingValue));
        }
        setIsEditing(false);
    };

    const onEditingKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === "Enter") {
            onSubmit();
        } else if (e.key === "Escape") {
            onCancel();
        }
    };

    const onCancel = () => {
        setIsEditing(false);
    };

    if (!isEditing) {
        return (
            <Button
                title="Click to edit."
                intent={isPending ? "warning" : "none"}
                text={text}
                onClick={onSetEditing}
            />
        );
    }

    return (
        <EditingDiv>
            <Button icon="cross" intent="danger" onClick={onCancel} />
            <NumericInput
                autoFocus
                onKeyDown={onEditingKeyPress}
                value={editingValue}
                onValueChange={(v, vs) => setEditingValue(vs)}
            />
            <Button icon="tick" intent="primary" onClick={onSubmit} />
        </EditingDiv>
    );
});
