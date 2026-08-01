import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { TaskPriority } from '../models/task.model';
import { TaskService } from './task.service';

describe('TaskService', () => {
    let service: TaskService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [TaskService]
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
});
