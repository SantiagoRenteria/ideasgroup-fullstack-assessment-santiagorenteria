import { NgModule } from '@angular/core';
import { HashLocationStrategy, LocationStrategy } from '@angular/common';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { AppLayoutModule } from './layout/app.layout.module';
import { NotFoundComponent } from './features/not-found/not-found.component';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';

@NgModule({
    declarations: [
        AppComponent, NotFoundComponent
    ],
    imports: [
        AppRoutingModule,
        AppLayoutModule,
        ButtonModule,
        ProgressSpinnerModule
    ],
    providers: [
        { provide: LocationStrategy, useClass: HashLocationStrategy },
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
