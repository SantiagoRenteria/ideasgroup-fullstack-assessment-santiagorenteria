import { BoardTask } from './task.model';

export interface BoardColumn {
    id: string;
    name: string;
    order: number;
    tasks: BoardTask[];
}

export interface Board {
    projectId: string;
    projectName: string;
    columns: BoardColumn[];
}
