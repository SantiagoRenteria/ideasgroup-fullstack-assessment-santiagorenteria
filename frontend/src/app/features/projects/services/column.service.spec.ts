import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { ColumnService } from './column.service';

describe('ColumnService', () => {
    let service: ColumnService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [ColumnService]
        });
        service = TestBed.inject(ColumnService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => httpMock.verify());

    it('listByProject hace GET anidado bajo el proyecto', () => {
        service.listByProject('proj-1').subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/projects/proj-1/columns`);
        expect(req.request.method).toBe('GET');
        req.flush([]);
    });

    it('create hace POST anidado bajo el proyecto con name y order', () => {
        service.create('proj-1', { name: 'Por hacer', order: 0 }).subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/projects/proj-1/columns`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ name: 'Por hacer', order: 0 });
        req.flush({ id: 'col-1', projectId: 'proj-1', name: 'Por hacer', order: 0 });
    });

    it('update hace PUT al recurso columns/{id}, no anidado bajo el proyecto', () => {
        service.update('col-1', { name: 'En progreso', order: 1 }).subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/columns/col-1`);
        expect(req.request.method).toBe('PUT');
        req.flush({ id: 'col-1', projectId: 'proj-1', name: 'En progreso', order: 1 });
    });

    it('delete hace DELETE al recurso columns/{id}', () => {
        service.delete('col-1').subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/columns/col-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});
