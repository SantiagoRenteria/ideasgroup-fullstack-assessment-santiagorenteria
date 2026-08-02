import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ProjectColumn } from '../models/column.model';
import { ColumnService } from '../services/column.service';

@Component({
    selector: 'app-project-columns',
    templateUrl: './project-columns.component.html'
})
export class ProjectColumnsComponent implements OnChanges {
    @Input() visible = false;
    @Input() projectId: string | null = null;
    @Input() projectName = '';
    @Output() visibleChange = new EventEmitter<boolean>();

    columns: ProjectColumn[] = [];
    loading = false;
    newColumnName = '';
    editingId: string | null = null;
    editingName = '';

    constructor(private columnService: ColumnService, private messageService: MessageService) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.projectId) {
            this.reload();
        }
    }

    reload(): void {
        if (!this.projectId) {
            return;
        }

        this.loading = true;
        this.columnService.listByProject(this.projectId).subscribe({
            next: (columns) => {
                this.columns = columns;
                this.loading = false;
            },
            error: () => {
                this.loading = false;
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudieron cargar las columnas' });
            }
        });
    }

    add(): void {
        const name = this.newColumnName.trim();

        if (!name || !this.projectId) {
            return;
        }

        this.columnService.create(this.projectId, { name, order: this.columns.length }).subscribe({
            next: (column) => {
                this.columns = [...this.columns, column];
                this.newColumnName = '';
            },
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudo crear la columna' })
        });
    }

    startEdit(column: ProjectColumn): void {
        this.editingId = column.id;
        this.editingName = column.name;
    }

    cancelEdit(): void {
        this.editingId = null;
        this.editingName = '';
    }

    confirmEdit(column: ProjectColumn): void {
        const name = this.editingName.trim();

        if (!name) {
            return;
        }

        this.columnService.update(column.id, { name, order: column.order }).subscribe({
            next: (updated) => {
                this.columns = this.columns.map((c) => (c.id === updated.id ? updated : c));
                this.cancelEdit();
            },
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudo renombrar la columna' })
        });
    }

    moveUp(index: number): void {
        if (index === 0) {
            return;
        }
        this.swap(index, index - 1);
    }

    moveDown(index: number): void {
        if (index === this.columns.length - 1) {
            return;
        }
        this.swap(index, index + 1);
    }

    delete(column: ProjectColumn): void {
        this.columnService.delete(column.id).subscribe({
            next: () => {
                this.columns = this.columns.filter((c) => c.id !== column.id);
                this.messageService.add({ severity: 'success', summary: 'Éxito', detail: 'Columna eliminada' });
            },
            error: (err) =>
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err.error?.error ?? 'No se pudo eliminar la columna'
                })
        });
    }

    close(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }

    private swap(indexA: number, indexB: number): void {
        const a = this.columns[indexA];
        const b = this.columns[indexB];

        this.columnService.update(a.id, { name: a.name, order: b.order }).subscribe({
            next: (updatedA) => {
                this.columnService.update(b.id, { name: b.name, order: a.order }).subscribe({
                    next: (updatedB) => {
                        const next = [...this.columns];
                        next[indexA] = updatedB;
                        next[indexB] = updatedA;
                        this.columns = next.sort((x, y) => x.order - y.order);
                    },
                    error: () =>
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudo reordenar la columna' })
                });
            },
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudo reordenar la columna' })
        });
    }
}
