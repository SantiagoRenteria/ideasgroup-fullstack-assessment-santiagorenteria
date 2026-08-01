import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { Project, ProjectStatus } from '../models/project.model';
import { ProjectService } from '../services/project.service';

function endDateNotBeforeStartDate(): ValidatorFn {
    return (group): ValidationErrors | null => {
        const start = group.get('startDate')?.value;
        const end = group.get('endDate')?.value;

        if (!start || !end) {
            return null;
        }

        return new Date(end) < new Date(start) ? { endDateBeforeStartDate: true } : null;
    };
}

function todayWithoutTime(): Date {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return today;
}

// Solo se aplica al crear (ver ngOnChanges): permitir que un proyecto ya iniciado
// mantenga su fecha de inicio original al editarlo, en vez de bloquear la edicion
// de cualquier proyecto cuya fecha de inicio ya haya pasado.
function startDateNotInPast(): ValidatorFn {
    return (control): ValidationErrors | null => {
        if (!control.value) {
            return null;
        }

        return new Date(control.value) < todayWithoutTime() ? { startDateInPast: true } : null;
    };
}

@Component({
    selector: 'app-project-form',
    templateUrl: './project-form.component.html'
})
export class ProjectFormComponent implements OnChanges {
    @Input() visible = false;
    @Input() project: Project | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    submitted = false;
    saving = false;

    readonly statusOptions = [
        { label: 'Planificado', value: ProjectStatus.Planned },
        { label: 'En progreso', value: ProjectStatus.InProgress },
        { label: 'Completado', value: ProjectStatus.Completed },
        { label: 'Cancelado', value: ProjectStatus.Cancelled }
    ];

    form: FormGroup = this.fb.group(
        {
            name: ['', [Validators.required, Validators.maxLength(200)]],
            description: ['', [Validators.required, Validators.maxLength(2000)]],
            startDate: [null as Date | null, Validators.required],
            endDate: [null as Date | null, Validators.required],
            status: [ProjectStatus.Planned, Validators.required]
        },
        { validators: endDateNotBeforeStartDate() }
    );

    constructor(private fb: FormBuilder, private projectService: ProjectService, private messageService: MessageService) {}

    get isEditMode(): boolean {
        return this.project !== null;
    }

    // Propiedad estable (no un getter): [minDate] en p-calendar espera la MISMA
    // referencia de Date entre ciclos de deteccion de cambios. Un getter que hace
    // "new Date()" en cada llamada le devuelve un objeto distinto en cada tick de
    // Angular, y PrimeNG reacciona a eso reprocesando el calendario en cada ciclo --
    // el sintoma visible es que el picker no deja completar la seleccion de una fecha.
    // Sin restriccion en modo edicion: un proyecto ya iniciado conserva su fecha de
    // inicio real, aunque ya haya pasado.
    minStartDate: Date | null = null;

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.submitted = false;
            this.form.reset({
                name: this.project?.name ?? '',
                description: this.project?.description ?? '',
                startDate: this.project ? new Date(this.project.startDate) : null,
                endDate: this.project ? new Date(this.project.endDate) : null,
                status: this.project?.status ?? ProjectStatus.Planned
            });

            this.minStartDate = this.isEditMode ? null : todayWithoutTime();

            const startDateControl = this.form.get('startDate')!;
            startDateControl.setValidators(
                this.isEditMode ? [Validators.required] : [Validators.required, startDateNotInPast()]
            );
            startDateControl.updateValueAndValidity({ emitEvent: false });
        }
    }

    save(): void {
        this.submitted = true;

        if (this.form.invalid) {
            // Antes fallaba en silencio: el unico indicio era el texto rojo bajo el
            // campo, facil de pasar por alto si no esta a la vista. Un toast asegura
            // que siempre haya alguna señal de que "Guardar" no hizo nada.
            this.messageService.add({
                severity: 'warn',
                summary: 'Revisá el formulario',
                detail: 'Hay campos obligatorios o inválidos antes de poder guardar.'
            });
            return;
        }

        this.saving = true;
        const value = this.form.value;
        const request = {
            name: value.name,
            description: value.description,
            startDate: this.toDateOnly(value.startDate),
            endDate: this.toDateOnly(value.endDate),
            status: value.status as ProjectStatus
        };

        const request$ = this.isEditMode
            ? this.projectService.update(this.project!.id, request)
            : this.projectService.create(request);

        request$.subscribe({
            next: () => {
                this.saving = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Éxito',
                    detail: this.isEditMode ? 'Proyecto actualizado' : 'Proyecto creado'
                });
                this.saved.emit();
                this.close();
            },
            error: (err: HttpErrorResponse) => {
                this.saving = false;
                const detail = err.error?.error ?? 'No se pudo guardar el proyecto';
                this.messageService.add({ severity: 'error', summary: 'Error', detail });
            }
        });
    }

    close(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }

    private toDateOnly(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}
