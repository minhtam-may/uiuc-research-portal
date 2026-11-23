export type ChatRole = 'user' | 'assistant';

export interface ChatCitation {
  type: 'paper' | 'researcher';
  id: string;
  label: string;
  url: string | null;
}

export interface ChatMessage {
  id: string;
  role: ChatRole;
  content: string;
  citations?: ChatCitation[];
  pending?: boolean;
}

export interface ChatRequest {
  question: string;
}

export interface ChatResponse {
  answer: string;
  citations: ChatCitation[];
}
