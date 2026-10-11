import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { DashboardSummary, TrendPeriod, TrendResponse, TrendSeries } from '../../core/models/dashboard.models';
import { Goal, GOAL_STATUS_LABELS } from '../../core/models/goal.models';
import { DashboardService } from '../../core/services/dashboard.service';
import { GoalService } from '../../core/services/goal.service';
import { startOfToday, toIsoDate } from '../../core/utils/date.utils';
import { LineChart, LineSeries } from '../../shared/components/line-chart';
import { DialogLauncher } from '../../shared/dialogs/dialog-launcher.service';
import { DailyActivities } from '../daily-activities/daily-activities';
import { GoalFormDialog } from '../goals/goal-form-dialog';

@Component({
  selector: 'app-dashboard',
  imports: [
    DatePipe,
    DecimalPipe,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatProgressBarModule,
    MatSelectModule,
    DailyActivities,
    LineChart,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly dashboardService = inject(DashboardService);
  private readonly goalService = inject(GoalService);
  private readonly dialogs = inject(DialogLauncher);
  private readonly dialog = inject(MatDialog);

  protected readonly goalStatusLabels = GOAL_STATUS_LABELS;
  protected readonly periods: TrendPeriod[] = ['Daily', 'Weekly', 'Monthly', 'Annual'];
  protected readonly today = startOfToday();

  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly trends = signal<TrendResponse | null>(null);
  protected readonly goals = signal<Goal[]>([]);
  protected readonly loading = signal(false);
  protected readonly trendsLoading = signal(false);

  protected readonly period = signal<TrendPeriod>('Daily');
  protected readonly selectedDate = signal<Date>(startOfToday());
  protected readonly selectedActivityIds = signal<number[]>([]);
  protected readonly selectedGoalIds = signal<number[]>([]);

  protected readonly isToday = computed(() => toIsoDate(this.selectedDate()) === toIsoDate(this.today));
  protected readonly activeGoals = computed(() => this.goals().filter((g) => g.status !== 'Completed'));

  protected readonly scoreCards = computed(() => {
    const s = this.summary();
    if (!s) return [];
    const dayLabel = this.isToday() ? "Today's score" : `Score on ${this.selectedDate().toLocaleDateString()}`;
    return [
      { label: dayLabel, data: s.today, icon: 'today' },
      { label: 'Weekly score', data: s.week, icon: 'date_range' },
      { label: 'Monthly score', data: s.month, icon: 'calendar_month' },
    ];
  });

  protected readonly activityChart = computed(() =>
    this.buildSeries(this.trends()?.activitiesTotal, this.trends()?.activities ?? [], this.selectedActivityIds()),
  );
  protected readonly goalChart = computed(() =>
    this.buildSeries(this.trends()?.goalsTotal, this.trends()?.goals ?? [], this.selectedGoalIds()),
  );

  constructor() {
    this.loadOverview();
    this.loadTrends();
  }

  protected setPeriod(period: TrendPeriod): void {
    this.period.set(period);
    this.loadTrends();
  }

  protected changeDate(date: Date): void {
    this.selectedDate.set(date);
    this.loadSummary();
  }

  protected logProgress(goal: Goal): void {
    this.dialogs
      .logGoalProgress({ goalId: goal.id, title: goal.title, currentProgress: goal.progressPercentage })
      .subscribe((saved) => saved && this.refreshAll());
  }

  protected addGoal(): void {
    this.dialog
      .open(GoalFormDialog, { data: null, width: '520px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((saved) => saved && this.refreshAll());
  }

  protected refreshAll(): void {
    this.loadOverview();
    this.loadTrends();
  }

  private loadOverview(): void {
    this.loading.set(true);
    forkJoin({
      summary: this.dashboardService.getSummary(toIsoDate(this.selectedDate())),
      goals: this.goalService.search(),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ summary, goals }) => {
        this.summary.set(summary);
        this.goals.set(goals);
      });
  }

  private loadSummary(): void {
    this.dashboardService.getSummary(toIsoDate(this.selectedDate())).subscribe((s) => this.summary.set(s));
  }

  private loadTrends(): void {
    this.trendsLoading.set(true);
    this.dashboardService
      .getTrends(this.period())
      .pipe(finalize(() => this.trendsLoading.set(false)))
      .subscribe((t) => this.trends.set(t));
  }

  private buildSeries(total: TrendSeries | undefined, items: TrendSeries[], selectedIds: number[]): LineSeries[] {
    if (!total) return [];
    const selected = items
      .filter((s) => s.id !== null && selectedIds.includes(s.id))
      .map((s) => ({ label: s.name, data: s.values }));
    return [{ label: total.name, data: total.values, emphasis: true }, ...selected];
  }
}
