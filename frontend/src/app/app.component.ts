import { Component, OnInit } from '@angular/core';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router } from '@angular/router';
import { PrimeNGConfig } from 'primeng/api';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

    loading = false;

    constructor(private primengConfig: PrimeNGConfig, private router: Router) { }

    ngOnInit() {
        this.primengConfig.ripple = true;

        // Los modulos lazy-loaded (features/*, demo/*) tardan en descargarse la primera
        // vez que se navega a ellos; sin indicador, la pantalla queda en blanco un
        // instante y se percibe como parpadeo.
        this.router.events.subscribe((event) => {
            if (event instanceof NavigationStart) {
                this.loading = true;
            } else if (
                event instanceof NavigationEnd ||
                event instanceof NavigationCancel ||
                event instanceof NavigationError
            ) {
                this.loading = false;
            }
        });
    }
}
