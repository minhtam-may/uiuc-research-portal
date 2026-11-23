import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env';
import { FocusArea, Paper, Researcher } from '@shared/types';

@Injectable({ providedIn: 'root' })
export class PortalDataService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getFocusAreas(limit = 12): Observable<FocusArea[]> {
    return this.http.get<FocusArea[]>(`${this.baseUrl}/focus-areas`, {
      params: { limit }
    });
  }

  getRecentPapers(limit = 20): Observable<Paper[]> {
    return this.http.get<Paper[]>(`${this.baseUrl}/papers`, {
      params: { limit }
    });
  }

  getResearchers(search?: string, limit = 24): Observable<Researcher[]> {
    return this.http.get<Researcher[]>(`${this.baseUrl}/researchers`, {
      params: search ? { search, limit } : { limit }
    });
  }
}
