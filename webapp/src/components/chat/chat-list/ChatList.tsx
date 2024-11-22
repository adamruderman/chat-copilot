// Copyright (c) Microsoft. All rights reserved.

import InfiniteScroll from 'react-infinite-scroll-component';
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
import { FC, useCallback, useEffect, useRef, useState } from 'react';
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
import { isToday } from '../../utils/TextUtils';
import { NewBotMenu } from './bot-menu/NewBotMenu';
import { SimplifiedNewBotMenu } from './bot-menu/SimplifiedNewBotMenu';
import { ChatListSection } from './ChatListSection';

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
        overflowY: 'auto', // Enable vertical scrolling
        overflowX: 'hidden', // Prevent horizontal scrolling
        height: 'calc(100vh - 120px)', // Adjust height to allow scrolling
        flexGrow: 1, // Allow the list to grow and take available space
        '&:hover': {
            '&::-webkit-scrollbar-thumb': {
                backgroundColor: tokens.colorScrollbarOverlay,
                visibility: 'visible',
            },
        },
        '&::-webkit-scrollbar-track': {
            backgroundColor: tokens.colorSubtleBackground,
        },
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
        ...Breakpoints.small({
            display: 'none',
        }),
        fontSize: tokens.fontSizeBase500,
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
    olderConversations?: Conversations;
}

export const ChatList: FC = () => {
    const classes = useClasses();
    const { features } = useAppSelector((state: RootState) => state.app);
    const { conversations } = useAppSelector((state: RootState) => state.conversations);

    const [isFiltering, setIsFiltering] = useState(false);
    const [filterText, setFilterText] = useState('');
    const [conversationsView, setConversationsView] = useState<ConversationsView>({});
    const [hasMoreChats, setHasMoreChats] = useState(true);
    //const [skip, setSkip] = useState(0);

    const chat = useChat();
    const fileHandler = useFile();
    const dispatch = useAppDispatch();

    const chatsPerPage = 5;

    const sortConversations = (conversations: Conversations): ConversationsView => {
        const sortedIds = Object.keys(conversations).sort((a, b) => {
            if (conversations[a].lastUpdatedTimestamp === undefined) {
                return 1;
            }
            if (conversations[b].lastUpdatedTimestamp === undefined) {
                return -1;
            }
            return conversations[b].lastUpdatedTimestamp - conversations[a].lastUpdatedTimestamp;
        });

        const latestConversations: Conversations = {};
        const olderConversations: Conversations = {};
        sortedIds.forEach((id) => {
            if (isToday(new Date(conversations[id].lastUpdatedTimestamp ?? 0))) {
                latestConversations[id] = conversations[id];
            } else {
                olderConversations[id] = conversations[id];
            }
        });
        return {
            latestConversations: latestConversations,
            olderConversations: olderConversations,
        };
    };

    useEffect(() => {
        const nonHiddenConversations: Conversations = {};
        for (const key in conversations) {
            const conversation = conversations[key];
            if (
                !conversation.hidden &&
                (!filterText ||
                    getFriendlyChatName(conversation).toLocaleUpperCase().includes(filterText.toLocaleUpperCase()))
            ) {
                nonHiddenConversations[key] = conversation;
            }
        }
        setConversationsView(sortConversations(nonHiddenConversations));
    }, [conversations, filterText]);

    const loadMoreChats = async () => {
        try {
            // Calculate the correct 'skip' value based on the number of conversations already loaded
            const loadedCount =
                Object.keys(conversationsView.latestConversations ?? {}).length +
                Object.keys(conversationsView.olderConversations ?? {}).length;

            const { hasMore } = await chat.loadChats(loadedCount, chatsPerPage);

            setHasMoreChats(hasMore);
        } catch (error) {
            console.error('Error loading more chats:', error);
            setHasMoreChats(false);
        }
    };


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
            <InfiniteScroll
                dataLength={
                    Object.keys(conversationsView.latestConversations ?? {}).length +
                    Object.keys(conversationsView.olderConversations ?? {}).length
                }
                next={loadMoreChats}
                hasMore={hasMoreChats}
                loader={<h4>Loading more chats...</h4>}
                scrollableTarget="scrollableDiv"
            >
                <div aria-label={'chat list'} className={classes.list} id="scrollableDiv">
                    {conversationsView.latestConversations && (
                        <ChatListSection header="Today" conversations={conversationsView.latestConversations} />
                    )}
                    {conversationsView.olderConversations && (
                        <ChatListSection header="Older" conversations={conversationsView.olderConversations} />
                    )}
                </div>
            </InfiniteScroll>
        </div>
    );
};
