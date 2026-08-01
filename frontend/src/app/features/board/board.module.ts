import { DragDropModule } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { BoardRoutingModule } from './board-routing.module';
import { BoardComponent } from './board/board.component';
import { TaskFormComponent } from './task-form/task-form.component';

@NgModule({
    declarations: [BoardComponent, TaskFormComponent],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        DragDropModule,
        BoardRoutingModule,
        ButtonModule,
        InputTextModule,
        InputTextareaModule,
        DialogModule,
        DropdownModule,
        ToastModule,
        ConfirmDialogModule,
        TagModule
    ]
})
export class BoardModule {}
