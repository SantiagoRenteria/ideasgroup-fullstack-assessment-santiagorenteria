import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { environment } from 'src/environments/environment';
import { LoginResponse, UserSession } from '../models/user-session.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
    // El token vive solo en memoria (no localStorage ni cookie): se pierde al recargar
    // la pagina a proposito, ver docs/decisions/arquitectura-decisiones.md §7.
    private token: string | null = null;

    private currentUserSubject = new BehaviorSubject<UserSession | null>(null);
    currentUser$ = this.currentUserSubject.asObservable();

    constructor(private http: HttpClient, private router: Router) {}

    login(email: string, password: string): Observable<void> {
        return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password }).pipe(
            tap((response) => {
                this.token = response.token;
                this.currentUserSubject.next({ name: response.name, email: response.email });
            }),
            map(() => void 0)
        );
    }

    logout(): void {
        this.token = null;
        this.currentUserSubject.next(null);
        this.router.navigate(['/auth/login']);
    }

    getToken(): string | null {
        return this.token;
    }

    isAuthenticated(): boolean {
        return this.token !== null;
    }
}
