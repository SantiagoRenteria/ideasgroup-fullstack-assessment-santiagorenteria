import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../services/auth.service';
import { AuthInterceptor } from './auth.interceptor';

describe('AuthInterceptor', () => {
    let http: HttpClient;
    let httpMock: HttpTestingController;
    let authService: jasmine.SpyObj<AuthService>;

    beforeEach(() => {
        authService = jasmine.createSpyObj('AuthService', ['getToken', 'logout']);

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [
                { provide: AuthService, useValue: authService },
                { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
            ]
        });

        http = TestBed.inject(HttpClient);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => httpMock.verify());

    it('con token en memoria, adjunta el header Authorization', () => {
        authService.getToken.and.returnValue('jwt-de-prueba');

        http.get('/api/projects').subscribe();

        const req = httpMock.expectOne('/api/projects');
        expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-de-prueba');
        req.flush({});
    });

    it('sin token, no adjunta el header Authorization', () => {
        authService.getToken.and.returnValue(null);

        http.get('/api/projects').subscribe();

        const req = httpMock.expectOne('/api/projects');
        expect(req.request.headers.has('Authorization')).toBeFalse();
        req.flush({});
    });

    it('ante un 401 fuera del login, dispara logout', () => {
        authService.getToken.and.returnValue('jwt-vencido');

        http.get('/api/projects').subscribe({ error: () => {} });

        httpMock.expectOne('/api/projects').flush({ error: 'no autorizado' }, { status: 401, statusText: 'Unauthorized' });

        expect(authService.logout).toHaveBeenCalled();
    });

    it('ante un 401 del propio login, no dispara logout (es credencial invalida, no sesion vencida)', () => {
        authService.getToken.and.returnValue(null);

        http.post('/api/auth/login', { email: 'x', password: 'y' }).subscribe({ error: () => {} });

        httpMock
            .expectOne('/api/auth/login')
            .flush({ error: 'credenciales invalidas' }, { status: 401, statusText: 'Unauthorized' });

        expect(authService.logout).not.toHaveBeenCalled();
    });

    it('ante un 401 del propio logout, no vuelve a disparar logout (evita una llamada recursiva)', () => {
        authService.getToken.and.returnValue('jwt-ya-revocado');

        http.post('/api/auth/logout', {}).subscribe({ error: () => {} });

        httpMock
            .expectOne('/api/auth/logout')
            .flush({ error: 'sesion ya cerrada' }, { status: 401, statusText: 'Unauthorized' });

        expect(authService.logout).not.toHaveBeenCalled();
    });
});
