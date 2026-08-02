import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Subscription } from 'rxjs';
import { AppUser } from '../models/app-user.model';
import { Board, BoardColumn } from '../models/board.model';
import { BoardTask, TASK_PRIORITY_LABELS, TASK_PRIORITY_SEVERITY, TaskPriority } from '../models/task.model';
import { RealtimeBoardService, TaskDeletedPayload, TaskMovedPayload } from '../services/realtime-board.service';
import { BoardService } from '../services/board.service';
import { ReportFormat, ReportService } from '../services/report.service';
import { TaskService } from '../services/task.service';
import { UserService } from '../services/user.service';

@Component({
    selector: 'app-board',
    templateUrl: './board.component.html',
    styleUrls: ['./board.component.scss'],
    providers: [ConfirmationService, MessageService]
})
export class BoardComponent implements OnInit, OnDestroy {
    board: Board | null = null;
    loading = false;

    taskFormVisible = false;
    editingTask: BoardTask | null = null;
    targetColumnId: string | null = null;
    downloadingReport: ReportFormat | null = null;

    users: AppUser[] = [];
    filterAssigneeId: string | null = null;
    filterPriority: TaskPriority | null = null;
    searchText = '';
    connectedUsers: string[] = [];

    readonly priorityLabels = TASK_PRIORITY_LABELS;
    readonly prioritySeverity = TASK_PRIORITY_SEVERITY;
    readonly priorityOptions = Object.values(TaskPriority).map((value) => ({ label: TASK_PRIORITY_LABELS[value], value }));

    private projectId!: string;
    private readonly realtimeSubscriptions: Subscription[] = [];

    constructor(
        private route: ActivatedRoute,
        private boardService: BoardService,
        private taskService: TaskService,
        private reportService: ReportService,
        private userService: UserService,
        private realtimeService: RealtimeBoardService,
        private confirmationService: ConfirmationService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.projectId = this.route.snapshot.paramMap.get('projectId')!;
        this.loadBoard();
        this.connectRealtime();
        this.userService.listAll().subscribe({
            next: (users) => (this.users = users),
            // No bloquea el uso del tablero: solo el filtro por responsable queda sin
            // opciones hasta la proxima carga exitosa.
            error: () => this.messageService.add({ severity: 'warn', summary: 'Aviso', detail: 'No se pudo cargar la lista de responsables' })
        });
    }

    // Cierre correcto de la conexion y las suscripciones al destruir el componente
    // (seccion 6.7, ultimo punto) -- ver docs/decisions/arquitectura-decisiones.md §15.4.
    ngOnDestroy(): void {
        this.realtimeSubscriptions.forEach((subscription) => subscription.unsubscribe());
        void this.realtimeService.leaveBoard(this.projectId).finally(() => this.realtimeService.disconnect());
    }

    private connectRealtime(): void {
        this.realtimeService
            .connect()
            .then(() => this.realtimeService.joinBoard(this.projectId))
            .catch(() =>
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Tiempo real no disponible',
                    detail: 'El tablero funciona igual, pero no veras los cambios de otras sesiones en vivo.'
                })
            );

        this.realtimeSubscriptions.push(
            this.realtimeService.taskCreated$.subscribe((task) => this.applyRemoteTaskCreated(task)),
            this.realtimeService.taskUpdated$.subscribe((task) => this.replaceTaskInPlace(task)),
            this.realtimeService.taskDeleted$.subscribe((payload) => this.applyRemoteTaskDeleted(payload)),
            this.realtimeService.taskMoved$.subscribe((payload) => this.applyRemoteTaskMoved(payload)),
            this.realtimeService.connectedUsers$.subscribe((users) => (this.connectedUsers = users))
        );
    }

    // Replican la misma mutacion optimista que onDrop/deleteTask (ADR §15.5); el emisor
    // no recibe su propio evento (ADR §15.3), no hace falta distinguir propio/ajeno.
    private applyRemoteTaskCreated(task: BoardTask): void {
        const column = this.board?.columns.find((c) => c.id === task.columnId);
        if (!column || column.tasks.some((t) => t.id === task.id)) {
            return;
        }
        column.tasks.push(task);
        column.tasks.sort((a, b) => (a.order < b.order ? -1 : a.order > b.order ? 1 : 0));
    }

    private applyRemoteTaskDeleted(payload: TaskDeletedPayload): void {
        const column = this.board?.columns.find((c) => c.id === payload.columnId);
        if (column) {
            column.tasks = column.tasks.filter((t) => t.id !== payload.taskId);
        }
    }

    private applyRemoteTaskMoved(payload: TaskMovedPayload): void {
        if (!this.board) {
            return;
        }

        for (const column of this.board.columns) {
            const index = column.tasks.findIndex((t) => t.id === payload.task.id);
            if (index >= 0) {
                column.tasks.splice(index, 1);
                break;
            }
        }

        const targetColumn = this.board.columns.find((c) => c.id === payload.task.columnId);
        if (targetColumn) {
            const insertAt = Math.min(payload.targetIndex, targetColumn.tasks.length);
            targetColumn.tasks.splice(insertAt, 0, payload.task);
        }
    }

    loadBoard(): void {
        this.loading = true;
        this.boardService.getByProject(this.projectId).subscribe({
            next: (board) => {
                this.board = board;
                this.loading = false;
            },
            error: () => {
                this.loading = false;
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudo cargar el tablero' });
            }
        });
    }

    // Filtro client-side (deseable sección 7): no muta board.columns[].tasks, para no
    // romper el tiempo real que sí muta esos arrays.
    get isFiltering(): boolean {
        return this.filterAssigneeId !== null || this.filterPriority !== null || this.searchText.trim().length > 0;
    }

    getVisibleTasks(column: BoardColumn): BoardTask[] {
        const search = this.searchText.trim().toLowerCase();

        return column.tasks.filter(
            (task) =>
                (this.filterAssigneeId === null || task.assigneeId === this.filterAssigneeId) &&
                (this.filterPriority === null || task.priority === this.filterPriority) &&
                (search === '' || task.title.toLowerCase().includes(search) || task.description.toLowerCase().includes(search))
        );
    }

    clearFilters(): void {
        this.filterAssigneeId = null;
        this.filterPriority = null;
        this.searchText = '';
    }

    // <a download> efimero en vez de navegar a la URL (issue #19): asi el request lleva
    // el header Authorization, el JWT no viaja en la URL. Manda el filtro activo del tablero.
    downloadReport(format: ReportFormat): void {
        this.downloadingReport = format;
        const filters = { assigneeId: this.filterAssigneeId, priority: this.filterPriority };
        this.reportService.download(this.projectId, format, filters).subscribe({
            next: ({ blob, fileName }) => {
                this.downloadingReport = null;
                this.triggerDownload(blob, fileName);
            },
            error: () => {
                this.downloadingReport = null;
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudo descargar el reporte' });
            }
        });
    }

    private triggerDownload(blob: Blob, fileName: string): void {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
    }

    trackByColumnId(_index: number, column: BoardColumn): string {
        return column.id;
    }

    trackByTaskId(_index: number, task: BoardTask): string {
        return task.id;
    }

    // Actualizacion optimista con reversion visible si el servidor falla (sección 6.6).
    onDrop(event: CdkDragDrop<BoardTask[]>, targetColumn: BoardColumn): void {
        if (!this.board) {
            return;
        }

        const task = event.item.data as BoardTask;
        const snapshot = this.board.columns.map((c) => ({ ...c, tasks: [...c.tasks] }));

        if (event.previousContainer === event.container) {
            if (event.previousIndex === event.currentIndex) {
                return;
            }
            moveItemInArray(targetColumn.tasks, event.previousIndex, event.currentIndex);
        } else {
            const sourceColumn = this.board.columns.find((c) => c.id === event.previousContainer.id);
            if (!sourceColumn) {
                return;
            }
            transferArrayItem(sourceColumn.tasks, targetColumn.tasks, event.previousIndex, event.currentIndex);
        }

        this.taskService.move(task.id, { targetColumnId: targetColumn.id, targetIndex: event.currentIndex }).subscribe({
            next: (updated) => this.replaceTaskInPlace(updated),
            error: () => {
                if (this.board) {
                    this.board = { ...this.board, columns: snapshot };
                }
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'No se pudo mover la tarea, se revirtió el cambio.'
                });
            }
        });
    }

    openCreateTask(columnId: string): void {
        this.editingTask = null;
        this.targetColumnId = columnId;
        this.taskFormVisible = true;
    }

    openEditTask(task: BoardTask): void {
        this.editingTask = task;
        this.targetColumnId = null;
        this.taskFormVisible = true;
    }

    confirmDelete(task: BoardTask): void {
        this.confirmationService.confirm({
            message: `¿Eliminar la tarea "${task.title}"?`,
            header: 'Confirmar eliminación',
            icon: 'pi pi-exclamation-triangle',
            accept: () => this.deleteTask(task)
        });
    }

    private deleteTask(task: BoardTask): void {
        this.taskService.delete(task.id).subscribe({
            next: () => {
                const column = this.board?.columns.find((c) => c.id === task.columnId);
                if (column) {
                    column.tasks = column.tasks.filter((t) => t.id !== task.id);
                }
                this.messageService.add({ severity: 'success', summary: 'Éxito', detail: 'Tarea eliminada' });
            },
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudo eliminar la tarea' })
        });
    }

    private replaceTaskInPlace(updated: BoardTask): void {
        const column = this.board?.columns.find((c) => c.id === updated.columnId);
        const index = column?.tasks.findIndex((t) => t.id === updated.id) ?? -1;

        if (column && index >= 0) {
            column.tasks[index] = updated;
        }
    }
}
