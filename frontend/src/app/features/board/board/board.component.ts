import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Board, BoardColumn } from '../models/board.model';
import { BoardTask, TASK_PRIORITY_LABELS, TASK_PRIORITY_SEVERITY } from '../models/task.model';
import { BoardService } from '../services/board.service';
import { TaskService } from '../services/task.service';

@Component({
    selector: 'app-board',
    templateUrl: './board.component.html',
    styleUrls: ['./board.component.scss'],
    providers: [ConfirmationService, MessageService]
})
export class BoardComponent implements OnInit {
    board: Board | null = null;
    loading = false;

    taskFormVisible = false;
    editingTask: BoardTask | null = null;
    targetColumnId: string | null = null;

    readonly priorityLabels = TASK_PRIORITY_LABELS;
    readonly prioritySeverity = TASK_PRIORITY_SEVERITY;

    private projectId!: string;

    constructor(
        private route: ActivatedRoute,
        private boardService: BoardService,
        private taskService: TaskService,
        private confirmationService: ConfirmationService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.projectId = this.route.snapshot.paramMap.get('projectId')!;
        this.loadBoard();
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

    trackByColumnId(_index: number, column: BoardColumn): string {
        return column.id;
    }

    trackByTaskId(_index: number, task: BoardTask): string {
        return task.id;
    }

    // Traslado entre columnas y reordenamiento dentro de una misma columna (seccion 6.6):
    // se aplica el cambio en el array local antes de la respuesta del servidor
    // (actualizacion optimista) y, si el servidor responde con error, se restaura la
    // instantanea previa al arrastre -- reversion visible exigida por el enunciado.
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
