import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { AppNotificationsComponent } from './components/app-notifications/app-notifications.component';

// Capa "shared" que el ADR §2.1 documenta (componentes reutilizables sin logica de negocio
// propia) pero que todavia no existia en el codigo -- ver revision de arquitectura frontend.
@NgModule({
    declarations: [AppNotificationsComponent],
    imports: [CommonModule, ToastModule, ConfirmDialogModule],
    exports: [AppNotificationsComponent]
})
export class SharedModule {}
