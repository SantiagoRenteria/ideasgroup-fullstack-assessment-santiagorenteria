import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styles: [`
        :host ::ng-deep .pi-eye,
        :host ::ng-deep .pi-eye-slash {
            transform:scale(1.6);
            margin-right: 1rem;
            color: var(--primary-color) !important;
        }
    `]
})
export class LoginComponent {

    email = '';
    password = '';
    loading = false;
    error: string | null = null;

    constructor(public layoutService: LayoutService, private authService: AuthService, private router: Router) { }

    onSubmit(): void {
        this.error = null;
        this.loading = true;

        this.authService.login(this.email, this.password).subscribe({
            next: () => {
                this.loading = false;
                this.router.navigate(['/']);
            },
            error: () => {
                this.loading = false;
                this.error = 'Correo o contraseña incorrectos.';
            }
        });
    }
}
