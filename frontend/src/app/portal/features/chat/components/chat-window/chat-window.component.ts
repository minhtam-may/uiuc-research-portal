import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ChatMessage } from '@shared/types';
import { ChatService } from '../../services/chat.service';
import { MessageBubbleComponent } from '../message-bubble/message-bubble.component';

@Component({
  selector: 'app-chat-window',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MessageBubbleComponent
  ],
  templateUrl: './chat-window.component.html',
  styleUrl: './chat-window.component.scss'
})
export class ChatWindowComponent {
  private readonly chatService = inject(ChatService);

  readonly isOpen = signal(false);
  readonly question = signal('');
  readonly isSending = signal(false);
  readonly messages = signal<ChatMessage[]>([
    {
      id: 'welcome',
      role: 'assistant',
      content:
        'Hi! Ask me about recent UIUC research, publications, or faculty expertise — e.g. "Which faculty work on computer vision in healthcare?"'
    }
  ]);

  toggle(): void {
    this.isOpen.update((open) => !open);
  }

  send(): void {
    const text = this.question().trim();
    if (!text || this.isSending()) {
      return;
    }

    const userMessage: ChatMessage = { id: crypto.randomUUID(), role: 'user', content: text };
    const pendingId = crypto.randomUUID();
    const pendingMessage: ChatMessage = {
      id: pendingId,
      role: 'assistant',
      content: 'Thinking…',
      pending: true
    };

    this.messages.update((list) => [...list, userMessage, pendingMessage]);
    this.question.set('');
    this.isSending.set(true);

    this.chatService.ask(text).subscribe({
      next: (response) => {
        this.messages.update((list) =>
          list.map((m) =>
            m.id === pendingId
              ? { ...m, content: response.answer, citations: response.citations, pending: false }
              : m
          )
        );
        this.isSending.set(false);
      },
      error: () => {
        this.messages.update((list) =>
          list.map((m) =>
            m.id === pendingId
              ? {
                  ...m,
                  content: 'Sorry, something went wrong answering that. Please try again.',
                  pending: false
                }
              : m
          )
        );
        this.isSending.set(false);
      }
    });
  }
}
