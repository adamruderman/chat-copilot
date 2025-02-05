import { Divider, Switch, Text, makeStyles, shorthands, tokens } from '@fluentui/react-components';
import { useCallback } from 'react';
import { AuthHelper } from '../../../libs/auth/AuthHelper';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { FeatureKeys, Setting } from '../../../redux/features/app/AppState';
import { toggleFeatureFlag } from '../../../redux/features/app/appSlice';
import { toggleMultiUserConversations } from '../../../redux/features/conversations/conversationsSlice';
import { UserPreferenceService } from '../../../libs/services/UserPreferenceService';
import { useMsal } from '@azure/msal-react';

const useClasses = makeStyles({
    feature: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.gap(tokens.spacingVerticalNone),
    },
    featureDescription: {
        paddingLeft: '5%',
        paddingBottom: tokens.spacingVerticalS,
    },
});

interface ISettingsSectionProps {
    setting: Setting;
    contentOnly?: boolean;
}

export const SettingSection: React.FC<ISettingsSectionProps> = ({ setting, contentOnly }) => {
    const classes = useClasses();
    const { activeUserInfo, features } = useAppSelector((state: RootState) => state.app);
    const dispatch = useAppDispatch();
    const { instance, inProgress } = useMsal();

    const onFeatureChange = useCallback(
        async (featureKey: FeatureKeys) => {
            // Compute updated features based on the current state
            const updatedFeatures = {
                ...features,
                [featureKey]: {
                    ...features[featureKey],
                    enabled: !features[featureKey].enabled,
                },
            };

            // Dispatch the feature toggle action
            dispatch(toggleFeatureFlag(featureKey));
            if (featureKey === FeatureKeys.MultiUserChat) {
                dispatch(toggleMultiUserConversations());
            }

            // Prepare preferences with updated features
            const preferences = {
                Id: activeUserInfo?.id ?? '',
                UserId: activeUserInfo?.id ?? '',
                DarkMode: updatedFeatures[FeatureKeys.DarkMode].enabled,
                SimplifiedChat: updatedFeatures[FeatureKeys.SimplifiedExperience].enabled,
                Persona: updatedFeatures[FeatureKeys.Personas].enabled,
                ExportChat: updatedFeatures[FeatureKeys.BotAsDocs].enabled,
            };

            try {
                const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
                await UserPreferenceService.setUserPreference(preferences, accessToken);
                console.log('Preferences saved successfully');
            } catch (error) {
                console.error('Error saving preferences:', error);
            }
        },
        [dispatch, features, activeUserInfo, inProgress, instance],
    );

    return (
        <>
            {!contentOnly && <h3>{setting.title}</h3>}
            {setting.description && <p>{setting.description}</p>}
            <div
                style={{
                    display: 'flex',
                    flexDirection: setting.stackVertically ? 'column' : 'row',
                    flexWrap: 'wrap',
                }}
            >
                {setting.features
                    .filter((key) => key !== FeatureKeys.ExportChatSessions) // Exclude ExportChatSessions
                    .map((key) => {
                        const feature = features[key];
                        return (
                            <div key={key} className={classes.feature}>
                                <Switch
                                    label={feature.label}
                                    checked={feature.enabled}
                                    disabled={
                                        !!feature.inactive ||
                                        (key === FeatureKeys.MultiUserChat && !AuthHelper.isAuthAAD())
                                    }
                                    onChange={() => {
                                        void onFeatureChange(key);
                                    }}
                                    data-testid={feature.label}
                                />
                                <Text
                                    className={classes.featureDescription}
                                    style={{
                                        color: feature.inactive
                                            ? tokens.colorNeutralForegroundDisabled
                                            : tokens.colorNeutralForeground2,
                                    }}
                                >
                                    {feature.description}
                                </Text>
                            </div>
                        );
                    })}
            </div>
            {!contentOnly && <Divider />}
        </>
    );
};
