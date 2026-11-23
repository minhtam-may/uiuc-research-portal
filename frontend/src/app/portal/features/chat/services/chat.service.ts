import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env';
import { ChatRequest, ChatResponse } from '@shared/types';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  ask(question: string): Observable<ChatResponse> {
    const payload: ChatRequest = { question };
    return this.http.post<ChatResponse>(`${this.baseUrl}/chat`, payload);
  }
}
