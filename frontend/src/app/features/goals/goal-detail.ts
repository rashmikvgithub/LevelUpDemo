import { DatePipe } from '@angular/common';
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
import { Goal, GOAL_STATUS_LABELS, GoalCheckpoint } from '../../core/models/goal.models';
import { RecommendationHistory } from '../../core/models/recommendation.models';
import { GoalService } from '../../core/services/goal.service';
import { NotificationService } from '../../core/services/notification.service';
import { RecommendationService } from '../../core/services/recommendation.service';
import { fromIsoDate } from '../../core/utils/date.utils';
import { LineChart, LineSeries } from '../../shared/components/line-chart';
import { DialogLauncher } from '../../shared/dialogs/dialog-launcher.service';
import { GoalFormDialog } from './goal-form-dialog';

@Component({
  selector: 'app-goal-detail',
  imports: [
    DatePipe,
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
  templateUrl: './goal-detail.html',
})
export class GoalDetail {
  /** Route parameter (bound via withComponentInputBinding). */
  readonly id = input.required<string>();

  private readonly service = inject(GoalService);
  private readonly recommendations = inject(RecommendationService);
  private readonly dialog = inject(MatDialog);
  private readonly dialogs = inject(DialogLauncher);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly statusLabels = GOAL_STATUS_LABELS;
  protected readonly checkpointColumns = ['checkpointDate', 'percentComplete', 'note', 'actions'];
  protected readonly historyColumns = ['shownDate', 'text'];

  protected readonly goal = signal<Goal | null>(null);
  protected readonly history = signal<RecommendationHistory[]>([]);
  protected readonly loading = signal(false);

  /** Newest first for the table. */
  protected readonly checkpoints = computed<GoalCheckpoint[]>(() =>
    [...(this.goal()?.checkpoints ?? [])].sort(
      (a, b) => b.checkpointDate.localeCompare(a.checkpointDate) || b.createdDate.localeCompare(a.createdDate),
    ),
  );

  protected readonly chart = computed(() => {
    const ordered = [...this.checkpoints()].reverse();
    const labels = ordered.map((c) => fromIsoDate(c.checkpointDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: '2-digit' }));
    const series: LineSeries[] = [{ label: 'Progress %', data: ordered.map((c) => c.percentComplete), emphasis: true }];
    return { labels, series };
  });

  protected readonly daysLeft = computed(() => {
    const target = this.goal()?.targetDate;
    if (!target) return null;
    return Math.ceil((fromIsoDate(target).getTime() - Date.now()) / 86_400_000);
  });

  constructor() {
    effect(() => this.load(Number(this.id())));
  }

  protected logProgress(): void {
    const g = this.goal();
    if (!g) return;
    this.dialogs
      .logGoalProgress({ goalId: g.id, title: g.title, currentProgress: g.progressPercentage })
      .subscribe((saved) => saved && this.load(g.id));
  }

  protected edit(): void {
    const g = this.goal();
    if (!g) return;
    this.dialog
      .open(GoalFormDialog, { data: g, width: '520px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((saved) => saved && this.load(g.id));
  }

  protected complete(): void {
    const g = this.goal();
    if (!g) return;
    this.dialogs
      .confirm({ title: 'Mark goal completed?', message: `This logs a 100% checkpoint for "${g.title}".`, confirmText: 'Mark completed' })
      .pipe(
        filter(Boolean),
        switchMap(() => this.service.complete(g.id)),
      )
      .subscribe(() => {
        this.notifications.success(`"${g.title}" completed!`);
        this.load(g.id);
      });
  }

  protected remove(): void {
    const g = this.goal();
    if (!g) return;
    this.dialogs
      .confirm({ title: 'Delete goal?', message: `"${g.title}" and all of its checkpoints will be permanently deleted.`, confirmText: 'Delete', danger: true })
      .pipe(
        filter(Boolean),
        switchMap(() => this.service.delete(g.id)),
      )
      .subscribe(() => {
        this.notifications.success('Goal deleted');
        this.router.navigate(['/goals']);
      });
  }

  protected deleteCheckpoint(checkpoint: GoalCheckpoint): void {
    this.dialogs
      .confirm({ title: 'Delete checkpoint?', message: 'Goal progress will be recalculated from the remaining checkpoints.', confirmText: 'Delete', danger: true })
      .pipe(
        filter(Boolean),
        switchMap(() => this.service.deleteCheckpoint(checkpoint.goalId, checkpoint.id)),
      )
      .subscribe(() => {
        this.notifications.success('Checkpoint deleted');
        this.load(checkpoint.goalId);
      });
  }

  private load(id: number): void {
    if (!Number.isFinite(id)) return;
    this.loading.set(true);
    forkJoin({ goal: this.service.getById(id), history: this.recommendations.history(null, id, 25) })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ goal, history }) => {
          this.goal.set(goal);
          this.history.set(history);
        },
        error: () => this.router.navigate(['/goals']),
      });
  }
}
