import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export type ReportFormat = 'pdf' | 'excel';

export interface DownloadedReport {
    blob: Blob;
    fileName: string;
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
    download(projectId: string, format: ReportFormat): Observable<DownloadedReport> {
        return this.http
            .get(`${this.baseUrl}/${projectId}/report`, {
                params: { format },
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
