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

        return next.handle(authReq).pipe(
            catchError((error: HttpErrorResponse) => {
                // El 401 del propio login es una credencial invalida, no una sesion
                // expirada: no debe disparar el logout automatico.
                if (error.status === 401 && !esLogin) {
                    this.authService.logout();
                }
                return throwError(() => error);
            })
        );
    }
}
