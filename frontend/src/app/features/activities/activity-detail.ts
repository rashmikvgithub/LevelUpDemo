import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterLink } from '@angular/router';
import { filter, finalize, forkJoin, switchMap } from 'rxjs';
import { Activity, ActivityScore, SCORE_LABELS } from '../../core/models/activity.models';
import { RecommendationHistory } from '../../core/models/recommendation.models';
import { ActivityService } from '../../core/services/activity.service';
import { NotificationService } from '../../core/services/notification.service';
import { RecommendationService } from '../../core/services/recommendation.service';
import { addDays, startOfToday, toIsoDate } from '../../core/utils/date.utils';
import { LineChart, LineSeries } from '../../shared/components/line-chart';
import { DialogLauncher } from '../../shared/dialogs/dialog-launcher.service';
import { ActivityFormDialog } from './activity-form-dialog';

const CHART_DAYS = 30;

@Component({
  selector: 'app-activity-detail',
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    MatTabsModule,
    MatTooltipModule,
    LineChart,
  ],
  templateUrl: './activity-detail.html',
})
export class ActivityDetail {
  /** Route parameter (bound via withComponentInputBinding). */
  readonly id = input.required<string>();

  private readonly service = inject(ActivityService);
  private readonly recommendations = inject(RecommendationService);
  private readonly dialog = inject(MatDialog);
  private readonly dialogs = inject(DialogLauncher);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly scoreLabels = SCORE_LABELS;
  protected readonly scoreColumns = ['evaluationDate', 'score', 'notes', 'actions'];
  protected readonly historyColumns = ['shownDate', 'text', 'trigger'];

  protected readonly activity = signal<Activity | null>(null);
  protected readonly scores = signal<ActivityScore[]>([]);
  protected readonly history = signal<RecommendationHistory[]>([]);
  protected readonly loading = signal(false);

  protected readonly stats = computed(() => {
    const scores = this.scores();
    const count = scores.length;
    const avg = count ? scores.reduce((sum, s) => sum + s.score, 0) / count : null;
    const byDate = new Set(scores.filter((s) => s.score > 0).map((s) => s.evaluationDate));
    let streak = 0;
    let day = startOfToday();
    // Today may not be graded yet; don't break the streak for it.
    if (!byDate.has(toIsoDate(day))) day = addDays(day, -1);
    while (byDate.has(toIsoDate(day))) {
      streak++;
      day = addDays(day, -1);
    }
    const skipped = scores.filter((s) => s.score === 0).length;
    return { count, avg, streak, skipped };
  });

  protected readonly chart = computed(() => {
    const byDate = new Map(this.scores().map((s) => [s.evaluationDate, s.score]));
    const today = startOfToday();
    const labels: string[] = [];
    const data: number[] = [];
    for (let i = CHART_DAYS - 1; i >= 0; i--) {
      const d = addDays(today, -i);
      labels.push(d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }));
      data.push(byDate.get(toIsoDate(d)) ?? 0);
    }
    const series: LineSeries[] = [{ label: 'Score', data, emphasis: true }];
    return { labels, series };
  });

  constructor() {
    effect(() => this.load(Number(this.id())));
  }

  protected grade(date?: string): void {
    const a = this.activity();
    if (!a) return;
    this.dialogs
      .gradeActivity({ activityId: a.id, title: a.title, date })
      .subscribe(() => this.load(a.id));
  }

  protected edit(): void {
    const a = this.activity();
    if (!a) return;
    this.dialog
      .open(ActivityFormDialog, { data: a, width: '520px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((saved) => saved && this.load(a.id));
  }

  protected remove(): void {
    const a = this.activity();
    if (!a) return;
    this.dialogs
      .confirm({
        title: 'Delete activity?',
        message: `"${a.title}" and all of its daily grades will be permanently deleted.`,
        confirmText: 'Delete',
        danger: true,
      })
      .pipe(
        filter(Boolean),
        switchMap(() => this.service.delete(a.id)),
      )
      .subscribe(() => {
        this.notifications.success('Activity deleted');
        this.router.navigate(['/activities']);
      });
  }

  protected deleteScore(score: ActivityScore): void {
    this.dialogs
      .confirm({ title: 'Delete grade?', message: `Remove the grade for ${score.evaluationDate}?`, confirmText: 'Delete', danger: true })
      .pipe(
        filter(Boolean),
        switchMap(() => this.service.deleteScore(score.activityId, score.id)),
      )
      .subscribe(() => {
        this.notifications.success('Grade deleted');
        this.load(score.activityId);
      });
  }

  private load(id: number): void {
    if (!Number.isFinite(id)) return;
    this.loading.set(true);
    forkJoin({
      activity: this.service.getById(id),
      scores: this.service.getScores(id),
      history: this.recommendations.history(id, null, 25),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ activity, scores, history }) => {
          this.activity.set(activity);
          this.scores.set([...scores].sort((a, b) => b.evaluationDate.localeCompare(a.evaluationDate)));
          this.history.set(history);
        },
        error: () => this.router.navigate(['/activities']),
      });
  }
}
