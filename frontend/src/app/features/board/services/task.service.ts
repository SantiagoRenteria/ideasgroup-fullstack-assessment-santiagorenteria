import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { BoardTask, CreateTaskRequest, MoveTaskRequest, UpdateTaskRequest } from '../models/task.model';
import { RealtimeBoardService } from './realtime-board.service';

@Injectable({ providedIn: 'root' })
export class TaskService {
    private readonly apiUrl = environment.apiUrl;

    constructor(private http: HttpClient, private realtimeService: RealtimeBoardService) {}

    create(request: CreateTaskRequest): Observable<BoardTask> {
        return this.http.post<BoardTask>(`${this.apiUrl}/tasks`, request, { headers: this.realtimeHeaders() });
    }

    update(id: string, request: UpdateTaskRequest): Observable<BoardTask> {
        return this.http.put<BoardTask>(`${this.apiUrl}/tasks/${id}`, request, { headers: this.realtimeHeaders() });
    }

    // PATCH, no PUT: refleja en el cliente la misma distincion que el backend hace
    // entre editar datos de negocio y trasladar por drag&drop (ver ADR §14.1).
    move(id: string, request: MoveTaskRequest): Observable<BoardTask> {
        return this.http.patch<BoardTask>(`${this.apiUrl}/tasks/${id}/move`, request, { headers: this.realtimeHeaders() });
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/tasks/${id}`, { headers: this.realtimeHeaders() });
    }

    // Identifica al emisor de la mutacion ante el backend para que excluya esta misma
    // conexion al notificar por tiempo real (ADR §15.3) -- sin canal abierto, se omite.
    private realtimeHeaders(): HttpHeaders {
        const connectionId = this.realtimeService.connectionId;
        return connectionId ? new HttpHeaders({ 'X-Realtime-Connection-Id': connectionId }) : new HttpHeaders();
    }
}
