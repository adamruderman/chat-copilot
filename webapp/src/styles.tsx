import {
    BrandVariants,
    GriffelStyle,
    Theme,
    createDarkTheme,
    createLightTheme,
    makeStaticStyles,
    makeStyles,
    shorthands,
    themeToTokensObject,
    tokens,
} from '@fluentui/react-components';

export const semanticKernelBrandRamp: BrandVariants = {
    10: '#060103',
    20: '#261018',
    30: '#431426',
    40: '#591732',
    50: '#701A3E',
    60: '#861F4B',
    70: '#982C57',
    80: '#A53E63',
    90: '#B15070',
    100: '#BC627E',
    110: '#C6748B',
    120: '#CF869A',
    130: '#D898A8',
    140: '#E0AAB7',
    150: '#E8BCC6',
    160: '#EFCFD6',
};

export const semanticKernelLightTheme: Theme & { colorMeBackground: string } = {
    ...createLightTheme(semanticKernelBrandRamp),
    colorMeBackground: '#e8ebf9',
};

export const semanticKernelDarkTheme: Theme & { colorMeBackground: string } = {
    ...createDarkTheme(semanticKernelBrandRamp),
    colorMeBackground: '#2b2b3e',
    
};

export const useGlobalDarkStyles = makeStaticStyles({
    'body[data-theme="dark"] pre': {
        backgroundColor: '#1e1e2e', // Dark background for <pre>
        color: '#ffffff', // Light text
        padding: '12px',
        borderRadius: '4px',
        fontFamily: 'monospace',
        overflowX: 'auto',
    },
    'body[data-theme="dark"] pre code': {
        backgroundColor: 'transparent', // Match <pre> background
        color: 'inherit', // Inherit text color from <pre>
        fontFamily: 'inherit', // Use the same font as <pre>
    },
    'body[data-theme="dark"] code': {
        backgroundColor: '#1e1e2e', // Dark background for standalone <code>
        color: '#ffffff', // Light text color
        padding: '4px 8px', // Add some padding for readability
        borderRadius: '4px', // Rounded corners for better appearance
        fontFamily: 'monospace', // Code-friendly font
        overflowX: 'auto', // Handle long lines
    },
});

export const customTokens = themeToTokensObject(semanticKernelLightTheme);

export const Breakpoints = {
    small: (style: GriffelStyle): Record<string, GriffelStyle> => {
        return { '@media (max-width: 744px)': style };
    },
};

export const ScrollBarStyles: GriffelStyle = {
    overflowY: 'auto',
    '&:hover': {
        '&::-webkit-scrollbar-thumb': {
            backgroundColor: tokens.colorScrollbarOverlay,
            visibility: 'visible',
        },
        '&::-webkit-scrollbar-track': {
            backgroundColor: tokens.colorNeutralBackground1,
            WebkitBoxShadow: 'inset 0 0 5px rgba(0, 0, 0, 0.1)',
            visibility: 'visible',
        },
    },
};

export const SharedStyles: Record<string, GriffelStyle> = {
    scroll: {
        height: '100%',
        ...ScrollBarStyles,
    },
    overflowEllipsis: {
        ...shorthands.overflow('hidden'),
        textOverflow: 'ellipsis',
        whiteSpace: 'nowrap',
    },
};

export const useSharedClasses = makeStyles({
    informativeView: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.padding('80px'),
        alignItems: 'center',
        ...shorthands.gap(tokens.spacingVerticalXL),
        marginTop: tokens.spacingVerticalXXXL,
    },
});

export const useDialogClasses = makeStyles({
    surface: {
        paddingRight: tokens.spacingVerticalXS,
    },
    content: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.overflow('hidden'),
        width: '100%',
    },
    paragraphs: {
        marginTop: tokens.spacingHorizontalS,
    },
    innerContent: {
        height: '100%',
        ...SharedStyles.scroll,
        paddingRight: tokens.spacingVerticalL,
    },
    text: {
        whiteSpace: 'pre-wrap',
        textOverflow: 'wrap',
    },
    footer: {
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'flex-start',
        minWidth: '175px',
    },
});
