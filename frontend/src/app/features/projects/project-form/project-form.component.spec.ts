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
            startDate: new Date('2026-06-30'),
            endDate: new Date('2026-01-01')
        });

        expect(component.form.errors?.['endDateBeforeStartDate']).toBeTrue();
    });

    it('save() con formulario invalido no llama al servicio', () => {
        openDialog(null);

        component.save();

        expect(projectService.create).not.toHaveBeenCalled();
        expect(component.submitted).toBeTrue();
    });

    it('save() en modo creacion con formulario valido llama a create, emite saved y cierra el dialog', () => {
        openDialog(null);
        component.form.patchValue({
            name: 'Migracion ERP',
            description: 'Descripcion',
            startDate: new Date(2026, 0, 1),
            endDate: new Date(2026, 5, 30),
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
            startDate: new Date(2026, 0, 1),
            endDate: new Date(2026, 5, 30)
        });
        projectService.create.and.returnValue(throwError(() => new Error('falla de red')));

        component.save();

        expect(component.saving).toBeFalse();
        expect(component.visible).toBeTrue();
    });
});
