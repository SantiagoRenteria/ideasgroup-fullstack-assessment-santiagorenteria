import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Board } from '../models/board.model';

@Injectable({ providedIn: 'root' })
export class BoardService {
    private readonly apiUrl = environment.apiUrl;

    constructor(private http: HttpClient) {}

    getByProject(projectId: string): Observable<Board> {
        return this.http.get<Board>(`${this.apiUrl}/projects/${projectId}/board`);
    }
}
