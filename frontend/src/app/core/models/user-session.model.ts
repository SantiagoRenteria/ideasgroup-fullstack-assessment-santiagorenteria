export interface UserSession {
    name: string;
    email: string;
}

export interface LoginResponse {
    token: string;
    expiresAtUtc: string;
    name: string;
    email: string;
}
