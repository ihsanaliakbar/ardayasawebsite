import { Routes } from '@angular/router';
import { roleGuard } from './core/auth/auth.guard';
import { ROLE_ADMIN, ROLE_PATIENT, ROLE_PSYCHOLOGIST } from './core/auth/auth.models';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/public-layout').then((m) => m.PublicLayout),
    children: [
      { path: '', loadComponent: () => import('./pages/home/home').then((m) => m.Home) },
      { path: 'masuk', loadComponent: () => import('./pages/auth/login').then((m) => m.Login) },
      { path: 'daftar', loadComponent: () => import('./pages/auth/register').then((m) => m.Register) },
      { path: 'verifikasi-email', loadComponent: () => import('./pages/auth/verify-email').then((m) => m.VerifyEmail) },
      { path: 'lupa-kata-sandi', loadComponent: () => import('./pages/auth/forgot-password').then((m) => m.ForgotPassword) },
      { path: 'atur-ulang-kata-sandi', loadComponent: () => import('./pages/auth/reset-password').then((m) => m.ResetPassword) },
      { path: 'terima-undangan', loadComponent: () => import('./pages/auth/accept-invitation').then((m) => m.AcceptInvitation) },
      {
        path: 'akun',
        canActivate: [roleGuard(ROLE_PATIENT, ROLE_PSYCHOLOGIST, ROLE_ADMIN)],
        loadComponent: () => import('./pages/account/account-home').then((m) => m.AccountHome),
      },
      {
        path: 'psikolog',
        canActivate: [roleGuard(ROLE_PSYCHOLOGIST, ROLE_ADMIN)],
        loadComponent: () => import('./pages/psychologist/psych-home').then((m) => m.PsychHome),
      },
      {
        path: 'admin',
        canActivate: [roleGuard(ROLE_ADMIN)],
        loadComponent: () => import('./pages/admin/admin-home').then((m) => m.AdminHome),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
