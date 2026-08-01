export interface ProjectColumn {
    id: string;
    projectId: string;
    name: string;
    order: number;
}

export interface CreateColumnRequest {
    name: string;
    order: number;
}

export type UpdateColumnRequest = CreateColumnRequest;
