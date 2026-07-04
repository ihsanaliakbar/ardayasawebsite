export interface UserDto {
  id: string;
  fullName: string;
  email: string;
  whatsAppNumber: string | null;
  roles: string[];
}

export interface TokenResponse {
  user: UserDto;
  accessToken: string;
  expiresInSeconds: number;
}

export interface ApiError {
  code: string;
  description?: string;
}

export interface ApiErrorBody {
  errors?: ApiError[];
}

export const ROLE_ADMIN = 'Admin';
export const ROLE_PSYCHOLOGIST = 'Psychologist';
export const ROLE_PATIENT = 'Patient';
