import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from 'src/environments/environment';
import { AuthService } from '../../../core/services/auth.service';
import { BoardTask } from '../models/task.model';

export interface TaskDeletedPayload {
    taskId: string;
    columnId: string;
}

export interface TaskMovedPayload {
    task: BoardTask;
    targetIndex: number;
}

// Canal de tiempo real del tablero (seccion 6.7). Alcance de la conexion atado al
// componente que la usa (BoardComponent.ngOnInit/ngOnDestroy), no un servicio de sesion
// compartido -- ver docs/decisions/arquitectura-decisiones.md §15.4.
@Injectable({ providedIn: 'root' })
export class RealtimeBoardService {
    private connection: signalR.HubConnection | null = null;
    private currentProjectId: string | null = null;

    private readonly taskCreatedSubject = new Subject<BoardTask>();
    private readonly taskUpdatedSubject = new Subject<BoardTask>();
    private readonly taskDeletedSubject = new Subject<TaskDeletedPayload>();
    private readonly taskMovedSubject = new Subject<TaskMovedPayload>();

    readonly taskCreated$ = this.taskCreatedSubject.asObservable();
    readonly taskUpdated$ = this.taskUpdatedSubject.asObservable();
    readonly taskDeleted$ = this.taskDeletedSubject.asObservable();
    readonly taskMoved$ = this.taskMovedSubject.asObservable();

    constructor(private authService: AuthService) {}

    // Se envia en el header X-Realtime-Connection-Id de las mutaciones HTTP (TaskService)
    // para que el backend excluya a este mismo cliente al notificar (ADR §15.3).
    get connectionId(): string | null {
        return this.connection?.connectionId ?? null;
    }

    async connect(): Promise<void> {
        // El token JWT vive solo en memoria (ver AuthService); accessTokenFactory lo
        // adjunta como query string en el handshake de WebSocket, que no admite headers
        // personalizados -- ver ADR §15 y la configuracion de JwtBearerEvents en el backend.
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(environment.signalrHubUrl, { accessTokenFactory: () => this.authService.getToken() ?? '' })
            .withAutomaticReconnect()
            .build();

        this.connection.on('TaskCreated', (task: BoardTask) => this.taskCreatedSubject.next(task));
        this.connection.on('TaskUpdated', (task: BoardTask) => this.taskUpdatedSubject.next(task));
        this.connection.on('TaskDeleted', (payload: TaskDeletedPayload) => this.taskDeletedSubject.next(payload));
        this.connection.on('TaskMoved', (payload: TaskMovedPayload) => this.taskMovedSubject.next(payload));

        // La reconexion automatica abre una conexion nueva (nuevo connectionId): la
        // membresia de grupo del servidor se pierde y hay que solicitarla de nuevo.
        this.connection.onreconnected(() => {
            if (this.currentProjectId) {
                void this.connection?.invoke('JoinBoard', this.currentProjectId);
            }
        });

        await this.connection.start();
    }

    async joinBoard(projectId: string): Promise<void> {
        this.currentProjectId = projectId;
        await this.connection?.invoke('JoinBoard', projectId);
    }

    async leaveBoard(projectId: string): Promise<void> {
        this.currentProjectId = null;
        await this.connection?.invoke('LeaveBoard', projectId);
    }

    async disconnect(): Promise<void> {
        await this.connection?.stop();
        this.connection = null;
    }
}
