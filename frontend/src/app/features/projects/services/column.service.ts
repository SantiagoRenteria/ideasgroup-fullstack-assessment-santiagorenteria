import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { CreateColumnRequest, ProjectColumn, UpdateColumnRequest } from '../models/column.model';

@Injectable({ providedIn: 'root' })
export class ColumnService {
    private readonly apiUrl = environment.apiUrl;

    constructor(private http: HttpClient) {}

    listByProject(projectId: string): Observable<ProjectColumn[]> {
        return this.http.get<ProjectColumn[]>(`${this.apiUrl}/projects/${projectId}/columns`);
    }

    create(projectId: string, request: CreateColumnRequest): Observable<ProjectColumn> {
        return this.http.post<ProjectColumn>(`${this.apiUrl}/projects/${projectId}/columns`, request);
    }

    update(id: string, request: UpdateColumnRequest): Observable<ProjectColumn> {
        return this.http.put<ProjectColumn>(`${this.apiUrl}/columns/${id}`, request);
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/columns/${id}`);
    }
}
