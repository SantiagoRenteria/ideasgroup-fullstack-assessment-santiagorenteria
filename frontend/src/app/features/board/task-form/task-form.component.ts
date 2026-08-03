import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { AppUser } from '../models/app-user.model';
import { BoardTask, TASK_PRIORITY_OPTIONS, TaskPriority } from '../models/task.model';
import { TaskService } from '../services/task.service';
import { UserService } from '../services/user.service';

@Component({
    selector: 'app-task-form',
    templateUrl: './task-form.component.html'
})
export class TaskFormComponent implements OnInit, OnChanges {
    @Input() visible = false;
    @Input() task: BoardTask | null = null;
    @Input() columnId: string | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    submitted = false;
    saving = false;
    users: AppUser[] = [];

    readonly priorityOptions = TASK_PRIORITY_OPTIONS;

    form: FormGroup = this.fb.group({
        title: ['', [Validators.required, Validators.maxLength(200)]],
        description: ['', [Validators.required, Validators.maxLength(2000)]],
        priority: [TaskPriority.Medium, Validators.required],
        assigneeId: [null as string | null]
    });

    constructor(private fb: FormBuilder, private taskService: TaskService, private userService: UserService, private messageService: MessageService) {}

    get isEditMode(): boolean {
        return this.task !== null;
    }

    ngOnInit(): void {
        this.userService.listAll().subscribe({
            next: (users) => (this.users = users),
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudieron cargar los responsables' })
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.submitted = false;
            this.form.reset({
                title: this.task?.title ?? '',
                description: this.task?.description ?? '',
                priority: this.task?.priority ?? TaskPriority.Medium,
                assigneeId: this.task?.assigneeId ?? null
            });
        }
    }

    save(): void {
        this.submitted = true;

        if (this.form.invalid) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Revisá el formulario',
                detail: 'Hay campos obligatorios o inválidos antes de poder guardar.'
            });
            return;
        }

        this.saving = true;
        const value = this.form.value;

        const request$ = this.isEditMode
            ? this.taskService.update(this.task!.id, {
                  title: value.title,
                  description: value.description,
                  priority: value.priority,
                  assigneeId: value.assigneeId
              })
            : this.taskService.create({
                  columnId: this.columnId!,
                  title: value.title,
                  description: value.description,
                  priority: value.priority,
                  assigneeId: value.assigneeId
              });

        request$.subscribe({
            next: () => {
                this.saving = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Éxito',
                    detail: this.isEditMode ? 'Tarea actualizada' : 'Tarea creada'
                });
                this.saved.emit();
                this.close();
            },
            error: (err: HttpErrorResponse) => {
                this.saving = false;
                const detail = err.error?.error ?? 'No se pudo guardar la tarea';
                this.messageService.add({ severity: 'error', summary: 'Error', detail });
            }
        });
    }

    close(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }
}
