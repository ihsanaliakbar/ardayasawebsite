import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-psych-home',
  imports: [MatCardModule, TranslatePipe],
  template: `
    <h1>{{ 'psych.title' | translate }}</h1>
    <mat-card class="placeholder">
      <p>{{ 'psych.comingSoon' | translate }}</p>
    </mat-card>
  `,
  styles: `
    h1 { font: var(--mat-sys-headline-medium); }
    .placeholder { padding: 24px; max-width: 480px; }
  `,
})
export class PsychHome {}
