import { Component } from '@angular/core';

// Host unico de p-toast/p-confirmDialog para toda la app (revision de arquitectura frontend):
// antes cada pagina que llamaba a MessageService/ConfirmationService (BoardComponent,
// ProjectListComponent) declaraba su propia instancia de ambos servicios a nivel de
// @Component y repetia este mismo par de tags en su propio template. Con
// ConfirmationService/MessageService provistos una sola vez en AppModule (ver app.module.ts),
// todo componente que los inyecta (incluidos los hijos como TaskFormComponent,
// ProjectFormComponent) apunta a la misma instancia que este host raiz escucha.
@Component({
    selector: 'app-notifications',
    templateUrl: './app-notifications.component.html'
})
export class AppNotificationsComponent {}
