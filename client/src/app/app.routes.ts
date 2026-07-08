import { Routes } from '@angular/router';
import { roleGuard } from './core/auth/auth.guard';
import { ROLE_ADMIN, ROLE_PATIENT, ROLE_PSYCHOLOGIST } from './core/auth/auth.models';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/public-layout').then((m) => m.PublicLayout),
    children: [
      { path: '', loadComponent: () => import('./pages/home/home').then((m) => m.Home) },
      { path: 'layanan', loadComponent: () => import('./pages/services/services-page').then((m) => m.ServicesPage) },
      { path: 'tentang-kami', loadComponent: () => import('./pages/about/about-page').then((m) => m.AboutPage) },
      { path: 'psikolog-kami', loadComponent: () => import('./pages/psychologists/psychologists-page').then((m) => m.PsychologistsPage) },
      { path: 'psikolog-kami/:slug', loadComponent: () => import('./pages/psychologists/psychologist-detail-page').then((m) => m.PsychologistDetailPage) },
      { path: 'artikel', loadComponent: () => import('./pages/articles/articles-page').then((m) => m.ArticlesPage) },
      { path: 'artikel/:slug', loadComponent: () => import('./pages/articles/article-detail-page').then((m) => m.ArticleDetailPage) },
      { path: 'faq', loadComponent: () => import('./pages/faq/faq-page').then((m) => m.FaqPage) },
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
        path: 'akun/data-pribadi',
        canActivate: [roleGuard(ROLE_PATIENT)],
        loadComponent: () => import('./pages/account/patient-profile-page').then((m) => m.PatientProfilePage),
      },
      {
        path: 'akun/booking',
        canActivate: [roleGuard(ROLE_PATIENT)],
        loadComponent: () => import('./pages/account/patient-bookings').then((m) => m.PatientBookings),
      },
      {
        path: 'akun/booking/:id',
        canActivate: [roleGuard(ROLE_PATIENT)],
        loadComponent: () => import('./pages/account/patient-booking-detail').then((m) => m.PatientBookingDetail),
      },
      {
        path: 'janji-temu/:slug',
        canActivate: [roleGuard(ROLE_PATIENT)],
        loadComponent: () => import('./pages/booking/booking-wizard').then((m) => m.BookingWizard),
      },
      {
        path: 'psikolog',
        canActivate: [roleGuard(ROLE_PSYCHOLOGIST, ROLE_ADMIN)],
        loadComponent: () => import('./pages/psychologist/psych-home').then((m) => m.PsychHome),
      },
      {
        path: 'psikolog/pasien/:id',
        canActivate: [roleGuard(ROLE_PSYCHOLOGIST)],
        loadComponent: () => import('./pages/psychologist/psych-patient-detail').then((m) => m.PsychPatientDetail),
      },
      {
        path: 'admin',
        canActivate: [roleGuard(ROLE_ADMIN)],
        loadComponent: () => import('./pages/admin/admin-layout').then((m) => m.AdminLayout),
        children: [
          { path: '', loadComponent: () => import('./pages/admin/admin-home').then((m) => m.AdminHome) },
          { path: 'psikolog/:id', loadComponent: () => import('./pages/admin/psychologist-profile-edit').then((m) => m.PsychologistProfileEdit) },
          { path: 'psikolog/:id/jadwal', loadComponent: () => import('./pages/admin/psychologist-schedule-edit').then((m) => m.PsychologistScheduleEdit) },
          { path: 'booking', loadComponent: () => import('./pages/admin/bookings-admin').then((m) => m.BookingsAdmin) },
          { path: 'pengaturan', loadComponent: () => import('./pages/admin/settings-admin').then((m) => m.SettingsAdmin) },
          { path: 'artikel', loadComponent: () => import('./pages/admin/articles-admin').then((m) => m.ArticlesAdmin) },
          { path: 'artikel/baru', loadComponent: () => import('./pages/admin/article-edit').then((m) => m.ArticleEdit) },
          { path: 'artikel/:id', loadComponent: () => import('./pages/admin/article-edit').then((m) => m.ArticleEdit) },
          { path: 'faq', loadComponent: () => import('./pages/admin/faq-admin').then((m) => m.FaqAdmin) },
          { path: 'testimoni', loadComponent: () => import('./pages/admin/testimonials-admin').then((m) => m.TestimonialsAdmin) },
          { path: 'layanan', loadComponent: () => import('./pages/admin/services-admin').then((m) => m.ServicesAdmin) },
          { path: 'pasien', loadComponent: () => import('./pages/admin/patients-admin').then((m) => m.PatientsAdmin) },
        ],
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
