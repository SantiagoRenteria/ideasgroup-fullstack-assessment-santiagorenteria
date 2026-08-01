import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { BehaviorSubject } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { UserSession } from '../core/models/user-session.model';
import { AppTopBarComponent } from './app.topbar.component';
import { LayoutService } from './service/app.layout.service';

describe('AppTopBarComponent', () => {
    let fixture: ComponentFixture<AppTopBarComponent>;
    let authService: { currentUser$: BehaviorSubject<UserSession | null>; logout: jasmine.Spy };

    beforeEach(async () => {
        const currentUser$ = new BehaviorSubject<UserSession | null>({ name: 'Administrador', email: 'admin@ideasgroup.test' });
        authService = { currentUser$, logout: jasmine.createSpy('logout') };

        await TestBed.configureTestingModule({
            declarations: [AppTopBarComponent],
            imports: [RouterTestingModule],
            providers: [LayoutService, { provide: AuthService, useValue: authService }],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(AppTopBarComponent);
        fixture.detectChanges();
    });

    it('muestra el nombre del usuario logueado', () => {
        const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
        expect(text).toContain('Administrador');
    });

    it('al cerrar sesion, llama a AuthService.logout()', () => {
        const logoutButton = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find((button) =>
            button.textContent?.includes('Cerrar sesión')
        );

        logoutButton?.click();

        expect(authService.logout).toHaveBeenCalled();
    });
});
