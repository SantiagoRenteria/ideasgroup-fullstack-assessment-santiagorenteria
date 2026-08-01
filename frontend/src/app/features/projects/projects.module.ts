import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CalendarModule } from 'primeng/calendar';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ProjectColumnsComponent } from './project-columns/project-columns.component';
import { ProjectFormComponent } from './project-form/project-form.component';
import { ProjectListComponent } from './project-list/project-list.component';
import { ProjectsRoutingModule } from './projects-routing.module';

@NgModule({
    declarations: [ProjectListComponent, ProjectFormComponent, ProjectColumnsComponent],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        ProjectsRoutingModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        InputTextareaModule,
        DialogModule,
        DropdownModule,
        CalendarModule,
        ToastModule,
        ConfirmDialogModule,
        TooltipModule
    ]
})
export class ProjectsModule {}
