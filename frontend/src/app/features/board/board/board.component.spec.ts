import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';
import { Board } from '../models/board.model';
import { BoardTask, TaskPriority } from '../models/task.model';
import { BoardService } from '../services/board.service';
import { TaskService } from '../services/task.service';
import { BoardComponent } from './board.component';

describe('BoardComponent', () => {
    let component: BoardComponent;
    let fixture: ComponentFixture<BoardComponent>;
    let boardService: jasmine.SpyObj<BoardService>;
    let taskService: jasmine.SpyObj<TaskService>;

    function createTask(id: string, columnId: string, order: string): BoardTask {
        return { id, columnId, title: `Tarea ${id}`, description: 'Desc', priority: TaskPriority.Medium, assigneeId: null, order, createdAt: '2026-07-01T00:00:00Z' };
    }

    function buildBoard(): Board {
        return {
            projectId: 'proj-1',
            projectName: 'Demo',
            columns: [
                { id: 'col-1', name: 'Por hacer', order: 0, tasks: [createTask('t1', 'col-1', 'a'), createTask('t2', 'col-1', 'b')] },
                { id: 'col-2', name: 'Hecho', order: 1, tasks: [createTask('t3', 'col-2', 'a')] }
            ]
        };
    }

    beforeEach(async () => {
        boardService = jasmine.createSpyObj('BoardService', ['getByProject']);
        taskService = jasmine.createSpyObj('TaskService', ['move', 'delete']);
        boardService.getByProject.and.returnValue(of(buildBoard()));

        await TestBed.configureTestingModule({
            declarations: [BoardComponent],
            providers: [
                { provide: BoardService, useValue: boardService },
                { provide: TaskService, useValue: taskService },
                { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ projectId: 'proj-1' }) } } },
                ConfirmationService,
                MessageService
            ],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(BoardComponent);
        component = fixture.componentInstance;
    });

    function dropEvent(
        item: BoardTask,
        previousContainerId: string,
        containerId: string,
        previousIndex: number,
        currentIndex: number
    ): CdkDragDrop<BoardTask[]> {
        const sameContainer = previousContainerId === containerId;
        return {
            item: { data: item } as any,
            previousContainer: { id: previousContainerId } as any,
            container: sameContainer ? ({ id: previousContainerId } as any) : ({ id: containerId } as any),
            previousIndex,
            currentIndex,
            isPointerOverContainer: true,
            distance: { x: 0, y: 0 },
            dropPoint: { x: 0, y: 0 },
            event: new MouseEvent('mouseup')
        } as unknown as CdkDragDrop<BoardTask[]>;
    }

    it('ngOnInit carga el tablero del proyecto de la ruta', () => {
        fixture.detectChanges();

        expect(boardService.getByProject).toHaveBeenCalledWith('proj-1');
        expect(component.board?.columns.length).toBe(2);
    });

    it('onDrop reordena dentro de la misma columna de forma optimista y llama a TaskService.move', () => {
        fixture.detectChanges();
        const updated = createTask('t2', 'col-1', 'ab');
        taskService.move.and.returnValue(of(updated));

        const column = component.board!.columns[0];
        const task = column.tasks[1];
        const event = dropEvent(task, 'col-1', 'col-1', 1, 0);

        component.onDrop(event, column);

        expect(taskService.move).toHaveBeenCalledWith('t2', { targetColumnId: 'col-1', targetIndex: 0 });
        expect(component.board!.columns[0].tasks[0].id).toBe('t2');
    });

    it('onDrop entre columnas distintas transfiere la tarea de forma optimista', () => {
        fixture.detectChanges();
        const updated = createTask('t1', 'col-2', 'z');
        taskService.move.and.returnValue(of(updated));

        const sourceColumn = component.board!.columns[0];
        const targetColumn = component.board!.columns[1];
        const task = sourceColumn.tasks[0];
        const event = dropEvent(task, 'col-1', 'col-2', 0, 1);

        component.onDrop(event, targetColumn);

        expect(component.board!.columns[0].tasks.map((t) => t.id)).toEqual(['t2']);
        expect(component.board!.columns[1].tasks.map((t) => t.id)).toEqual(['t3', 't1']);
    });

    it('onDrop revierte el movimiento visible si el servidor responde con error (seccion 6.6)', () => {
        fixture.detectChanges();
        taskService.move.and.returnValue(throwError(() => new Error('fallo de red')));

        const column = component.board!.columns[0];
        const originalOrder = column.tasks.map((t) => t.id);
        const task = column.tasks[0];
        const event = dropEvent(task, 'col-1', 'col-1', 0, 1);

        component.onDrop(event, column);

        expect(component.board!.columns[0].tasks.map((t) => t.id)).toEqual(originalOrder);
    });

    it('confirmDelete, al aceptar, elimina la tarea de la columna local', () => {
        fixture.detectChanges();
        taskService.delete.and.returnValue(of(undefined));
        // ConfirmationService esta declarado como provider a nivel de componente (no del
        // modulo de testing), asi que hay que resolverlo desde el injector del propio
        // componente para interceptar la misma instancia que usa BoardComponent.
        const confirmationService = fixture.debugElement.injector.get(ConfirmationService);
        spyOn(confirmationService, 'confirm').and.callFake((options: any) => {
            options.accept();
            return confirmationService;
        });

        const task = component.board!.columns[0].tasks[0];
        component.confirmDelete(task);

        expect(taskService.delete).toHaveBeenCalledWith(task.id);
        expect(component.board!.columns[0].tasks.find((t) => t.id === task.id)).toBeUndefined();
    });
});
