import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { catchError, debounceTime, forkJoin, map, Observable, of, Subject, switchMap, tap } from 'rxjs';
import { Activity, ActivityScore, SCORE_LABELS } from '../../core/models/activity.models';
import { ShownRecommendation } from '../../core/models/recommendation.models';
import { ActivityService } from '../../core/services/activity.service';
import { NotificationService } from '../../core/services/notification.service';
import { RecommendationService } from '../../core/services/recommendation.service';
import { addDays, startOfToday, toIsoDate } from '../../core/utils/date.utils';

const DEFAULT_RANGE_DAYS = 29;
const MILLISECONDS_PER_DAY = 86_400_000;

function countInclusiveDays(from: Date, to: Date): number {
  const fromUtc = Date.UTC(from.getFullYear(), from.getMonth(), from.getDate());
  const toUtc = Date.UTC(to.getFullYear(), to.getMonth(), to.getDate());
  return Math.max(1, Math.round((toUtc - fromUtc) / MILLISECONDS_PER_DAY) + 1);
}

@Component({
  selector: 'app-score-insights',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: './score-insights.html',
  styleUrl: './score-insights.scss',
})
export class ScoreInsights {
  private readonly service = inject(ActivityService);
  private readonly recommendations = inject(RecommendationService);
  private readonly notifications = inject(NotificationService);
  private readonly searchTrigger = new Subject<void>();
  /** Emits the activity to get an idea for, or null to cancel any in-flight request and clear the suggestion. */
  private readonly suggestTrigger = new Subject<number | null>();

  protected readonly scoreLabels = SCORE_LABELS;
  protected readonly columns = ['evaluationDate', 'title', 'score', 'notes'];
  protected readonly today = startOfToday();

  protected readonly activities = signal<Activity[]>([]);
  protected readonly results = signal<ActivityScore[]>([]);
  protected readonly loading = signal(false);
  protected readonly selectedActivityId = signal<number | null>(null);
  protected readonly suggestion = signal<ShownRecommendation | null>(null);
  protected readonly suggestionLoading = signal(false);
  protected readonly analysisVisible = signal(false);

  protected readonly filters = inject(FormBuilder).group({
    activityId: [null as number | null],
    from: [addDays(startOfToday(), -DEFAULT_RANGE_DAYS) as Date | null],
    to: [startOfToday() as Date | null],
  });

  protected readonly lowScoreCount = computed(() => this.results().filter((score) => score.score <= 4).length);

  protected readonly selectedActivity = computed(() => {
    const id = this.selectedActivityId();
    return id === null ? null : (this.activities().find((activity) => activity.id === id) ?? null);
  });

  protected readonly scoreAnalysis = computed(() => {
    const activity = this.selectedActivity();
    if (!activity || !this.analysisVisible() || this.loading()) {
      return null;
    }

    const { from, to } = this.filters.getRawValue();
    const periodDays = from && to ? countInclusiveDays(from, to) : null;
    const scores = this.results().filter((score) => score.activityId === activity.id);

    if (scores.length === 0) {
      return {
        activity,
        latestScore: null,
        periodStart: from,
        periodEnd: to,
        periodDays,
        missedSummary: `No scores were recorded for ${activity.title} in the selected date range.`,
        averageSummary: null,
      };
    }

    const missedCount = scores.filter((score) => score.score === 0).length;
    const average = scores.reduce((sum, score) => sum + score.score, 0) / scores.length;

    return {
      activity,
      latestScore: scores[0].score,
      periodStart: from,
      periodEnd: to,
      periodDays,
      missedSummary:
        missedCount === 0
          ? `${activity.title} was not missed in the selected date range.`
          : `${activity.title} was missed ${missedCount} ${missedCount === 1 ? 'time' : 'times'} in the selected date range.`,
      averageSummary: `Your average score for ${activity.title} in this date range is ${average.toFixed(1)}/5.`,
    };
  });

  constructor() {
    this.searchTrigger
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap(() => this.buildSearch()),
        takeUntilDestroyed(),
      )
      .subscribe((scores) => {
        this.results.set(scores);
        this.loading.set(false);
      });

    this.loading.set(true);
    this.service.search().subscribe({
      next: (activities) => {
        this.activities.set(activities);
        this.searchTrigger.next();
      },
      error: () => this.loading.set(false),
    });

    this.filters.valueChanges
      .pipe(
        tap(() => this.loading.set(true)),
        debounceTime(300),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.searchTrigger.next());

    // A suggestion and its analysis belong to one activity, so cancel any in-flight request
    // and drop both as soon as the activity filter changes.
    this.filters.controls.activityId.valueChanges.pipe(takeUntilDestroyed()).subscribe((activityId) => {
      this.selectedActivityId.set(activityId ?? null);
      this.analysisVisible.set(false);
      this.suggestTrigger.next(null);
    });

    this.suggestTrigger
      .pipe(
        tap((activityId) => this.suggestionLoading.set(activityId !== null)),
        switchMap((activityId) =>
          activityId === null
            ? of(null)
            : this.recommendations
                .suggest({ category: 'Activity', activityId, trigger: 'Manual' })
                .pipe(catchError(() => of(null))),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((suggestion) => {
        this.suggestion.set(suggestion);
        this.suggestionLoading.set(false);
      });
  }

  /** Asks the recommendation engine for an idea about the selected activity and reveals its score analysis. */
  protected getRecommendation(): void {
    const activityId = this.selectedActivityId();
    if (activityId === null) {
      this.notifications.error('Select an activity first to get an AI recommendation.');
      return;
    }

    this.analysisVisible.set(true);
    this.suggestTrigger.next(activityId);
  }

  protected clearFilters(): void {
    this.filters.reset({
      activityId: null,
      from: addDays(this.today, -DEFAULT_RANGE_DAYS),
      to: this.today,
    });
  }

  /**
   * Builds the request for the current filters. Used with switchMap so a newer search
   * cancels any in-flight request and stale responses can never overwrite the results.
   */
  private buildSearch(): Observable<ActivityScore[]> {
    const { activityId, from, to } = this.filters.getRawValue();
    const activityIds = activityId ? [activityId] : this.activities().map((activity) => activity.id);

    if (activityIds.length === 0) {
      return of([]);
    }

    const fromIso = from ? toIsoDate(from) : undefined;
    const toIso = to ? toIsoDate(to) : undefined;

    return forkJoin(activityIds.map((id) => this.service.getScores(id, fromIso, toIso))).pipe(
      map((batches) =>
        batches
          .flat()
          .sort((a, b) => b.evaluationDate.localeCompare(a.evaluationDate) || a.title.localeCompare(b.title)),
      ),
      catchError(() => of<ActivityScore[]>([])),
    );
  }
}
