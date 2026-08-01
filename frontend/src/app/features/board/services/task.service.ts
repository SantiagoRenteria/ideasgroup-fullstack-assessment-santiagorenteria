import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { BoardTask, CreateTaskRequest, MoveTaskRequest, UpdateTaskRequest } from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
    private readonly apiUrl = environment.apiUrl;

    constructor(private http: HttpClient) {}

    create(request: CreateTaskRequest): Observable<BoardTask> {
        return this.http.post<BoardTask>(`${this.apiUrl}/tasks`, request);
    }

    update(id: string, request: UpdateTaskRequest): Observable<BoardTask> {
        return this.http.put<BoardTask>(`${this.apiUrl}/tasks/${id}`, request);
    }

    // PATCH, no PUT: refleja en el cliente la misma distincion que el backend hace
    // entre editar datos de negocio y trasladar por drag&drop (ver ADR §14.1).
    move(id: string, request: MoveTaskRequest): Observable<BoardTask> {
        return this.http.patch<BoardTask>(`${this.apiUrl}/tasks/${id}/move`, request);
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/tasks/${id}`);
    }
}
