// Copyright (c) Microsoft. All rights reserved.

import { FC, useCallback, useState } from 'react';

import { useMsal } from '@azure/msal-react';
import {
    Avatar,
    Button,
    Menu,
    MenuDivider,
    MenuItem,
    MenuList,
    MenuPopover,
    MenuTrigger,
    Persona,
    makeStyles,
    shorthands,
    tokens,
} from '@fluentui/react-components';
import { Settings24Regular } from '@fluentui/react-icons';
import { AuthHelper } from '../../libs/auth/AuthHelper';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState, resetState } from '../../redux/app/store';
import { FeatureKeys, Features } from '../../redux/features/app/AppState';
import { SettingsDialog } from './settings-dialog/SettingsDialog';

export const useClasses = makeStyles({
    root: {
        marginRight: tokens.spacingHorizontalXL,
    },
    persona: {
        ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingVerticalMNudge),
        overflowWrap: 'break-word',
    },
});

interface IUserSettingsProps {
    setLoadingState: () => void;
}

export const UserSettingsMenu: FC<IUserSettingsProps> = ({ setLoadingState }) => {
    const classes = useClasses();
    const { instance } = useMsal();

    const { activeUserInfo, features } = useAppSelector((state: RootState) => state.app);

    const [openSettingsDialog, setOpenSettingsDialog] = useState(false);

    const onLogout = useCallback(() => {
        setLoadingState();
        AuthHelper.logoutAsync(instance);
        resetState();
    }, [instance, setLoadingState]);

    const helpTitle = Features[FeatureKeys.HelpTitle].text != '' ? Features[FeatureKeys.HelpTitle].text : 'Help';
    const helpUrl = Features[FeatureKeys.HelpUrl].text != '' ? Features[FeatureKeys.HelpUrl].text : 'https://example.com/help';

    const openHelp = () => {
        window.open(helpUrl, '_blank');
    };
    return (
        <div style={{ display: 'flex', alignItems: 'center' }}>
            <Button
                data-testid="helpButton"
                style={{ color: 'white', fontSize: 'inherit', textDecoration: 'underline' }}
                appearance="transparent"
                title="Click to open in a new tab"
                onClick={openHelp}
            >
                {helpTitle}
            </Button>
            {AuthHelper.isAuthAAD() ? (
                <Menu>
                    <MenuTrigger disableButtonEnhancement>
                        <Avatar
                            className={classes.root}
                            key={activeUserInfo?.username}
                            name={activeUserInfo?.username}
                            size={28}
                            badge={
                                !features[FeatureKeys.SimplifiedExperience].enabled
                                    ? { status: 'available' }
                                    : undefined
                            }
                            data-testid="userSettingsButton"
                        />
                    </MenuTrigger>
                    <MenuPopover>
                        <MenuList>
                            <Persona
                                className={classes.persona}
                                name={activeUserInfo?.username}
                                secondaryText={activeUserInfo?.email}
                                presence={
                                    !features[FeatureKeys.SimplifiedExperience].enabled
                                        ? { status: 'available' }
                                        : undefined
                                }
                                avatar={{ color: 'colorful' }}
                            />
                            <MenuDivider />
                            <MenuItem
                                data-testid="settingsMenuItem"
                                onClick={() => {
                                    setOpenSettingsDialog(true);
                                }}
                            >
                                Settings
                            </MenuItem>
                            <MenuItem data-testid="logOutMenuButton" onClick={onLogout}>
                                Sign out
                            </MenuItem>
                        </MenuList>
                    </MenuPopover>
                </Menu>
            ) : (
                <Button
                    data-testid="settingsButtonWithoutAuth"
                    style={{ color: 'white' }}
                    appearance="transparent"
                    icon={<Settings24Regular color="white" />}
                    onClick={() => {
                        setOpenSettingsDialog(true);
                    }}
                >
                    Settings
                </Button>
            )}
            <SettingsDialog
                open={openSettingsDialog}
                closeDialog={() => {
                    setOpenSettingsDialog(false);
                }}
            />
        </div>
    );
};
