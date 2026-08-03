import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { AppLayoutModule } from './layout/app.layout.module';
import { NotFoundComponent } from './features/not-found/not-found.component';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { SharedModule } from './shared/shared.module';

@NgModule({
    declarations: [
        AppComponent, NotFoundComponent
    ],
    imports: [
        CommonModule,
        AppRoutingModule,
        AppLayoutModule,
        SharedModule,
        ButtonModule,
        ProgressSpinnerModule
    ],
    providers: [
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
        // Instancia unica para toda la app -- ver shared/components/app-notifications, que
        // es el unico lugar que renderiza p-toast/p-confirmDialog (antes duplicado por pagina).
        ConfirmationService,
        MessageService
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
