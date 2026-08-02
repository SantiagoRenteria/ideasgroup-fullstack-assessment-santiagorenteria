import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { TaskPriority } from '../models/task.model';
import { RealtimeBoardService } from './realtime-board.service';
import { TaskService } from './task.service';

describe('TaskService', () => {
    let service: TaskService;
    let httpMock: HttpTestingController;
    let realtimeService: { connectionId: string | null };

    beforeEach(() => {
        realtimeService = { connectionId: null };

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [TaskService, { provide: RealtimeBoardService, useValue: realtimeService }]
        });
        service = TestBed.inject(TaskService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => httpMock.verify());

    it('create hace POST a /tasks', () => {
        service
            .create({ columnId: 'col-1', title: 'Titulo', description: 'Desc', priority: TaskPriority.Medium, assigneeId: null })
            .subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/tasks`);
        expect(req.request.method).toBe('POST');
        req.flush({});
    });

    it('update hace PUT a /tasks/{id}', () => {
        service.update('task-1', { title: 'Titulo', description: 'Desc', priority: TaskPriority.Low, assigneeId: null }).subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/tasks/task-1`);
        expect(req.request.method).toBe('PUT');
        req.flush({});
    });

    it('move hace PATCH a /tasks/{id}/move, no PUT', () => {
        service.move('task-1', { targetColumnId: 'col-2', targetIndex: 0 }).subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/tasks/task-1/move`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual({ targetColumnId: 'col-2', targetIndex: 0 });
        req.flush({});
    });

    it('delete hace DELETE a /tasks/{id}', () => {
        service.delete('task-1').subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/tasks/task-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    // ADR §15.3: el backend excluye a este mismo cliente al notificar por tiempo real
    // usando este header -- solo tiene sentido enviarlo si el canal esta conectado.
    it('sin conexion de tiempo real activa, no envia el header X-Realtime-Connection-Id', () => {
        service.create({ columnId: 'col-1', title: 'Titulo', description: 'Desc', priority: TaskPriority.Medium, assigneeId: null }).subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/tasks`);
        expect(req.request.headers.has('X-Realtime-Connection-Id')).toBeFalse();
        req.flush({});
    });

    it('con una conexion de tiempo real activa, envia su connectionId en X-Realtime-Connection-Id', () => {
        realtimeService.connectionId = 'conn-1';

        service.move('task-1', { targetColumnId: 'col-2', targetIndex: 0 }).subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/tasks/task-1/move`);
        expect(req.request.headers.get('X-Realtime-Connection-Id')).toBe('conn-1');
        req.flush({});
    });
});
