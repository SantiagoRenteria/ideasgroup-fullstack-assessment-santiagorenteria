export enum ProjectStatus {
    Planned = 'Planned',
    InProgress = 'InProgress',
    Completed = 'Completed',
    Cancelled = 'Cancelled'
}

export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
    [ProjectStatus.Planned]: 'Planificado',
    [ProjectStatus.InProgress]: 'En progreso',
    [ProjectStatus.Completed]: 'Completado',
    [ProjectStatus.Cancelled]: 'Cancelado'
};

export interface Project {
    id: string;
    name: string;
    description: string;
    startDate: string;
    endDate: string;
    status: ProjectStatus;
}

export interface ProjectListParams {
    page: number;
    pageSize: number;
    name?: string;
    status?: ProjectStatus;
}

export interface CreateProjectRequest {
    name: string;
    description: string;
    startDate: string;
    endDate: string;
    status: ProjectStatus;
}

export type UpdateProjectRequest = CreateProjectRequest;
