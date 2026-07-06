import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EMPTY, catchError, switchMap, tap } from 'rxjs';
import { CLINIC } from '../../core/clinic';
import { ContentService } from '../../core/content/content.service';
import { SeoService } from '../../core/seo';

@Component({
  selector: 'app-psychologist-detail-page',
  imports: [RouterLink, MatButtonModule, MatIconModule, TranslatePipe],
  template: `
    @if (psychologist(); as p) {
      <div class="page-container page">
        <a routerLink="/psikolog-kami" class="back">
          <mat-icon inline>arrow_back_ios</mat-icon> {{ 'psychologists.backToList' | translate }}
        </a>

        <div class="layout">
          <aside class="card-panel side">
            @if (p.photoUrl) {
              <img [src]="p.photoUrl" [alt]="p.displayName" class="photo" />
            }
            @if (p.scheduleLines.length > 0) {
              <h3>{{ 'psychologists.schedule' | translate }}</h3>
              <ul class="schedule">
                @for (line of p.scheduleLines; track line) {
                  <li><mat-icon inline>schedule</mat-icon> {{ line }}</li>
                }
              </ul>
              <p class="schedule-note">{{ 'psychologists.scheduleNote' | translate }}</p>
            }
            <a mat-flat-button class="gold-btn" [href]="clinic.whatsAppUrl" target="_blank" rel="noopener">
              {{ 'psychologists.book' | translate }}
            </a>
          </aside>

          <div class="main">
            <h1>{{ p.displayName }}<span class="degree">, {{ p.title }}</span></h1>
            @if (p.specialization) {
              <span class="badge">{{ p.specialization }}</span>
            }

            @if (p.education.length > 0) {
              <ul class="education">
                @for (line of p.education; track line) {
                  <li><mat-icon inline>school</mat-icon> {{ line }}</li>
                }
              </ul>
            }

            @if (p.bio) {
              <p class="bio">{{ p.bio }}</p>
            }

            @if (p.expertise.length > 0) {
              <h2>{{ 'psychologists.expertise' | translate }}</h2>
              <ul class="expertise">
                @for (item of p.expertise; track item) {
                  <li>{{ item }}</li>
                }
              </ul>
            }

            @if (p.testimonials.length > 0) {
              <h2>{{ 'psychologists.testimonials' | translate }}</h2>
              <div class="testimonials">
                @for (t of p.testimonials; track t.id) {
                  <div class="card-panel testimonial">
                    <p>"{{ t.content }}"</p>
                    <div class="who">{{ t.authorName }} · <span>{{ t.roleLabel }}</span></div>
                  </div>
                }
              </div>
            }
          </div>
        </div>
      </div>
    }
  `,
  styles: `
    .page { padding-top: 32px; }
    .back { color: var(--text-muted); text-decoration: none; font-size: 0.9rem; }
    .back:hover { color: var(--accent-gold); }
    .layout { display: grid; grid-template-columns: 300px 1fr; gap: 28px; margin-top: 20px; }

    .side { padding: 24px; height: fit-content; display: flex; flex-direction: column; gap: 8px; }
    .photo { width: 100%; border-radius: 14px; border: 2px solid rgba(244, 217, 111, 0.35); }
    .side h3 { margin: 12px 0 4px; font-size: 1rem; }
    .schedule { list-style: none; margin: 0; padding: 0; color: var(--text-muted); font-size: 0.875rem; }
    .schedule li { padding: 4px 0; }
    .schedule mat-icon { color: var(--accent-gold); vertical-align: -3px; margin-right: 6px; }
    .schedule-note { color: var(--teal-muted); font-size: 0.75rem; }
    .gold-btn { --mat-button-filled-container-color: var(--accent-gold); --mat-button-filled-label-text-color: #071c1f; font-weight: 600; margin-top: 8px; }

    h1 { font-size: clamp(1.6rem, 3vw, 2.2rem); margin: 0 0 8px; }
    .degree { font-family: var(--font-body); font-weight: 400; font-size: 1rem; color: var(--text-muted); }
    .badge {
      display: inline-block; font-size: 0.8rem; color: var(--accent-gold);
      border: 1px solid rgba(244, 217, 111, 0.4); border-radius: 999px; padding: 4px 12px; margin-bottom: 16px;
    }
    .education { list-style: none; padding: 0; margin: 0 0 16px; color: var(--text-muted); font-size: 0.9rem; }
    .education li { padding: 2px 0; }
    .education mat-icon { color: var(--teal-muted); vertical-align: -3px; margin-right: 6px; }
    .bio { color: var(--text-main); line-height: 1.7; max-width: 68ch; }
    h2 { font-size: 1.25rem; margin: 28px 0 12px; }
    .expertise { columns: 2; padding-left: 18px; color: var(--text-muted); }
    .expertise li { margin-bottom: 6px; break-inside: avoid; }
    .testimonials { display: grid; gap: 14px; }
    .testimonial { padding: 18px 20px; }
    .testimonial p { margin: 0 0 8px; font-style: italic; color: var(--text-main); }
    .who { font-size: 0.82rem; color: var(--teal-muted); }
    .who span { color: var(--text-muted); }

    @media (max-width: 800px) {
      .layout { grid-template-columns: 1fr; }
      .expertise { columns: 1; }
    }
  `,
})
export class PsychologistDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly content = inject(ContentService);
  private readonly seo = inject(SeoService);
  private readonly translate = inject(TranslateService);

  protected readonly clinic = CLINIC;

  protected readonly psychologist = toSignal(
    this.route.paramMap.pipe(
      switchMap((params) =>
        this.content.getPsychologist(params.get('slug')!).pipe(
          catchError(() => {
            void this.router.navigateByUrl('/psikolog-kami');
            return EMPTY;
          }),
        ),
      ),
      tap((p) =>
        this.seo.update({
          title: `${p.displayName}, ${p.title ?? ''}`.replace(/, $/, ''),
          description:
            p.bio ?? this.translate.instant('psychologists.metaDescription'),
          image: p.photoUrl ?? undefined,
        }),
      ),
    ),
  );
}
