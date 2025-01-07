import { AuthenticatedTemplate, UnauthenticatedTemplate, useIsAuthenticated, useMsal } from '@azure/msal-react';
import { FluentProvider, makeStyles, shorthands,tokens } from '@fluentui/react-components';
import { useCallback, useEffect, useState } from 'react';
import Chat from './components/chat/Chat';
import { Loading, Login } from './components/views'; 
import { AuthHelper } from './libs/auth/AuthHelper';
import { useChat, useFile } from './libs/hooks';
import { AlertType } from './libs/models/AlertType';
import { useAppDispatch, useAppSelector } from './redux/app/hooks';
import { RootState } from './redux/app/store';
import { FeatureKeys, Features } from './redux/features/app/AppState';
import { addAlert, setActiveUserInfo, setServiceInfo, setFeatureFlag } from './redux/features/app/appSlice';
import { semanticKernelDarkTheme, semanticKernelLightTheme, useGlobalDarkStyles } from './styles';
import { updateChatSessions } from './redux/features/conversations/conversationsSlice';
import { UserPreferenceService } from './libs/services/UserPreferenceService';

import logo from './assets/frontend-icons/logo.png';

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
    const { instance, inProgress } = useMsal();
    const isAuthenticated = useIsAuthenticated();
    const { features, isMaintenance } = useAppSelector((state: RootState) => state.app);

    // Access chat sessions continuation token correctly
    const { chatSessions } = useAppSelector((state: RootState) => state.conversations);
    const { continuationToken: chatSessionsContinuationToken } = chatSessions;

    const chat = useChat();
    const file = useFile();

    useEffect(() => {
        const loadPreferences = async () => {
            try {
                const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
                const preferences = await UserPreferenceService.getUserPreference(accessToken);

                // Ensure each feature flag matches the preferences
                if (preferences) {
                    dispatch(setFeatureFlag({ featureKey: FeatureKeys.DarkMode, enabled: preferences.DarkMode }));
                    dispatch(
                        setFeatureFlag({
                            featureKey: FeatureKeys.SimplifiedExperience,
                            enabled: preferences.SimplifiedChat,
                        }),
                    );
                    dispatch(setFeatureFlag({ featureKey: FeatureKeys.Personas, enabled: preferences.Persona }));
                    dispatch(setFeatureFlag({ featureKey: FeatureKeys.BotAsDocs, enabled: preferences.ExportChat }));
                }
            } catch (error) {
                console.error('Error loading user preferences:', error);
            }
        };
        if (isAuthenticated) {
            void loadPreferences();
        }
    }, [dispatch, instance, inProgress, isAuthenticated]);
    // Delay theme calculation until features have updated
    const [isPreferencesLoaded, setIsPreferencesLoaded] = useState(false);

    useEffect(() => {
        // Watch for changes in features and set the loaded flag
        if (FeatureKeys.DarkMode in features) {
            setIsPreferencesLoaded(true);
        }
    }, [features]);

    // const theme = isPreferencesLoaded
    //     ? features[FeatureKeys.DarkMode].enabled
    //         ? semanticKernelDarkTheme
    //         : semanticKernelLightTheme
    //     : semanticKernelLightTheme; // Default to light theme until preferences are loaded

    const chatsPerPage = 19;

    const handleAppStateChange = useCallback((newState: AppState) => {
        setAppState(newState);
    }, []);

    const loadInitialChats = async () => {
        try {
            const {
                chats = [],
                continuationToken,
                hasMore,
            } = await chat.loadChats(chatSessionsContinuationToken, chatsPerPage);

            console.log('Loaded chats:', chats);
            console.log('Has more chats:', hasMore);

            // Ensure chats is an array before dispatching
            dispatch(updateChatSessions({ sessions: chats, continuationToken }));

            // Fetch additional information (content safety status and service info)
            await Promise.all([
                file.getContentSafetyStatus(),
                chat.getServiceInfo().then((serviceInfo) => {
                    if (serviceInfo) {
                        dispatch(setServiceInfo(serviceInfo));
                    }
                }),
            ]);
        } catch (error) {
            console.error('Error during initial chat load:', error);
            dispatch(
                addAlert({
                    message: `Failed to load initial chats. ${(error as Error).message}`,
                    type: AlertType.Error,
                }),
            );
        }
    };

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
        }
    }, [instance, isAuthenticated, appState, isMaintenance, handleAppStateChange, dispatch]);

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

    useEffect(() => {
        // Dynamically set the document title using Features
        document.title = chatTitle ?? 'Chat Copilot';
    }, [chatTitle]); // Runs once on component mount

    useEffect(() => {
        if (FeatureKeys.DarkMode in features) {
            setIsPreferencesLoaded(true);
        }
    }, [features]);

        useEffect(() => {
            if (FeatureKeys.DarkMode in features) {
                setIsPreferencesLoaded(true);
            }
        }, [features]);

        const isDarkTheme = isPreferencesLoaded ? features[FeatureKeys.DarkMode].enabled : false;

        const theme = isDarkTheme ? semanticKernelDarkTheme : semanticKernelLightTheme;

        // Apply the dark theme styles globally
        useGlobalDarkStyles();

        // Dynamically set the `data-theme` attribute on the `body`
        useEffect(() => {
            document.body.setAttribute('data-theme', isDarkTheme ? 'dark' : 'light');
        }, [isDarkTheme]);
    
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
                                    <img width="200" height="40" aria-label={chatTitle} src={logo}></img>
                                </div>
                            </div>
                            {appState === AppState.SigningOut && <Loading text="Signing you out..." />}
                            {appState !== AppState.SigningOut && <Login />}
                        </div>
                    </UnauthenticatedTemplate>
                    <AuthenticatedTemplate>
                        <Chat classes={classes} appState={appState} setAppState={handleAppStateChange} />
                    </AuthenticatedTemplate>
                </>
            ) : (
                <Chat classes={classes} appState={appState} setAppState={handleAppStateChange} />
            )}
        </FluentProvider>
    );
};

export default App;
