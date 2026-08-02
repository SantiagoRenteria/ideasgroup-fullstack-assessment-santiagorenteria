import { HttpClient } from '@angular/common/http';
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
        // Los specs comparten la misma pagina de Karma: sessionStorage persiste entre
        // tests si no se limpia explicitamente (a diferencia de una variable de instancia).
        sessionStorage.clear();

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

    it('logout revoca el token en el servidor, limpia el estado local y redirige al login', (done) => {
        service.login('admin@ideasgroup.test', 'IdeasGroup2026!').subscribe(() => {
            service.logout();

            const logoutReq = httpMock.expectOne(`${environment.apiUrl}/auth/logout`);
            expect(logoutReq.request.method).toBe('POST');
            logoutReq.flush({});

            expect(service.isAuthenticated()).toBeFalse();
            expect(service.getToken()).toBeNull();
            expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
            done();
        });

        httpMock
            .expectOne(`${environment.apiUrl}/auth/login`)
            .flush({ token: 'jwt-de-prueba', expiresAtUtc: '2026-08-01T00:00:00Z', name: 'Administrador', email: 'admin@ideasgroup.test' });
    });

    it('logout limpia el estado local aunque la revocacion en el servidor falle (sin dejar al usuario atrapado)', (done) => {
        service.login('admin@ideasgroup.test', 'IdeasGroup2026!').subscribe(() => {
            service.logout();

            httpMock.expectOne(`${environment.apiUrl}/auth/logout`).flush({ error: 'error de red' }, { status: 500, statusText: 'Server Error' });

            expect(service.isAuthenticated()).toBeFalse();
            expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
            done();
        });

        httpMock
            .expectOne(`${environment.apiUrl}/auth/login`)
            .flush({ token: 'jwt-de-prueba', expiresAtUtc: '2026-08-01T00:00:00Z', name: 'Administrador', email: 'admin@ideasgroup.test' });
    });

    it('logout sin sesion activa no llama al backend, solo redirige', () => {
        service.logout();

        httpMock.expectNone(`${environment.apiUrl}/auth/logout`);
        expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
    });

    // ADR §17: sessionStorage (no memoria pura) para sobrevivir a recargar la pagina --
    // una instancia nueva del servicio simula el estado tras un F5.
    it('una instancia nueva del servicio recupera la sesion desde sessionStorage (sobrevive a recargar la pagina)', () => {
        sessionStorage.setItem('gestion_proyectos_token', 'jwt-de-prueba');
        sessionStorage.setItem('gestion_proyectos_user', JSON.stringify({ name: 'Administrador', email: 'admin@ideasgroup.test' }));

        const freshService = new AuthService(TestBed.inject(HttpClient), router);
        let emittedUser: unknown;
        freshService.currentUser$.subscribe((user) => (emittedUser = user));

        expect(freshService.isAuthenticated()).toBeTrue();
        expect(freshService.getToken()).toBe('jwt-de-prueba');
        expect(emittedUser).toEqual({ name: 'Administrador', email: 'admin@ideasgroup.test' });
    });
});
