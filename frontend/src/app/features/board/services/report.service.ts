import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { TaskPriority } from '../models/task.model';

export type ReportFormat = 'pdf' | 'excel';

export interface DownloadedReport {
    blob: Blob;
    fileName: string;
}

// Mismo filtro que el usuario tiene activo en el tablero (deseable seccion 7: "aplicados
// tambien al contenido del reporte") -- ver BoardComponent.downloadReport.
export interface ReportFilters {
    assigneeId?: string | null;
    priority?: TaskPriority | null;
}

const DEFAULT_FILE_NAME: Record<ReportFormat, string> = {
    pdf: 'reporte.pdf',
    excel: 'reporte.xlsx'
};

@Injectable({ providedIn: 'root' })
export class ReportService {
    private readonly baseUrl = `${environment.apiUrl}/projects`;

    constructor(private http: HttpClient) {}

    // El backend ya arma el nombre de archivo correcto por formato (Content-Disposition,
    // enunciado seccion 6.8); el fallback local solo cubre el caso en que ese header no
    // llegue al cliente (p. ej. un proxy intermedio que no lo reenvie).
    download(projectId: string, format: ReportFormat, filters?: ReportFilters): Observable<DownloadedReport> {
        let params = new HttpParams().set('format', format);

        if (filters?.assigneeId) {
            params = params.set('assigneeId', filters.assigneeId);
        }

        if (filters?.priority) {
            params = params.set('priority', filters.priority);
        }

        return this.http
            .get(`${this.baseUrl}/${projectId}/report`, {
                params,
                observe: 'response',
                responseType: 'blob'
            })
            .pipe(
                map((response) => ({
                    blob: response.body as Blob,
                    fileName: this.extractFileName(response.headers.get('content-disposition')) ?? DEFAULT_FILE_NAME[format]
                }))
            );
    }

    private extractFileName(contentDisposition: string | null): string | null {
        if (!contentDisposition) {
            return null;
        }

        const match = /filename="?([^";]+)"?/i.exec(contentDisposition);
        return match ? match[1] : null;
    }
}
