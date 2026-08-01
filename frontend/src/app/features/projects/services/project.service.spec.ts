import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { ProjectService } from './project.service';
import { ProjectStatus } from '../models/project.model';

describe('ProjectService', () => {
    let service: ProjectService;
    let httpMock: HttpTestingController;
    const baseUrl = `${environment.apiUrl}/projects`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [ProjectService]
        });
        service = TestBed.inject(ProjectService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => httpMock.verify());

    it('list envia page y pageSize como query params obligatorios', () => {
        service.list({ page: 2, pageSize: 10 }).subscribe();

        const req = httpMock.expectOne((r) => r.url === baseUrl);
        expect(req.request.params.get('page')).toBe('2');
        expect(req.request.params.get('pageSize')).toBe('10');
        expect(req.request.params.has('name')).toBeFalse();
        req.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0 });
    });

    it('list agrega name y status solo cuando estan presentes', () => {
        service.list({ page: 1, pageSize: 10, name: 'erp', status: ProjectStatus.Planned }).subscribe();

        const req = httpMock.expectOne((r) => r.url === baseUrl);
        expect(req.request.params.get('name')).toBe('erp');
        expect(req.request.params.get('status')).toBe('Planned');
        req.flush({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });
    });

    it('create hace POST al endpoint de proyectos con el body recibido', () => {
        const request = {
            name: 'Migracion ERP',
            description: 'Descripcion',
            startDate: '2026-01-01',
            endDate: '2026-06-30',
            status: ProjectStatus.Planned
        };

        service.create(request).subscribe();

        const req = httpMock.expectOne(baseUrl);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(request);
        req.flush({ id: '1', ...request });
    });

    it('delete hace DELETE al proyecto por id', () => {
        service.delete('abc-123').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/abc-123`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});
