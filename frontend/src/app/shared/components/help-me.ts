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
        <span class="bulb-badge" aria-hidden="true">
          <mat-icon class="bulb">lightbulb</mat-icon>
        </span>
        <div class="suggestion-body">
          <p class="suggestion-label">Your idea</p>
          <p class="suggestion-text">{{ s.text }}</p>
        </div>
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
    :host { display: flex; flex-direction: column; gap: 12px; align-items: flex-start; }
    .suggestion {
      display: flex; gap: 16px; align-items: flex-start; width: 100%; box-sizing: border-box;
      padding: 18px 20px; border: 2px solid var(--pixel-ink); border-radius: 0;
      background: var(--mat-sys-tertiary-container); color: var(--mat-sys-on-tertiary-container);
      box-shadow: 4px 4px 0 rgba(32, 49, 60, 0.35);
    }
    .suggestion-body { display: flex; flex-direction: column; gap: 10px; min-width: 0; }
    .suggestion-label {
      margin: 0; font-size: 10px; letter-spacing: 1px; text-transform: uppercase;
      color: var(--mat-sys-on-tertiary-container); opacity: 0.75;
    }
    .suggestion-text {
      margin: 0; font-size: 11px; line-height: 2.1; letter-spacing: 0.4px;
      overflow-wrap: anywhere; white-space: pre-line;
    }
    .bulb-badge {
      flex: none; display: grid; place-items: center; width: 36px; height: 36px;
      border: 2px solid var(--pixel-ink); background: var(--pixel-gold);
      box-shadow: 2px 2px 0 rgba(32, 49, 60, 0.35);
    }
    .bulb { flex: none; color: var(--pixel-ink); }
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
