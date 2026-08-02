import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { environment } from 'src/environments/environment';
import { LoginResponse, UserSession } from '../models/user-session.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
    // sessionStorage (no localStorage): sobrevive a recargar la pagina pero se pierde al
    // cerrar la pestana/navegador -- decision revisada durante la Fase 4, ver
    // docs/decisions/arquitectura-decisiones.md §17 (originalmente memoria pura, §7).
    private static readonly TOKEN_KEY = 'gestion_proyectos_token';
    private static readonly USER_KEY = 'gestion_proyectos_user';

    private currentUserSubject = new BehaviorSubject<UserSession | null>(this.readStoredUser());
    currentUser$ = this.currentUserSubject.asObservable();

    constructor(private http: HttpClient, private router: Router) {}

    login(email: string, password: string): Observable<void> {
        return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password }).pipe(
            tap((response) => {
                const user: UserSession = { name: response.name, email: response.email };
                sessionStorage.setItem(AuthService.TOKEN_KEY, response.token);
                sessionStorage.setItem(AuthService.USER_KEY, JSON.stringify(user));
                this.currentUserSubject.next(user);
            }),
            map(() => void 0)
        );
    }

    // Revocacion real en servidor (ADR §16); limpia el estado y redirige aunque la
    // llamada falle -- un logout no debe dejar al usuario atrapado por un error de red.
    logout(): void {
        const hadToken = this.getToken() !== null;
        const finish = () => {
            sessionStorage.removeItem(AuthService.TOKEN_KEY);
            sessionStorage.removeItem(AuthService.USER_KEY);
            this.currentUserSubject.next(null);
            this.router.navigate(['/auth/login']);
        };

        if (hadToken) {
            this.http.post(`${environment.apiUrl}/auth/logout`, {}).subscribe({ next: finish, error: finish });
        } else {
            finish();
        }
    }

    getToken(): string | null {
        return sessionStorage.getItem(AuthService.TOKEN_KEY);
    }

    isAuthenticated(): boolean {
        return this.getToken() !== null;
    }

    private readStoredUser(): UserSession | null {
        const raw = sessionStorage.getItem(AuthService.USER_KEY);
        return raw ? (JSON.parse(raw) as UserSession) : null;
    }
}
