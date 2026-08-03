import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
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
    // BehaviorSubject (no Subject): un componente que se suscribe despues de JoinBoard
    // debe recibir la lista actual de inmediato, no solo los cambios futuros.
    private readonly connectedUsersSubject = new BehaviorSubject<string[]>([]);

    // Avisa de que hubo una ventana ciega: mientras la conexion estuvo caida, SignalR no
    // guarda ni reenvia los eventos emitidos, asi que el estado local quedo desfasado sin
    // forma de deducirlo desde el propio canal -- ver ADR §28.3.
    private readonly reconnectedSubject = new Subject<void>();

    readonly taskCreated$ = this.taskCreatedSubject.asObservable();
    readonly taskUpdated$ = this.taskUpdatedSubject.asObservable();
    readonly taskDeleted$ = this.taskDeletedSubject.asObservable();
    readonly taskMoved$ = this.taskMovedSubject.asObservable();
    readonly connectedUsers$ = this.connectedUsersSubject.asObservable();
    readonly reconnected$ = this.reconnectedSubject.asObservable();

    constructor(private authService: AuthService) {}

    // Se envia en el header X-Realtime-Connection-Id de las mutaciones HTTP (TaskService)
    // para que el backend excluya a este mismo cliente al notificar (ADR §15.3).
    get connectionId(): string | null {
        return this.connection?.connectionId ?? null;
    }

    async connect(): Promise<void> {
        // El token JWT vive en sessionStorage (ver AuthService, ADR §17); accessTokenFactory lo
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
        this.connection.on('BoardPresenceChanged', (users: string[]) => this.connectedUsersSubject.next(users));

        // La reconexion automatica abre una conexion nueva (nuevo connectionId): la
        // membresia de grupo del servidor se pierde y hay que solicitarla de nuevo. Solo
        // despues de volver al grupo se anuncia la reconexion, para que quien resincronice
        // no se pierda los eventos que lleguen entre el refetch y el JoinBoard.
        this.connection.onreconnected(() => {
            if (!this.currentProjectId) {
                return;
            }
            void this.connection
                ?.invoke('JoinBoard', this.currentProjectId)
                .then(() => this.reconnectedSubject.next());
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
        this.connectedUsersSubject.next([]);
    }

    async disconnect(): Promise<void> {
        await this.connection?.stop();
        this.connection = null;
        this.connectedUsersSubject.next([]);
    }
}
