import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    constructor(private authService: AuthService) {}

    intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        const token = this.authService.getToken();
        const authReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

        const esLogin = req.url.includes('/auth/login');
        // Si el propio logout devuelve 401 (token ya vencido/revocado), no hay que volver
        // a llamar a logout() -- provocaria una segunda llamada a POST /auth/logout
        // recursiva. AuthService.logout() ya limpia el estado local sin importar la
        // respuesta de esa llamada puntual.
        const esLogout = req.url.includes('/auth/logout');

        return next.handle(authReq).pipe(
            catchError((error: HttpErrorResponse) => {
                // El 401 del propio login es una credencial invalida, no una sesion
                // expirada: no debe disparar el logout automatico.
                if (error.status === 401 && !esLogin && !esLogout) {
                    this.authService.logout();
                }
                return throwError(() => error);
            })
        );
    }
}
