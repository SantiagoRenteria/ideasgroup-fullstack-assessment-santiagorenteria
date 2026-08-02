import { RouterModule } from '@angular/router';
import { NgModule } from '@angular/core';
import { NotFoundComponent } from './features/not-found/not-found.component';
import { AppLayoutComponent } from "./layout/app.layout.component";
import { authGuard } from './core/guards/auth.guard';

@NgModule({
    imports: [
        RouterModule.forRoot([
            {
                path: '', component: AppLayoutComponent, canActivate: [authGuard],
                children: [
                    { path: '', redirectTo: 'projects', pathMatch: 'full' },
                    { path: 'projects', loadChildren: () => import('./features/projects/projects.module').then(m => m.ProjectsModule) },
                    { path: 'board', loadChildren: () => import('./features/board/board.module').then(m => m.BoardModule) }
                ]
            },
            { path: 'auth/login', loadChildren: () => import('./features/auth/login/login.module').then(m => m.LoginModule) },
            { path: 'notfound', component: NotFoundComponent },
            { path: '**', redirectTo: '/notfound' },
        ], { scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled', onSameUrlNavigation: 'reload' })
    ],
    exports: [RouterModule]
})
export class AppRoutingModule {
}
