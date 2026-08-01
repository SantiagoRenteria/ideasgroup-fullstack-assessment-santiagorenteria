import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { CreateProjectRequest, Project, ProjectListParams, UpdateProjectRequest } from '../models/project.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class ProjectService {
    private readonly baseUrl = `${environment.apiUrl}/projects`;

    constructor(private http: HttpClient) {}

    list(params: ProjectListParams): Observable<PagedResult<Project>> {
        let httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);

        if (params.name) {
            httpParams = httpParams.set('name', params.name);
        }

        if (params.status) {
            httpParams = httpParams.set('status', params.status);
        }

        return this.http.get<PagedResult<Project>>(this.baseUrl, { params: httpParams });
    }

    getById(id: string): Observable<Project> {
        return this.http.get<Project>(`${this.baseUrl}/${id}`);
    }

    create(request: CreateProjectRequest): Observable<Project> {
        return this.http.post<Project>(this.baseUrl, request);
    }

    update(id: string, request: UpdateProjectRequest): Observable<Project> {
        return this.http.put<Project>(`${this.baseUrl}/${id}`, request);
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}
