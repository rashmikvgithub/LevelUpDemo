import { Component, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { RecommendationCategory, RecommendationTrigger, ShownRecommendation } from '../../core/models/recommendation.models';
import { RecommendationService } from '../../core/services/recommendation.service';

/**
 * "Help Me!" button + suggestion card. Asks the recommendation engine for an idea in the
 * given context; the backend records every suggestion shown in the recommendation history.
 */
@Component({
  selector: 'app-help-me',
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    @if (suggestion(); as s) {
      <div class="suggestion" role="status" aria-live="polite">
        <mat-icon class="bulb">lightbulb</mat-icon>
        <p>{{ s.text }}</p>
      </div>
    }
    <button mat-stroked-button type="button" class="help-btn" (click)="fetch()" [disabled]="loading()">
      @if (loading()) {
        <mat-progress-spinner diameter="18" mode="indeterminate" />
      } @else {
        <mat-icon>{{ suggestion() ? 'refresh' : 'tips_and_updates' }}</mat-icon>
      }
      {{ suggestion() ? 'Another idea' : 'Help Me!' }}
    </button>
  `,
  styles: `
    :host { display: flex; flex-direction: column; gap: 8px; align-items: flex-start; }
    .suggestion {
      display: flex; gap: 12px; align-items: flex-start; width: 100%; box-sizing: border-box;
      padding: 12px 16px; border-radius: 12px;
      background: var(--mat-sys-tertiary-container); color: var(--mat-sys-on-tertiary-container);
    }
    .suggestion p { margin: 0; }
    .bulb { flex: none; }
    .help-btn mat-progress-spinner { display: inline-block; margin-right: 8px; }
  `,
})
export class HelpMe {
  readonly category = input.required<RecommendationCategory>();
  readonly activityId = input<number | null>(null);
  readonly goalId = input<number | null>(null);

  private readonly recommendations = inject(RecommendationService);
  protected readonly suggestion = signal<ShownRecommendation | null>(null);
  protected readonly loading = signal(false);

  fetch(trigger: RecommendationTrigger = 'Manual'): void {
    this.loading.set(true);
    this.recommendations
      .suggest({ category: this.category(), activityId: this.activityId(), goalId: this.goalId(), trigger })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((s) => this.suggestion.set(s));
  }
}
