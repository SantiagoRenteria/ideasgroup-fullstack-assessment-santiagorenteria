import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { ReportService } from './report.service';

describe('ReportService', () => {
    let service: ReportService;
    let httpMock: HttpTestingController;
    const baseUrl = `${environment.apiUrl}/projects`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [ReportService]
        });
        service = TestBed.inject(ReportService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => httpMock.verify());

    it('download pide el blob con el formato correcto como query param', () => {
        service.download('proj-1', 'pdf').subscribe();

        const req = httpMock.expectOne((r) => r.url === `${baseUrl}/proj-1/report`);
        expect(req.request.params.get('format')).toBe('pdf');
        expect(req.request.responseType).toBe('blob');
        req.flush(new Blob(['contenido']), { headers: { 'content-disposition': 'attachment; filename=reporte-demo-2026-08-02.pdf' } });
    });

    it('download extrae el nombre de archivo del header Content-Disposition', (done) => {
        service.download('proj-1', 'pdf').subscribe((result) => {
            expect(result.fileName).toBe('reporte-demo-2026-08-02.pdf');
            expect(result.blob.size).toBeGreaterThan(0);
            done();
        });

        const req = httpMock.expectOne((r) => r.url === `${baseUrl}/proj-1/report`);
        req.flush(new Blob(['contenido']), { headers: { 'content-disposition': 'attachment; filename=reporte-demo-2026-08-02.pdf' } });
    });

    it('download usa un nombre por defecto si el servidor no envia Content-Disposition', (done) => {
        service.download('proj-1', 'excel').subscribe((result) => {
            expect(result.fileName).toBe('reporte.xlsx');
            done();
        });

        const req = httpMock.expectOne((r) => r.url === `${baseUrl}/proj-1/report`);
        req.flush(new Blob(['contenido']));
    });
});
