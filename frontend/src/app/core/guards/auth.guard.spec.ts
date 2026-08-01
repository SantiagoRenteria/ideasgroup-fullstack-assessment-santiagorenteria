import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
    let authService: jasmine.SpyObj<AuthService>;
    let router: jasmine.SpyObj<Router>;

    beforeEach(() => {
        authService = jasmine.createSpyObj('AuthService', ['isAuthenticated']);
        router = jasmine.createSpyObj('Router', ['navigate']);

        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: authService },
                { provide: Router, useValue: router }
            ]
        });
    });

    it('con sesion valida, permite el acceso sin redirigir', () => {
        authService.isAuthenticated.and.returnValue(true);

        const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

        expect(result).toBeTrue();
        expect(router.navigate).not.toHaveBeenCalled();
    });

    it('sin sesion valida, bloquea el acceso y redirige a /auth/login', () => {
        authService.isAuthenticated.and.returnValue(false);

        const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

        expect(result).toBeFalse();
        expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
    });
});
