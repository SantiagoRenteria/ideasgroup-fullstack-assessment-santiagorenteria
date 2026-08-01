import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { environment } from 'src/environments/environment';
import { AuthService } from './auth.service';

describe('AuthService', () => {
    let service: AuthService;
    let httpMock: HttpTestingController;
    let router: jasmine.SpyObj<Router>;

    beforeEach(() => {
        const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [AuthService, { provide: Router, useValue: routerSpy }]
        });

        service = TestBed.inject(AuthService);
        httpMock = TestBed.inject(HttpTestingController);
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    });

    afterEach(() => httpMock.verify());

    it('antes de iniciar sesion, no esta autenticado y no hay token', () => {
        expect(service.isAuthenticated()).toBeFalse();
        expect(service.getToken()).toBeNull();
    });

    it('login exitoso guarda el token en memoria y publica el usuario actual', (done) => {
        let emittedUser: unknown;
        service.currentUser$.subscribe((user) => (emittedUser = user));

        service.login('admin@ideasgroup.test', 'IdeasGroup2026!').subscribe(() => {
            expect(service.isAuthenticated()).toBeTrue();
            expect(service.getToken()).toBe('jwt-de-prueba');
            expect(emittedUser).toEqual({ name: 'Administrador', email: 'admin@ideasgroup.test' });
            done();
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ email: 'admin@ideasgroup.test', password: 'IdeasGroup2026!' });
        req.flush({
            token: 'jwt-de-prueba',
            expiresAtUtc: '2026-08-01T00:00:00Z',
            name: 'Administrador',
            email: 'admin@ideasgroup.test'
        });
    });

    it('login con credenciales invalidas no deja el servicio autenticado', (done) => {
        service.login('admin@ideasgroup.test', 'incorrecta').subscribe({
            error: () => {
                expect(service.isAuthenticated()).toBeFalse();
                expect(service.getToken()).toBeNull();
                done();
            }
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
        req.flush({ error: 'Correo o contraseña incorrectos.' }, { status: 401, statusText: 'Unauthorized' });
    });

    it('logout limpia el token, el usuario actual y redirige al login', (done) => {
        service.login('admin@ideasgroup.test', 'IdeasGroup2026!').subscribe(() => {
            service.logout();

            expect(service.isAuthenticated()).toBeFalse();
            expect(service.getToken()).toBeNull();
            expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
            done();
        });

        httpMock
            .expectOne(`${environment.apiUrl}/auth/login`)
            .flush({ token: 'jwt-de-prueba', expiresAtUtc: '2026-08-01T00:00:00Z', name: 'Administrador', email: 'admin@ideasgroup.test' });
    });
});
