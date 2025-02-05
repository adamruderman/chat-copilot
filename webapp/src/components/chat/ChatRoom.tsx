// Copyright (c) Microsoft. All rights reserved.
import React, { useEffect, useRef, useState, useCallback } from 'react';
import { makeStyles, shorthands, tokens } from '@fluentui/react-components';
import { GetResponseOptions, useChat } from '../../libs/hooks/useChat';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { updateConversationMessages } from '../../redux/features/conversations/conversationsSlice';
import { FeatureKeys, Features } from '../../redux/features/app/AppState';
import { SharedStyles } from '../../styles';
import { ChatInput } from './ChatInput';
import { ChatHistory } from './chat-history/ChatHistory';
import debounce from 'lodash/debounce';

const useClasses = makeStyles({
    root: {
        ...shorthands.overflow('hidden'),
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        height: '100%',
    },
    scroll: {
        ...shorthands.margin(tokens.spacingVerticalXS),
        ...SharedStyles.scroll,
        position: 'relative',
    },
    history: {
        ...shorthands.padding(tokens.spacingVerticalM),
        paddingLeft: tokens.spacingHorizontalM,
        paddingRight: tokens.spacingHorizontalM,
        display: 'flex',
        justifyContent: 'center',
    },
    input: {
        display: 'flex',
        flexDirection: 'row',
        justifyContent: 'center',
        ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingVerticalNone),
    },
    loadingIndicator: {
        textAlign: 'center',
        color: tokens.colorNeutralForeground1,
        marginBottom: tokens.spacingVerticalM,
        fontSize: tokens.fontSizeBase300,
    },
});

export const ChatRoom: React.FC = () => {
    const dispatch = useAppDispatch();
    const classes = useClasses();
    const chat = useChat();

    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const messages = conversations[selectedId].messages;
    //const continuationToken = conversations[selectedId].continuationToken;
    const hasMoreMessages = !!conversations[selectedId].continuationToken;

    const scrollViewTargetRef = useRef<HTMLDivElement>(null);
    const [shouldAutoScroll, setShouldAutoScroll] = useState(true);
    const [isDraggingOver, setIsDraggingOver] = useState(false);
    // const [hasMoreMessages, setHasMoreMessages] = useState(true);
    const [isFetching, setIsFetching] = useState(false);

    const onDragEnter = (e: React.DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        setIsDraggingOver(true);
    };
    const onDragLeave = (e: React.DragEvent<HTMLDivElement | HTMLTextAreaElement>) => {
        e.preventDefault();
        setIsDraggingOver(false);
    };
    const scrollToBottom = useCallback(() => {
        scrollViewTargetRef.current?.scrollTo(0, scrollViewTargetRef.current.scrollHeight);
    }, []);
   const loadMoreMessages = useCallback(async () => {
       if (!hasMoreMessages || isFetching) return;

       console.log('Starting loadMoreMessages...');

       setIsFetching(true);

       try {
           const scrollContainer = scrollViewTargetRef.current;
           if (!scrollContainer) return;

           const currentScrollTop = scrollContainer.scrollTop; // Current scroll position
           const currentScrollHeight = scrollContainer.scrollHeight; // Current scroll height

           const { messages: newMessages, continuationToken: newContinuationToken } =
               await chat.loadMessages(selectedId);

           if (newMessages && newMessages.length > 0) {
               dispatch(
                   updateConversationMessages({
                       chatId: selectedId,
                       messages: newMessages,
                       continuationToken: newContinuationToken,
                       users:[]
                   }),
               );

               setTimeout(() => {
                   const newScrollHeight = scrollContainer.scrollHeight;
                   const scrollOffset = newScrollHeight - currentScrollHeight; // Calculate offset

                   scrollContainer.scrollTop = currentScrollTop + scrollOffset; // Adjust scroll position
               }, 0);
           }
       } catch (error) {
           console.error('Error loading more messages:', error);
       } finally {
           setIsFetching(false);
           console.log('Finished loadMoreMessages.');
       }
   }, [chat, selectedId, hasMoreMessages, isFetching, dispatch]);


    useEffect(() => {
        if (shouldAutoScroll) {
            scrollToBottom();
        }
    }, [messages, shouldAutoScroll, scrollToBottom]);

    useEffect(() => {
        // Scroll to the bottom when the selected chat changes
        setTimeout(() => {
            scrollViewTargetRef.current?.scrollTo(0, scrollViewTargetRef.current.scrollHeight);
        }, 0);
    }, [selectedId, messages.length]);

    useEffect(() => {
        const onScroll = debounce(() => {
            if (!scrollViewTargetRef.current) return;
            const { scrollTop, scrollHeight, clientHeight } = scrollViewTargetRef.current;

            const isAtBottom = scrollTop + clientHeight >= scrollHeight - 10;
            setShouldAutoScroll(isAtBottom);

            const thresholdReached = scrollTop <= 50;
            if (thresholdReached && hasMoreMessages && !isFetching) {
                console.log('Threshold reached, loading more messages...');
                void loadMoreMessages();
            }
        }, 300);

        if (!scrollViewTargetRef.current) return;

        const currentScrollViewTarget = scrollViewTargetRef.current;
        currentScrollViewTarget.addEventListener('scroll', onScroll);

        return () => {
            currentScrollViewTarget.removeEventListener('scroll', onScroll);
        };
    }, [hasMoreMessages, isFetching, loadMoreMessages]);

    const handleSubmit = async (options: GetResponseOptions) => {
        await chat.getResponse(options);
        setShouldAutoScroll(true);
    };

    if (conversations[selectedId].hidden) {
        return (
            <div className={classes.root}>
                <div className={classes.scroll}>
                    <div className={classes.history}>
                        <h3>
                            This conversation is not visible in the app because{' '}
                            {Features[FeatureKeys.MultiUserChat].label} is disabled. Please enable the feature in the
                            settings to view the conversation, select a different one, or create a new conversation.
                        </h3>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className={classes.root} onDragEnter={onDragEnter} onDragOver={onDragEnter} onDragLeave={onDragLeave}>
            {isFetching && <div className={classes.loadingIndicator}>Loading older messages...</div>}

            <div ref={scrollViewTargetRef} className={classes.scroll}>
                <div className={classes.history}>
                    <ChatHistory messages={messages} />
                </div>
            </div>
            <div className={classes.input}>
                <ChatInput isDraggingOver={isDraggingOver} onDragLeave={onDragLeave} onSubmit={handleSubmit} />
            </div>
        </div>
    );
};
