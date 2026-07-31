export interface UsuarioSesion {
    nombre: string;
    correo: string;
}

export interface LoginResponse {
    token: string;
    expiresAtUtc: string;
    nombre: string;
    correo: string;
}
