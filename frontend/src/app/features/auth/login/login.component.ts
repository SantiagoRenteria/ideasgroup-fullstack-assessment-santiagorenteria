import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
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

    submitted = false;
    loading = false;
    error: string | null = null;

    // Reactive Forms (consistente con task-form/project-form, ver revision de arquitectura):
    // el login no depende de validacion de formato en cliente para la seguridad (el backend
    // ya la exige via FluentValidation), pero antes no tenia ninguna, ni siquiera "campo
    // obligatorio" -- esto evita un round-trip HTTP para el caso mas trivial de error.
    form: FormGroup = this.fb.group({
        email: ['', [Validators.required, Validators.email]],
        password: ['', Validators.required]
    });

    constructor(public layoutService: LayoutService, private fb: FormBuilder, private authService: AuthService, private router: Router) { }

    onSubmit(): void {
        this.submitted = true;
        this.error = null;

        if (this.form.invalid) {
            return;
        }

        this.loading = true;
        const { email, password } = this.form.value;

        this.authService.login(email, password).subscribe({
            next: () => {
                this.loading = false;
                this.router.navigate(['/projects']);
            },
            error: () => {
                this.loading = false;
                this.error = 'Correo o contraseña incorrectos.';
            }
        });
    }
}
