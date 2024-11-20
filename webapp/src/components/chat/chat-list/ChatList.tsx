// Copyright (c) Microsoft. All rights reserved.

import { FC, useCallback, useEffect, useRef, useState } from 'react';
import {
    Button,
    Input,
    InputOnChangeData,
    makeStyles,
    mergeClasses,
    shorthands,
    Subtitle2Stronger,
    tokens,
} from '@fluentui/react-components';
import { useChat, useFile } from '../../../libs/hooks';
import { getFriendlyChatName } from '../../../libs/hooks/useChat';
import { AlertType } from '../../../libs/models/AlertType';
import { ChatArchive } from '../../../libs/models/ChatArchive';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { addAlert } from '../../../redux/features/app/appSlice';
import { FeatureKeys } from '../../../redux/features/app/AppState';
import { Conversations } from '../../../redux/features/conversations/ConversationsState';
import { Breakpoints } from '../../../styles';
import { FileUploader } from '../../FileUploader';
import { Dismiss20, Filter20 } from '../../shared/BundledIcons';
import { ChatListSection } from './ChatListSection';
import { NewBotMenu } from './bot-menu/NewBotMenu';
import { SimplifiedNewBotMenu } from './bot-menu/SimplifiedNewBotMenu';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexShrink: 0,
        width: '320px',
        backgroundColor: tokens.colorNeutralBackground4,
        flexDirection: 'column',
        ...shorthands.overflow('hidden'),
        ...Breakpoints.small({
            width: '64px',
        }),
    },
    list: {
        overflowY: 'auto',
        overflowX: 'hidden',
        flexGrow: 1,
        '&:hover': {
            '&::-webkit-scrollbar-thumb': {
                backgroundColor: tokens.colorScrollbarOverlay,
                visibility: 'visible',
            },
        },
        '&::-webkit-scrollbar-track': {
            backgroundColor: tokens.colorSubtleBackground,
        },
        alignItems: 'stretch',
    },
    header: {
        display: 'flex',
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginRight: tokens.spacingVerticalM,
        marginLeft: tokens.spacingHorizontalXL,
        alignItems: 'center',
        height: '60px',
        ...Breakpoints.small({
            justifyContent: 'center',
        }),
    },
    title: {
        flexGrow: 1,
        fontSize: tokens.fontSizeBase500,
        ...Breakpoints.small({
            display: 'none',
        }),
    },
    input: {
        flexGrow: 1,
        ...shorthands.padding(tokens.spacingHorizontalNone),
        ...shorthands.border(tokens.borderRadiusNone),
        backgroundColor: tokens.colorSubtleBackground,
        fontSize: tokens.fontSizeBase500,
    },
});

interface ConversationsView {
    latestConversations?: Conversations;
}

export const ChatList: FC = () => {
    const classes = useClasses();
    const { features } = useAppSelector((state: RootState) => state.app);
    const { conversations } = useAppSelector((state: RootState) => state.conversations);

    const [isFiltering, setIsFiltering] = useState(false);
    const [filterText, setFilterText] = useState('');
    const [conversationsView, setConversationsView] = useState<ConversationsView>({
        latestConversations: conversations,
    });

    const chat = useChat();
    const fileHandler = useFile();
    const dispatch = useAppDispatch();

    const listRef = useRef<HTMLDivElement>(null);
    const [isLoadingMore, setIsLoadingMore] = useState(false);

    const handleLoadMoreChats = async () => {
        setIsLoadingMore(true);
        try {
            await chat.loadMoreChats();
            console.log('Chats loaded successfully.');
        } catch (error) {
            console.error('Error loading chats:', error);
        } finally {
            setIsLoadingMore(false);
        }
    };

    useEffect(() => {
        const handleScroll = () => {
            if (listRef.current) {
                const { scrollTop, scrollHeight, clientHeight } = listRef.current;
                if (scrollTop + clientHeight >= scrollHeight - 100 && !isLoadingMore) {
                    void handleLoadMoreChats();
                }
            }
        };

        const currentListRef = listRef.current;
        if (currentListRef) {
            currentListRef.addEventListener('scroll', handleScroll);
        }

        return () => {
            if (currentListRef) {
                currentListRef.removeEventListener('scroll', handleScroll);
            }
        };
    }, [isLoadingMore]);

    useEffect(() => {
        const filteredConversations: Conversations = {};
        for (const key in conversations) {
            const conversation = conversations[key];
            if (
                !conversation.hidden &&
                (!filterText ||
                    getFriendlyChatName(conversation).toLocaleUpperCase().includes(filterText.toLocaleUpperCase()))
            ) {
                filteredConversations[key] = conversation;
            }
        }

        setConversationsView({ latestConversations: filteredConversations });
    }, [conversations, filterText]);

    const onFilterClick = () => {
        setIsFiltering(true);
    };

    const onFilterCancel = () => {
        setFilterText('');
        setIsFiltering(false);
    };

    const onSearch = (ev: React.ChangeEvent<HTMLInputElement>, data: InputOnChangeData) => {
        ev.preventDefault();
        setFilterText(data.value);
    };

    const fileUploaderRef = useRef<HTMLInputElement>(null);
    const onUpload = useCallback(
        (file: File) => {
            fileHandler.loadFile<ChatArchive>(file, chat.uploadBot).catch((error) =>
                dispatch(
                    addAlert({
                        message: `Failed to parse uploaded file. ${error instanceof Error ? error.message : ''}`,
                        type: AlertType.Error,
                    }),
                ),
            );
        },
        [fileHandler, chat, dispatch],
    );

    return (
        <div className={classes.root}>
            <div className={classes.header}>
                {features[FeatureKeys.SimplifiedExperience].enabled ? (
                    <SimplifiedNewBotMenu onFileUpload={() => fileUploaderRef.current?.click()} />
                ) : (
                    <>
                        {!isFiltering && (
                            <>
                                <Subtitle2Stronger className={classes.title}>Conversations</Subtitle2Stronger>
                                <Button icon={<Filter20 />} appearance="transparent" onClick={onFilterClick} />
                                <NewBotMenu onFileUpload={() => fileUploaderRef.current?.click()} />
                                <FileUploader
                                    ref={fileUploaderRef}
                                    acceptedExtensions={['.json']}
                                    onSelectedFile={onUpload}
                                />
                            </>
                        )}
                        {isFiltering && (
                            <>
                                <Input
                                    placeholder="Filter by name"
                                    className={mergeClasses(classes.input, classes.title)}
                                    onChange={onSearch}
                                    autoFocus
                                />
                                <Button icon={<Dismiss20 />} appearance="transparent" onClick={onFilterCancel} />
                            </>
                        )}
                    </>
                )}
            </div>
            <div aria-label="chat list" className={classes.list} ref={listRef}>
                {conversationsView.latestConversations && (
                    <ChatListSection header="Today" conversations={conversationsView.latestConversations} />
                )}
            </div>
        </div>
    );
};
