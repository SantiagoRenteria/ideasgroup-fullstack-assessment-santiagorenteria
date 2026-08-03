export enum TaskPriority {
    Low = 'Low',
    Medium = 'Medium',
    High = 'High',
    Urgent = 'Urgent'
}

export const TASK_PRIORITY_LABELS: Record<TaskPriority, string> = {
    [TaskPriority.Low]: 'Baja',
    [TaskPriority.Medium]: 'Media',
    [TaskPriority.High]: 'Alta',
    [TaskPriority.Urgent]: 'Urgente'
};

export const TASK_PRIORITY_SEVERITY: Record<TaskPriority, 'success' | 'info' | 'warning' | 'danger'> = {
    [TaskPriority.Low]: 'success',
    [TaskPriority.Medium]: 'info',
    [TaskPriority.High]: 'warning',
    [TaskPriority.Urgent]: 'danger'
};

// Antes se derivaba con el mismo Object.values(...).map(...) por separado en
// BoardComponent y TaskFormComponent -- un solo lugar para la lista de opciones del dropdown.
export const TASK_PRIORITY_OPTIONS = Object.values(TaskPriority).map((value) => ({ label: TASK_PRIORITY_LABELS[value], value }));

export interface BoardTask {
    id: string;
    columnId: string;
    title: string;
    description: string;
    priority: TaskPriority;
    assigneeId: string | null;
    order: string;
    createdAt: string;
}

export interface CreateTaskRequest {
    columnId: string;
    title: string;
    description: string;
    priority: TaskPriority;
    assigneeId: string | null;
}

export interface UpdateTaskRequest {
    title: string;
    description: string;
    priority: TaskPriority;
    assigneeId: string | null;
}

export interface MoveTaskRequest {
    targetColumnId: string;
    targetIndex: number;
}
