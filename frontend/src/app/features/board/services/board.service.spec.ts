import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { BoardService } from './board.service';

describe('BoardService', () => {
    let service: BoardService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [BoardService]
        });
        service = TestBed.inject(BoardService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => httpMock.verify());

    it('getByProject hace GET al endpoint agregado del tablero', () => {
        service.getByProject('proj-1').subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/projects/proj-1/board`);
        expect(req.request.method).toBe('GET');
        req.flush({ projectId: 'proj-1', projectName: 'Demo', columns: [] });
    });
});
