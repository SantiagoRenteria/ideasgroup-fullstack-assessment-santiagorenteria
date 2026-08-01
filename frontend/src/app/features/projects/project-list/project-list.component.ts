import { Component, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { TableLazyLoadEvent } from 'primeng/table';
import { PROJECT_STATUS_LABELS, Project, ProjectStatus } from '../models/project.model';
import { ProjectService } from '../services/project.service';

@Component({
    selector: 'app-project-list',
    templateUrl: './project-list.component.html',
    providers: [ConfirmationService, MessageService]
})
export class ProjectListComponent implements OnInit {
    projects: Project[] = [];
    totalRecords = 0;
    loading = false;
    page = 1;
    pageSize = 10;
    nameFilter = '';
    statusFilter: ProjectStatus | null = null;

    formVisible = false;
    editingProject: Project | null = null;

    columnsVisible = false;
    columnsProjectId: string | null = null;
    columnsProjectName = '';

    readonly statusLabels = PROJECT_STATUS_LABELS;
    readonly statusOptions = [
        { label: 'Planificado', value: ProjectStatus.Planned },
        { label: 'En progreso', value: ProjectStatus.InProgress },
        { label: 'Completado', value: ProjectStatus.Completed },
        { label: 'Cancelado', value: ProjectStatus.Cancelled }
    ];

    constructor(
        private projectService: ProjectService,
        private confirmationService: ConfirmationService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.load();
    }

    onLazyLoad(event: TableLazyLoadEvent): void {
        const rows = event.rows ?? this.pageSize;
        this.pageSize = rows;
        this.page = Math.floor((event.first ?? 0) / rows) + 1;
        this.load();
    }

    onFilterChange(): void {
        this.page = 1;
        this.load();
    }

    load(): void {
        this.loading = true;
        this.projectService
            .list({ page: this.page, pageSize: this.pageSize, name: this.nameFilter || undefined, status: this.statusFilter ?? undefined })
            .subscribe({
                next: (result) => {
                    this.projects = result.items;
                    this.totalRecords = result.totalCount;
                    this.loading = false;
                },
                error: () => {
                    this.loading = false;
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudieron cargar los proyectos' });
                }
            });
    }

    openNew(): void {
        this.editingProject = null;
        this.formVisible = true;
    }

    openEdit(project: Project): void {
        this.editingProject = project;
        this.formVisible = true;
    }

    openColumns(project: Project): void {
        this.columnsProjectId = project.id;
        this.columnsProjectName = project.name;
        this.columnsVisible = true;
    }

    confirmDelete(project: Project): void {
        this.confirmationService.confirm({
            message: `¿Eliminar el proyecto "${project.name}"? Esta acción también elimina sus columnas y tareas.`,
            header: 'Confirmar eliminación',
            icon: 'pi pi-exclamation-triangle',
            accept: () => this.delete(project)
        });
    }

    private delete(project: Project): void {
        this.projectService.delete(project.id).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Éxito', detail: 'Proyecto eliminado' });
                this.load();
            },
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudo eliminar el proyecto' })
        });
    }
}
