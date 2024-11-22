// Copyright (c) Microsoft. All rights reserved.

import React, { useEffect, useRef, useState } from 'react';
import { makeStyles, shorthands, tokens } from '@fluentui/react-components';
import { GetResponseOptions, useChat } from '../../libs/hooks/useChat';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
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
    const classes = useClasses();
    const chat = useChat();

    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const messages = conversations[selectedId].messages;

    const scrollViewTargetRef = useRef<HTMLDivElement>(null);
    const [shouldAutoScroll, setShouldAutoScroll] = useState(true);
    const [isDraggingOver, setIsDraggingOver] = useState(false);

    const [hasMoreMessages, setHasMoreMessages] = useState(true);
    const [isFetching, setIsFetching] = useState(false);
    const [skip, setSkip] = useState(messages.length);

    const onDragEnter = (e: React.DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        setIsDraggingOver(true);
    };
    const onDragLeave = (e: React.DragEvent<HTMLDivElement | HTMLTextAreaElement>) => {
        e.preventDefault();
        setIsDraggingOver(false);
    };

    const loadMoreMessages = async () => {
        if (!hasMoreMessages || isFetching) return;

        setIsFetching(true);
        const currentScrollHeight = scrollViewTargetRef.current?.scrollHeight ?? 0;
        try {
            console.log(`Loading messages with skip: ${skip} and count: 10`);
            const { hasMore } = await chat.loadMessages(selectedId, skip, 10);

            // Adjust scroll position to maintain the user's view
            setTimeout(() => {
                const newScrollHeight = scrollViewTargetRef.current?.scrollHeight ?? 0;
                const scrollDelta = newScrollHeight - currentScrollHeight;
                scrollViewTargetRef.current?.scrollBy(0, scrollDelta);
            }, 0);

            setHasMoreMessages(hasMore);
            setSkip((prev) => prev + 10);
        } catch (error) {
            console.error('Error loading more messages:', error);
            setHasMoreMessages(false);
        } finally {
            setIsFetching(false);
        }
    };

    useEffect(() => {
        if (!shouldAutoScroll) return;
        scrollViewTargetRef.current?.scrollTo(0, scrollViewTargetRef.current.scrollHeight);
    }, [messages, shouldAutoScroll]);

    useEffect(() => {
        const onScroll = debounce(() => {
            if (!scrollViewTargetRef.current) return;
            const { scrollTop, scrollHeight, clientHeight } = scrollViewTargetRef.current;

            const isAtBottom = scrollTop + clientHeight >= scrollHeight - 10;
            setShouldAutoScroll(isAtBottom);

            const thresholdReached = scrollTop <= 50;
            if (thresholdReached && hasMoreMessages) {
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
    }, [hasMoreMessages, skip]);

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
