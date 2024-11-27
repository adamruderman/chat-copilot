// Copyright (c) Microsoft. All rights reserved.

import { IChatMessage } from '../../../libs/models/ChatMessage';
import { ChatState } from './ChatState';
import { IChatSession } from '../../../libs/models/ChatSession';

export type Conversations = Record<string, ChatState>;

export interface ConversationsState {
    conversations: Conversations; // Existing conversations with messages
    selectedId: string; // Currently selected conversation ID
    chatSessions: {
        sessions: IChatSession[]; // List of loaded chat sessions
        continuationToken: string | null; // Continuation token for session pagination
    };
}

export const initialState: ConversationsState = {
    conversations: {},
    selectedId: '',
    chatSessions: {
        sessions: [],
        continuationToken: null,
    },
};

export interface UpdateConversationPayload {
    id: string;
    messages: IChatMessage[];
}

export interface ConversationTitleChange {
    id: string;
    newTitle: string;
}

export interface ConversationInputChange {
    id: string;
    newInput: string;
}

export interface ConversationSystemDescriptionChange {
    id: string;
    newSystemDescription: string;
}

export interface UpdatePluginStatePayload {
    id: string;
    pluginName: string;
    newState: boolean;
}
