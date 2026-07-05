import { Component, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';

export interface PsychologistProfile {
  id: string;
  displayName: string;
  title: string | null;
  slug: string | null;
  specialization: string | null;
  education: string[];
  expertise: string[];
  bio: string | null;
  photoUrl: string | null;
  scheduleLines: string[];
  displayOrder: number;
  isActive: boolean;
}

export interface ProfileSavePayload {
  displayName: string;
  title: string | null;
  specialization: string | null;
  education: string[];
  expertise: string[];
  bio: string | null;
  scheduleLines: string[];
  displayOrder: number | null;
  isActive: boolean | null;
}

/**
 * Profile editor used by the admin area only — psychologist profiles are
 * admin-managed (psychologists get a read-only view on their dashboard).
 * List fields are edited one-item-per-line.
 */
@Component({
  selector: 'app-psychologist-profile-form',
  imports: [
    ReactiveFormsModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatCheckboxModule, MatIconModule, TranslatePipe,
  ],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()">
      <div class="photo-row">
        @if (profile().photoUrl; as url) {
          <img [src]="url" alt="" class="photo" />
        } @else {
          <div class="photo placeholder"><mat-icon>person</mat-icon></div>
        }
        <label>
          <input type="file" accept="image/*" hidden (change)="onPhotoSelected($event)" />
          <span mat-stroked-button class="upload-btn">
            <mat-icon inline>upload</mat-icon> {{ 'profileForm.uploadPhoto' | translate }}
          </span>
        </label>
      </div>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'profileForm.displayName' | translate }}</mat-label>
        <input matInput formControlName="displayName" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'profileForm.title' | translate }}</mat-label>
        <input matInput formControlName="title" placeholder="M.Psi., Psikolog" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'profileForm.specialization' | translate }}</mat-label>
        <input matInput formControlName="specialization" placeholder="Psikolog Klinis Dewasa" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'profileForm.education' | translate }}</mat-label>
        <textarea matInput formControlName="education" rows="3"></textarea>
        <mat-hint>{{ 'profileForm.linesHint' | translate }}</mat-hint>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'profileForm.expertise' | translate }}</mat-label>
        <textarea matInput formControlName="expertise" rows="5"></textarea>
        <mat-hint>{{ 'profileForm.linesHint' | translate }}</mat-hint>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'profileForm.bio' | translate }}</mat-label>
        <textarea matInput formControlName="bio" rows="4"></textarea>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'profileForm.schedule' | translate }}</mat-label>
        <textarea matInput formControlName="scheduleLines" rows="4" placeholder="Senin 09.00–13.00 WIB"></textarea>
        <mat-hint>{{ 'profileForm.linesHint' | translate }}</mat-hint>
      </mat-form-field>

      <div class="admin-row">
        <mat-form-field appearance="outline" class="order-field">
          <mat-label>{{ 'profileForm.displayOrder' | translate }}</mat-label>
          <input matInput type="number" formControlName="displayOrder" />
        </mat-form-field>
        <mat-checkbox formControlName="isActive">{{ 'profileForm.isActive' | translate }}</mat-checkbox>
      </div>

      <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">
        {{ 'profileForm.save' | translate }}
      </button>
    </form>
  `,
  styles: `
    form { display: flex; flex-direction: column; gap: 6px; max-width: 640px; }
    .photo-row { display: flex; align-items: center; gap: 20px; margin-bottom: 16px; }
    .photo { width: 96px; height: 114px; object-fit: cover; border-radius: 12px; }
    .photo.placeholder { display: grid; place-items: center; background: var(--mat-sys-surface-container-high); }
    .upload-btn { cursor: pointer; display: inline-flex; align-items: center; gap: 6px; padding: 8px 16px; border: 1px solid var(--mat-sys-outline); border-radius: 999px; }
    .admin-row { display: flex; align-items: center; gap: 20px; }
    .order-field { width: 140px; }
    mat-form-field { width: 100%; }
    button[type='submit'] { align-self: flex-start; margin-top: 8px; }
  `,
})
export class PsychologistProfileForm {
  private readonly fb = inject(FormBuilder);

  readonly profile = input.required<PsychologistProfile>();
  readonly busy = input(false);
  readonly save = output<ProfileSavePayload>();
  readonly photoSelected = output<File>();

  protected readonly form = this.fb.nonNullable.group({
    displayName: ['', Validators.required],
    title: [''],
    specialization: [''],
    education: [''],
    expertise: [''],
    bio: [''],
    scheduleLines: [''],
    displayOrder: [0],
    isActive: [true],
  });

  constructor() {
    effect(() => {
      const p = this.profile();
      this.form.patchValue({
        displayName: p.displayName,
        title: p.title ?? '',
        specialization: p.specialization ?? '',
        education: p.education.join('\n'),
        expertise: p.expertise.join('\n'),
        bio: p.bio ?? '',
        scheduleLines: p.scheduleLines.join('\n'),
        displayOrder: p.displayOrder,
        isActive: p.isActive,
      });
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }

    const value = this.form.getRawValue();
    this.save.emit({
      displayName: value.displayName.trim(),
      title: value.title.trim() || null,
      specialization: value.specialization.trim() || null,
      education: toLines(value.education),
      expertise: toLines(value.expertise),
      bio: value.bio.trim() || null,
      scheduleLines: toLines(value.scheduleLines),
      displayOrder: value.displayOrder,
      isActive: value.isActive,
    });
  }

  protected onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (file) {
      this.photoSelected.emit(file);
    }
  }
}

function toLines(value: string): string[] {
  return value
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.length > 0);
}
