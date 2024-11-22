import { AuthenticatedTemplate, UnauthenticatedTemplate, useIsAuthenticated, useMsal } from '@azure/msal-react';
import { FluentProvider, makeStyles, shorthands, Subtitle1, tokens } from '@fluentui/react-components';

//import * as React from 'react';
import { useCallback, useEffect, useState } from 'react';
import Chat from './components/chat/Chat';
import { Loading, Login } from './components/views';
import { AuthHelper } from './libs/auth/AuthHelper';
import { useChat, useFile } from './libs/hooks';
import { AlertType } from './libs/models/AlertType';
import { useAppDispatch, useAppSelector } from './redux/app/hooks';
import { RootState } from './redux/app/store';
import { FeatureKeys, Features } from './redux/features/app/AppState';
import { addAlert, setActiveUserInfo, setServiceInfo } from './redux/features/app/appSlice';
import { semanticKernelDarkTheme, semanticKernelLightTheme } from './styles';

const headerTitleColor =
    Features[FeatureKeys.HeaderTitleColor].text != '' ? Features[FeatureKeys.HeaderTitleColor].text : 'white';
const headerBackgroundColor =
    Features[FeatureKeys.HeaderBackgroundColor].text !== ''
        ? Features[FeatureKeys.HeaderBackgroundColor].text
        : '#003F72';

export const useClasses = makeStyles({
    container: {
        display: 'flex',
        flexDirection: 'column',
        height: '100vh',
        width: '100%',
        ...shorthands.overflow('hidden'),
    },
    header: {
        alignItems: 'center',
        backgroundColor: headerBackgroundColor,
        color: tokens.colorNeutralForegroundOnBrand,
        display: 'flex',
        '& h1': {
            paddingLeft: tokens.spacingHorizontalXL,
            display: 'flex',
        },
        height: '48px',
        justifyContent: 'space-between',
        width: '100%',
    },
    persona: {
        marginRight: tokens.spacingHorizontalXXL,
    },
    cornerItems: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingHorizontalS),
    },
    banner: {
        backgroundColor: 'green',
        color: headerTitleColor,
        textAlign: 'center',
        width: '100%',
        padding: '8px 0',
    },
});

export enum AppState {
    ProbeForBackend,
    SettingUserInfo,
    ErrorLoadingChats,
    ErrorLoadingUserInfo,
    LoadChats,
    LoadingChats,
    Chat,
    SigningOut,
}

const App = () => {
    const classes = useClasses();
    const [appState, setAppState] = useState(AppState.ProbeForBackend);
    const dispatch = useAppDispatch();
    const { instance } = useMsal();
    const isAuthenticated = useIsAuthenticated();
    const { features, isMaintenance } = useAppSelector((state: RootState) => state.app);

    const chat = useChat();
    const file = useFile();

    /* const [skip, setSkip] = useState(0);
    const [hasMoreChats, setHasMoreChats] = useState(true);
    const [skip, setSkip] = useState(0);
    const [hasMoreChats, setHasMoreChats] = useState(true); */

 
    

    const chatsPerPage = 19;

    const handleAppStateChange = useCallback((newState: AppState) => {
        setAppState(newState);
    }, []);

    const loadInitialChats = async () => {
    try {
        const { hasMore } = await chat.loadChats(0, chatsPerPage);
        console.log('hasMore', hasMore);
        // Fetch additional information (content safety status and service info)
        await Promise.all([
            file.getContentSafetyStatus(),
            chat.getServiceInfo().then((serviceInfo) => {
                if (serviceInfo) {
                    dispatch(setServiceInfo(serviceInfo));
                }
            }),
        ]);

        //setHasMoreChats(hasMore);
       // setSkip(chatsPerPage);
    } catch (error) {
        console.error('Error during initial chat load:', error);
    }
};

   /*  const loadMoreChats = async () => {
        if (hasMoreChats) {
            const { hasMore } = await chat.loadChats(skip, chatsPerPage);
            setHasMoreChats(hasMore);
            setSkip((prev) => prev + chatsPerPage);
        }
    }; */

    useEffect(() => {
        if (isMaintenance && appState !== AppState.ProbeForBackend) {
            handleAppStateChange(AppState.ProbeForBackend);
            return;
        }

        if (isAuthenticated && appState === AppState.SettingUserInfo) {
            const account = instance.getActiveAccount();
            if (!account) {
                handleAppStateChange(AppState.ErrorLoadingUserInfo);
            } else {
                dispatch(
                    setActiveUserInfo({
                        id: `${account.localAccountId}.${account.tenantId}`,
                        email: account.username,
                        username: account.name ?? account.username,
                    }),
                );

                if (account.username.split('@')[1] === 'microsoft.com') {
                    dispatch(
                        addAlert({
                            message:
                                'By using Chat Copilot, you agree to protect sensitive data, not store it in chat, and allow chat history collection for service improvements. This tool is for internal use only.',
                            type: AlertType.Info,
                        }),
                    );
                }

                handleAppStateChange(AppState.LoadChats);
            }
        }

        if ((isAuthenticated || !AuthHelper.isAuthAAD()) && appState === AppState.LoadChats) {
            handleAppStateChange(AppState.LoadingChats);

            loadInitialChats()
                .then(() => {
                    handleAppStateChange(AppState.Chat);
                })
                .catch((error) => {
                    console.error('Error loading chats:', error);
                    handleAppStateChange(AppState.ErrorLoadingChats);
                });
        } // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [instance, isAuthenticated, appState, isMaintenance]);

    const theme = features[FeatureKeys.DarkMode].enabled ? semanticKernelDarkTheme : semanticKernelLightTheme;
    const chatTitle =
        features[FeatureKeys.HeaderTitle].text !== '' ? features[FeatureKeys.HeaderTitle].text : 'Chat Copilot';
    const headerTitleColor =
        features[FeatureKeys.HeaderTitleColor].text !== '' ? features[FeatureKeys.HeaderTitleColor].text : 'white';
    const headerBackgroundColor =
        features[FeatureKeys.HeaderBackgroundColor].text !== ''
            ? features[FeatureKeys.HeaderBackgroundColor].text
            : '#003F72';
    const bannerText =
        features[FeatureKeys.BannerText].text !== '' ? features[FeatureKeys.BannerText].text : 'Unclassified';

    return (
        <FluentProvider className="app-container" theme={theme}>
            {AuthHelper.isAuthAAD() ? (
                <>
                    <UnauthenticatedTemplate>
                        <div className={classes.container}>
                            <div className={classes.banner}>
                                <strong>{bannerText}</strong>
                            </div>
                            <div
                                style={{
                                    color: headerTitleColor,
                                    background: headerBackgroundColor,
                                    fontSize: 24,
                                    paddingBottom: 5,
                                    display: 'table',
                                }}
                            >
                                <div style={{ display: 'table-cell', verticalAlign: 'middle', width: '57%' }}>
                                    <Subtitle1 as="h1">{chatTitle}</Subtitle1>
                                </div>
                            </div>
                            {appState === AppState.SigningOut && <Loading text="Signing you out..." />}
                            {appState !== AppState.SigningOut && <Login />}
                        </div>
                    </UnauthenticatedTemplate>
                    <AuthenticatedTemplate>
                        <Chat
                            classes={classes}
                            appState={appState}
                            setAppState={handleAppStateChange}
                           // loadMoreChats={loadMoreChats}
                        />
                    </AuthenticatedTemplate>
                </>
            ) : (
                <Chat
                    classes={classes}
                    appState={appState}
                    setAppState={handleAppStateChange}
                    //loadMoreChats={loadMoreChats}
                />
            )}
        </FluentProvider>
    );
};

export default App;
