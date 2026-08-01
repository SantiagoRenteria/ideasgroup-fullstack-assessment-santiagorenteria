import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { UserService } from './user.service';

describe('UserService', () => {
    let service: UserService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [UserService]
        });
        service = TestBed.inject(UserService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => httpMock.verify());

    it('listAll hace GET a /users', () => {
        service.listAll().subscribe();

        const req = httpMock.expectOne(`${environment.apiUrl}/users`);
        expect(req.request.method).toBe('GET');
        req.flush([]);
    });
});
