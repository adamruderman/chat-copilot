// Copyright (c) Microsoft. All rights reserved.

import { makeStyles, shorthands, tokens, Text } from '@fluentui/react-components';
import React from 'react';
import { IChatMessage } from '../../../libs/models/ChatMessage';
import { ChatHistoryItem } from './ChatHistoryItem';

const useClasses = makeStyles({
    root: {
        ...shorthands.gap(tokens.spacingVerticalM),
        display: 'flex',
        flexDirection: 'column',
        maxWidth: '105em',
        width: '100%',
        justifySelf: 'center',
    },
    item: {
        display: 'flex',
        flexDirection: 'column',
    },
});

interface ChatHistoryProps {
    messages: IChatMessage[];
}

export const ChatHistory: React.FC<ChatHistoryProps> = ({ messages }) => {
    const classes = useClasses();

    // Render a placeholder if there are no messages
    if (messages.length === 0) {
        return (
            <div className={classes.root}>
                <Text></Text>
            </div>
        );
    }

    return (
        <div className={classes.root}>
            {messages.map((message, index) => (
                <ChatHistoryItem key={message.timestamp} message={message} messageIndex={index} />
            ))}
        </div>
    );
};
