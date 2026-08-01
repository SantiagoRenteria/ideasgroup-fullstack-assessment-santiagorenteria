import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';
import { ProjectStatus } from '../models/project.model';
import { ProjectService } from '../services/project.service';
import { ProjectFormComponent } from './project-form.component';

describe('ProjectFormComponent', () => {
    let component: ProjectFormComponent;
    let fixture: ComponentFixture<ProjectFormComponent>;
    let projectService: jasmine.SpyObj<ProjectService>;

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    const inSixMonths = new Date(today);
    inSixMonths.setMonth(inSixMonths.getMonth() + 6);

    beforeEach(async () => {
        projectService = jasmine.createSpyObj('ProjectService', ['create', 'update']);

        await TestBed.configureTestingModule({
            declarations: [ProjectFormComponent],
            imports: [ReactiveFormsModule],
            providers: [{ provide: ProjectService, useValue: projectService }, MessageService],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(ProjectFormComponent);
        component = fixture.componentInstance;
    });

    function openDialog(project: ProjectFormComponent['project'] = null) {
        component.project = project;
        component.visible = true;
        component.ngOnChanges({ visible: {} as never });
    }

    it('sin proyecto, arranca en modo creacion con el formulario reseteado', () => {
        openDialog(null);

        expect(component.isEditMode).toBeFalse();
        expect(component.form.value.name).toBe('');
        expect(component.form.value.status).toBe(ProjectStatus.Planned);
    });

    it('con un proyecto, arranca en modo edicion con el formulario precargado', () => {
        openDialog({
            id: 'p1',
            name: 'Migracion ERP',
            description: 'Desc',
            startDate: '2026-01-01',
            endDate: '2026-06-30',
            status: ProjectStatus.Planned
        });

        expect(component.isEditMode).toBeTrue();
        expect(component.form.value.name).toBe('Migracion ERP');
    });

    it('fecha de fin anterior a fecha de inicio invalida el formulario', () => {
        openDialog(null);
        component.form.patchValue({
            name: 'Nombre',
            description: 'Descripcion',
            startDate: inSixMonths,
            endDate: today
        });

        expect(component.form.errors?.['endDateBeforeStartDate']).toBeTrue();
    });

    it('al crear, una fecha de inicio anterior a hoy invalida el campo', () => {
        openDialog(null);
        component.form.patchValue({ startDate: yesterday });

        expect(component.form.get('startDate')?.errors?.['startDateInPast']).toBeTrue();
    });

    it('al crear, minStartDate es hoy (el date picker no deja elegir fechas pasadas)', () => {
        openDialog(null);

        expect(component.minStartDate).toEqual(today);
    });

    it('al editar, una fecha de inicio pasada no invalida el campo ni restringe minStartDate', () => {
        openDialog({
            id: 'p1',
            name: 'Proyecto en curso',
            description: 'Desc',
            startDate: '2020-01-01',
            endDate: '2026-06-30',
            status: ProjectStatus.InProgress
        });

        expect(component.form.get('startDate')?.errors?.['startDateInPast']).toBeFalsy();
        expect(component.minStartDate).toBeNull();
    });

    it('save() con formulario invalido no llama al servicio y avisa con un toast', () => {
        openDialog(null);
        const messageService = TestBed.inject(MessageService);
        spyOn(messageService, 'add');

        component.save();

        expect(projectService.create).not.toHaveBeenCalled();
        expect(component.submitted).toBeTrue();
        expect(messageService.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: 'warn' }));
    });

    it('save() en modo creacion con formulario valido llama a create, emite saved y cierra el dialog', () => {
        openDialog(null);
        component.form.patchValue({
            name: 'Migracion ERP',
            description: 'Descripcion',
            startDate: tomorrow,
            endDate: inSixMonths,
            status: ProjectStatus.Planned
        });
        projectService.create.and.returnValue(
            of({ id: 'p1', name: 'Migracion ERP', description: 'Descripcion', startDate: '2026-01-01', endDate: '2026-06-30', status: ProjectStatus.Planned })
        );
        spyOn(component.saved, 'emit');

        component.save();

        expect(projectService.create).toHaveBeenCalled();
        expect(component.saved.emit).toHaveBeenCalled();
        expect(component.visible).toBeFalse();
    });

    it('save() en modo edicion llama a update con el id del proyecto', () => {
        openDialog({
            id: 'p1',
            name: 'Nombre',
            description: 'Desc',
            startDate: '2026-01-01',
            endDate: '2026-06-30',
            status: ProjectStatus.Planned
        });
        projectService.update.and.returnValue(
            of({ id: 'p1', name: 'Nombre', description: 'Desc', startDate: '2026-01-01', endDate: '2026-06-30', status: ProjectStatus.Planned })
        );

        component.save();

        expect(projectService.update).toHaveBeenCalledWith('p1', jasmine.any(Object));
    });

    it('si el servicio falla, no cierra el dialog y apaga el spinner de guardado', () => {
        openDialog(null);
        component.form.patchValue({
            name: 'Migracion ERP',
            description: 'Descripcion',
            startDate: tomorrow,
            endDate: inSixMonths
        });
        projectService.create.and.returnValue(throwError(() => new Error('falla de red')));

        component.save();

        expect(component.saving).toBeFalse();
        expect(component.visible).toBeTrue();
    });

    it('si el servidor responde con un mensaje de negocio (ej. nombre duplicado), se muestra tal cual', () => {
        openDialog(null);
        component.form.patchValue({
            name: 'Migracion ERP',
            description: 'Descripcion',
            startDate: tomorrow,
            endDate: inSixMonths
        });
        const messageService = TestBed.inject(MessageService);
        spyOn(messageService, 'add');
        projectService.create.and.returnValue(
            throwError(() => ({ error: { error: 'Ya existe un proyecto con este nombre.' } }))
        );

        component.save();

        expect(messageService.add).toHaveBeenCalledWith(
            jasmine.objectContaining({ detail: 'Ya existe un proyecto con este nombre.' })
        );
    });
});
