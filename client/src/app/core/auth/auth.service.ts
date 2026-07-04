import { HttpClient, HttpContext, HttpContextToken } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { ROLE_ADMIN, ROLE_PSYCHOLOGIST, TokenResponse, UserDto } from './auth.models';

/** Marks requests the auth interceptor must not try to refresh-and-retry. */
export const SKIP_AUTH_REFRESH = new HttpContextToken<boolean>(() => false);

const skipRefresh = () => new HttpContext().set(SKIP_AUTH_REFRESH, true);

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  /** Access token lives in memory only (never localStorage) per SPEC §2. */
  private readonly accessToken = signal<string | null>(null);

  readonly user = signal<UserDto | null>(null);
  readonly isLoggedIn = computed(() => this.user() !== null);

  token(): string | null {
    return this.accessToken();
  }

  hasRole(role: string): boolean {
    return this.user()?.roles.includes(role) ?? false;
  }

  /** Route to land on after login, by role. */
  homePath(): string {
    if (this.hasRole(ROLE_ADMIN)) return '/admin';
    if (this.hasRole(ROLE_PSYCHOLOGIST)) return '/psikolog';
    return '/akun';
  }

  login(email: string, password: string): Observable<void> {
    return this.http
      .post<TokenResponse>('/api/auth/login', { email, password }, {
        withCredentials: true,
        context: skipRefresh(),
      })
      .pipe(map((r) => this.setSession(r)));
  }

  register(fullName: string, email: string, whatsAppNumber: string, password: string): Observable<void> {
    return this.http
      .post<void>('/api/auth/register', { fullName, email, whatsAppNumber, password }, { context: skipRefresh() });
  }

  verifyEmail(email: string, token: string): Observable<void> {
    return this.http.post<void>('/api/auth/verify-email', { email, token }, { context: skipRefresh() });
  }

  resendVerification(email: string): Observable<void> {
    return this.http.post<void>('/api/auth/resend-verification', { email }, { context: skipRefresh() });
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>('/api/auth/forgot-password', { email }, { context: skipRefresh() });
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<void> {
    return this.http.post<void>('/api/auth/reset-password', { email, token, newPassword }, { context: skipRefresh() });
  }

  acceptInvitation(email: string, token: string, password: string): Observable<void> {
    return this.http.post<void>('/api/auth/accept-invitation', { email, token, password }, { context: skipRefresh() });
  }

  /** Silent session restore from the httpOnly refresh cookie. Never errors. */
  refresh(): Observable<boolean> {
    return this.http
      .post<TokenResponse>('/api/auth/refresh', null, {
        withCredentials: true,
        context: skipRefresh(),
      })
      .pipe(
        map((r) => {
          this.setSession(r);
          return true;
        }),
        catchError(() => {
          this.clearSession();
          return of(false);
        }),
      );
  }

  logout(): Observable<void> {
    return this.http
      .post<void>('/api/auth/logout', null, { withCredentials: true, context: skipRefresh() })
      .pipe(tap(() => this.clearSession()));
  }

  loadCurrentUser(): Observable<UserDto> {
    return this.http.get<UserDto>('/api/auth/me').pipe(tap((u) => this.user.set(u)));
  }

  private setSession(response: TokenResponse): void {
    this.accessToken.set(response.accessToken);
    this.user.set(response.user);
  }

  private clearSession(): void {
    this.accessToken.set(null);
    this.user.set(null);
  }
}
