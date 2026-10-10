import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { DailyActivity, SCORE_LABELS } from '../../core/models/activity.models';
import { DashboardSummary, TrendPeriod, TrendResponse, TrendSeries } from '../../core/models/dashboard.models';
import { Goal, GOAL_STATUS_LABELS } from '../../core/models/goal.models';
import { ActivityService } from '../../core/services/activity.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { GoalService } from '../../core/services/goal.service';
import { addDays, startOfToday, toIsoDate } from '../../core/utils/date.utils';
import { LineChart, LineSeries } from '../../shared/components/line-chart';
import { DialogLauncher } from '../../shared/dialogs/dialog-launcher.service';

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
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTooltipModule,
    LineChart,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly dashboardService = inject(DashboardService);
  private readonly activityService = inject(ActivityService);
  private readonly goalService = inject(GoalService);
  private readonly dialogs = inject(DialogLauncher);

  protected readonly scoreLabels = SCORE_LABELS;
  protected readonly goalStatusLabels = GOAL_STATUS_LABELS;
  protected readonly periods: TrendPeriod[] = ['Daily', 'Weekly', 'Monthly', 'Annual'];
  protected readonly today = startOfToday();

  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly trends = signal<TrendResponse | null>(null);
  protected readonly dailyActivities = signal<DailyActivity[]>([]);
  protected readonly goals = signal<Goal[]>([]);
  protected readonly loading = signal(false);
  protected readonly trendsLoading = signal(false);

  protected readonly period = signal<TrendPeriod>('Daily');
  protected readonly selectedDate = signal<Date>(startOfToday());
  protected readonly selectedActivityIds = signal<number[]>([]);
  protected readonly selectedGoalIds = signal<number[]>([]);

  protected readonly isToday = computed(() => toIsoDate(this.selectedDate()) === toIsoDate(this.today));
  protected readonly gradedCount = computed(() => this.dailyActivities().filter((a) => a.score !== null).length);
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

  protected changeDate(date: Date | null): void {
    if (!date) return;
    this.selectedDate.set(date > this.today ? this.today : date);
    this.loadDaily();
    this.loadSummary();
  }

  protected shiftDate(days: number): void {
    this.changeDate(addDays(this.selectedDate(), days));
  }

  protected gradeActivity(activity: DailyActivity): void {
    this.dialogs
      .gradeActivity({ activityId: activity.activityId, title: activity.title, date: toIsoDate(this.selectedDate()) })
      .subscribe((saved) => saved && this.refreshAll());
  }

  protected logProgress(goal: Goal): void {
    this.dialogs
      .logGoalProgress({ goalId: goal.id, title: goal.title, currentProgress: goal.progressPercentage })
      .subscribe((saved) => saved && this.refreshAll());
  }

  private refreshAll(): void {
    this.loadOverview();
    this.loadTrends();
  }

  private loadOverview(): void {
    this.loading.set(true);
    forkJoin({
      summary: this.dashboardService.getSummary(toIsoDate(this.selectedDate())),
      daily: this.activityService.getDaily(toIsoDate(this.selectedDate())),
      goals: this.goalService.search(),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ summary, daily, goals }) => {
        this.summary.set(summary);
        this.dailyActivities.set(daily);
        this.goals.set(goals);
      });
  }

  private loadSummary(): void {
    this.dashboardService.getSummary(toIsoDate(this.selectedDate())).subscribe((s) => this.summary.set(s));
  }

  private loadDaily(): void {
    this.activityService.getDaily(toIsoDate(this.selectedDate())).subscribe((d) => this.dailyActivities.set(d));
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
