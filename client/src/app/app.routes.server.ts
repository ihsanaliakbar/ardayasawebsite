import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // Authenticated areas render client-side only (SPEC §4: no SSR for dashboards).
  { path: 'akun', renderMode: RenderMode.Client },
  { path: 'akun/**', renderMode: RenderMode.Client },
  { path: 'psikolog', renderMode: RenderMode.Client },
  { path: 'psikolog/**', renderMode: RenderMode.Client },
  { path: 'admin', renderMode: RenderMode.Client },
  { path: 'admin/**', renderMode: RenderMode.Client },
  { path: 'janji-temu/**', renderMode: RenderMode.Client },
  // Public routes are server-rendered for SEO.
  { path: '**', renderMode: RenderMode.Server },
];
