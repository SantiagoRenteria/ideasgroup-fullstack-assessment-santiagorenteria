import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { AppUser } from '../models/app-user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
    private readonly apiUrl = environment.apiUrl;

    constructor(private http: HttpClient) {}

    listAll(): Observable<AppUser[]> {
        return this.http.get<AppUser[]>(`${this.apiUrl}/users`);
    }
}
