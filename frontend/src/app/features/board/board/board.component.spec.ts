import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { HttpErrorResponse } from '@angular/common/http';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Subject, of, throwError } from 'rxjs';
import { AppUser } from '../models/app-user.model';
import { Board } from '../models/board.model';
import { BoardTask, TaskPriority } from '../models/task.model';
import { BoardService } from '../services/board.service';
import { RealtimeBoardService, TaskDeletedPayload, TaskMovedPayload } from '../services/realtime-board.service';
import { ReportService } from '../services/report.service';
import { TaskService } from '../services/task.service';
import { UserService } from '../services/user.service';
import { BoardComponent } from './board.component';

describe('BoardComponent', () => {
    let component: BoardComponent;
    let fixture: ComponentFixture<BoardComponent>;
    let boardService: jasmine.SpyObj<BoardService>;
    let taskService: jasmine.SpyObj<TaskService>;
    let reportService: jasmine.SpyObj<ReportService>;
    let userService: jasmine.SpyObj<UserService>;
    let router: jasmine.SpyObj<Router>;
    let taskCreated$: Subject<BoardTask>;
    let taskUpdated$: Subject<BoardTask>;
    let taskDeleted$: Subject<TaskDeletedPayload>;
    let taskMoved$: Subject<TaskMovedPayload>;
    let connectedUsers$: Subject<string[]>;

    function createTask(
        id: string,
        columnId: string,
        order: string,
        priority: TaskPriority = TaskPriority.Medium,
        assigneeId: string | null = null
    ): BoardTask {
        return { id, columnId, title: `Tarea ${id}`, description: 'Desc', priority, assigneeId, order, createdAt: '2026-07-01T00:00:00Z' };
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
        reportService = jasmine.createSpyObj('ReportService', ['download']);
        userService = jasmine.createSpyObj('UserService', ['listAll']);
        router = jasmine.createSpyObj('Router', ['navigate']);
        boardService.getByProject.and.returnValue(of(buildBoard()));
        userService.listAll.and.returnValue(of([]));

        taskCreated$ = new Subject<BoardTask>();
        taskUpdated$ = new Subject<BoardTask>();
        taskDeleted$ = new Subject<TaskDeletedPayload>();
        taskMoved$ = new Subject<TaskMovedPayload>();
        connectedUsers$ = new Subject<string[]>();
        const realtimeService = {
            connect: () => Promise.resolve(),
            joinBoard: () => Promise.resolve(),
            leaveBoard: () => Promise.resolve(),
            disconnect: () => Promise.resolve(),
            taskCreated$,
            taskUpdated$,
            taskDeleted$,
            taskMoved$,
            connectedUsers$
        };

        await TestBed.configureTestingModule({
            declarations: [BoardComponent],
            providers: [
                { provide: BoardService, useValue: boardService },
                { provide: TaskService, useValue: taskService },
                { provide: ReportService, useValue: reportService },
                { provide: UserService, useValue: userService },
                { provide: RealtimeBoardService, useValue: realtimeService },
                { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ projectId: 'proj-1' }) } } },
                { provide: Router, useValue: router },
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

    it('loadBoard saca al usuario del tablero si el proyecto ya no existe (404), sin dejarlo en una vista fantasma', () => {
        // Otra sesion borro el proyecto mientras esta lo tenia abierto: las columnas que
        // quedarian en pantalla ya rechazan toda mutacion con 404 (ver ADR §27.2).
        boardService.getByProject.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
        // Mismo comentario que en downloadReport: instancia unica provista por TestBed.
        const messageService = fixture.debugElement.injector.get(MessageService);
        spyOn(messageService, 'add');

        fixture.detectChanges();

        expect(component.board).toBeNull();
        expect(component.loading).toBeFalse();
        expect(router.navigate).toHaveBeenCalledWith(['/projects']);
        expect(messageService.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: 'warn' }));
    });

    it('loadBoard ante un error que no es 404 avisa pero deja al usuario donde esta', () => {
        // Un 500 o una caida de red son transitorios: expulsar al usuario del tablero seria
        // peor que dejarlo reintentar.
        boardService.getByProject.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
        const messageService = fixture.debugElement.injector.get(MessageService);
        spyOn(messageService, 'add');

        fixture.detectChanges();

        expect(component.loading).toBeFalse();
        expect(router.navigate).not.toHaveBeenCalled();
        expect(messageService.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: 'error' }));
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
        // ConfirmationService/MessageService ahora se proveen una sola vez a nivel de app
        // (ver revision de arquitectura frontend, shared/); TestBed los provee arriba, asi que
        // esta es la misma instancia inyectada en BoardComponent.
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

    // Seccion 6.7: eventos que llegan de otra sesion del mismo tablero deben actualizar el
    // estado local sin recarga manual.
    it('un TaskCreated recibido por el canal agrega la tarea a la columna correspondiente', () => {
        fixture.detectChanges();

        taskCreated$.next(createTask('t4', 'col-2', 'b'));

        expect(component.board!.columns[1].tasks.map((t) => t.id)).toEqual(['t3', 't4']);
    });

    it('un TaskUpdated recibido por el canal reemplaza la tarea en su columna', () => {
        fixture.detectChanges();

        const updated = { ...createTask('t1', 'col-1', 'a'), title: 'Titulo editado por otra sesion' };
        taskUpdated$.next(updated);

        expect(component.board!.columns[0].tasks[0].title).toBe('Titulo editado por otra sesion');
    });

    it('un TaskDeleted recibido por el canal quita la tarea de su columna', () => {
        fixture.detectChanges();

        taskDeleted$.next({ taskId: 't1', columnId: 'col-1' });

        expect(component.board!.columns[0].tasks.map((t) => t.id)).toEqual(['t2']);
    });

    it('un TaskMoved recibido por el canal traslada la tarea entre columnas en el indice indicado', () => {
        fixture.detectChanges();

        const movedTask = createTask('t1', 'col-2', 'ab');
        taskMoved$.next({ task: movedTask, targetIndex: 0 });

        expect(component.board!.columns[0].tasks.map((t) => t.id)).toEqual(['t2']);
        expect(component.board!.columns[1].tasks.map((t) => t.id)).toEqual(['t1', 't3']);
    });

    it('downloadReport pide el reporte en el formato solicitado y dispara la descarga', () => {
        fixture.detectChanges();
        const blob = new Blob(['contenido']);
        reportService.download.and.returnValue(of({ blob, fileName: 'reporte-demo.pdf' }));
        spyOn(component as any, 'triggerDownload');

        component.downloadReport('pdf');

        expect(reportService.download).toHaveBeenCalledWith('proj-1', 'pdf', { assigneeId: null, priority: null });
        expect((component as any).triggerDownload).toHaveBeenCalledWith(blob, 'reporte-demo.pdf');
        expect(component.downloadingReport).toBeNull();
    });

    it('downloadReport manda el filtro activo del tablero al pedir el reporte (seccion 7)', () => {
        fixture.detectChanges();
        reportService.download.and.returnValue(of({ blob: new Blob(), fileName: 'reporte.pdf' }));
        spyOn(component as any, 'triggerDownload');
        component.filterAssigneeId = 'user-1';
        component.filterPriority = TaskPriority.High;

        component.downloadReport('pdf');

        expect(reportService.download).toHaveBeenCalledWith('proj-1', 'pdf', { assigneeId: 'user-1', priority: TaskPriority.High });
    });

    it('downloadReport muestra un error si la descarga falla, sin dejar el boton en estado de carga', () => {
        fixture.detectChanges();
        reportService.download.and.returnValue(throwError(() => new Error('fallo de red')));
        // Mismo comentario que en confirmDelete: instancia unica provista por TestBed.
        const messageService = fixture.debugElement.injector.get(MessageService);
        spyOn(messageService, 'add');

        component.downloadReport('excel');

        expect(component.downloadingReport).toBeNull();
        expect(messageService.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: 'error' }));
    });

    it('getVisibleTasks solo devuelve las tareas que matchean el responsable filtrado, sin mutar el tablero real', () => {
        fixture.detectChanges();
        const column = component.board!.columns[0];
        column.tasks = [
            createTask('t1', column.id, 'a', TaskPriority.Medium, 'user-1'),
            createTask('t2', column.id, 'b', TaskPriority.Medium, 'user-2')
        ];
        component.filterAssigneeId = 'user-1';

        const visible = component.getVisibleTasks(column);

        expect(visible.map((t) => t.id)).toEqual(['t1']);
        expect(column.tasks.length).toBe(2); // el estado real del tablero no se toca
    });

    it('getVisibleTasks solo devuelve las tareas que matchean la prioridad filtrada', () => {
        fixture.detectChanges();
        const column = component.board!.columns[0];
        column.tasks = [createTask('t1', column.id, 'a', TaskPriority.Urgent), createTask('t2', column.id, 'b', TaskPriority.Low)];
        component.filterPriority = TaskPriority.Urgent;

        const visible = component.getVisibleTasks(column);

        expect(visible.map((t) => t.id)).toEqual(['t1']);
    });

    it('isFiltering es true solo cuando hay al menos un filtro activo', () => {
        expect(component.isFiltering).toBeFalse();

        component.filterAssigneeId = 'user-1';
        expect(component.isFiltering).toBeTrue();

        component.filterAssigneeId = null;
        component.filterPriority = TaskPriority.High;
        expect(component.isFiltering).toBeTrue();

        component.filterPriority = null;
        component.searchText = 'wireframes';
        expect(component.isFiltering).toBeTrue();

        component.searchText = '   ';
        expect(component.isFiltering).toBeFalse();
    });

    it('clearFilters limpia responsable, prioridad y busqueda', () => {
        component.filterAssigneeId = 'user-1';
        component.filterPriority = TaskPriority.High;
        component.searchText = 'wireframes';

        component.clearFilters();

        expect(component.filterAssigneeId).toBeNull();
        expect(component.filterPriority).toBeNull();
        expect(component.searchText).toBe('');
        expect(component.isFiltering).toBeFalse();
    });

    it('getVisibleTasks (deseable seccion 7) filtra por texto en titulo o descripcion, sin distinguir mayusculas', () => {
        fixture.detectChanges();
        const column = component.board!.columns[0];
        column.tasks = [
            { ...createTask('t1', column.id, 'a'), title: 'Diseñar wireframes', description: 'Bocetos iniciales' },
            { ...createTask('t2', column.id, 'b'), title: 'Definir alcance', description: 'incluye WIREFRAMES tambien' },
            { ...createTask('t3', column.id, 'c'), title: 'Otra tarea', description: 'sin relacion' }
        ];
        component.searchText = 'WireFrames';

        const visible = component.getVisibleTasks(column);

        expect(visible.map((t) => t.id)).toEqual(['t1', 't2']);
    });

    it('getVisibleTasks combina busqueda de texto con los demas filtros', () => {
        fixture.detectChanges();
        const column = component.board!.columns[0];
        column.tasks = [
            { ...createTask('t1', column.id, 'a', TaskPriority.High, 'user-1'), title: 'Diseñar wireframes', description: '' },
            { ...createTask('t2', column.id, 'b', TaskPriority.Low, 'user-1'), title: 'Diseñar wireframes', description: '' }
        ];
        component.searchText = 'wireframes';
        component.filterPriority = TaskPriority.High;

        const visible = component.getVisibleTasks(column);

        expect(visible.map((t) => t.id)).toEqual(['t1']);
    });

    it('un BoardPresenceChanged recibido por el canal actualiza la lista de conectados', () => {
        fixture.detectChanges();

        connectedUsers$.next(['Administrador', 'Evaluador']);

        expect(component.connectedUsers).toEqual(['Administrador', 'Evaluador']);
    });

    it('ngOnDestroy deja el tablero y cierra la conexion de tiempo real', () => {
        fixture.detectChanges();
        const realtimeService = TestBed.inject(RealtimeBoardService);
        spyOn(realtimeService, 'leaveBoard').and.returnValue(Promise.resolve());
        spyOn(realtimeService, 'disconnect').and.returnValue(Promise.resolve());

        component.ngOnDestroy();

        expect(realtimeService.leaveBoard).toHaveBeenCalledWith('proj-1');
    });
});
