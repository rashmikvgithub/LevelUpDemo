import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { DailyActivity, SCORE_LABELS } from '../../core/models/activity.models';
import { ActivityService } from '../../core/services/activity.service';
import { addDays, startOfToday, toIsoDate } from '../../core/utils/date.utils';
import { DialogLauncher } from '../../shared/dialogs/dialog-launcher.service';
import { ActivityFormDialog } from '../activities/activity-form-dialog';

@Component({
  selector: 'app-daily-activities',
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: './daily-activities.html',
  styleUrl: './daily-activities.scss',
})
export class DailyActivities {
  private readonly activityService = inject(ActivityService);
  private readonly dialogs = inject(DialogLauncher);
  private readonly dialog = inject(MatDialog);

  readonly embedded = input(false);
  readonly dateChanged = output<Date>();
  readonly activitiesChanged = output<void>();

  protected readonly scoreLabels = SCORE_LABELS;
  protected readonly today = startOfToday();
  protected readonly selectedDate = signal<Date>(startOfToday());
  protected readonly dailyActivities = signal<DailyActivity[]>([]);
  protected readonly loading = signal(false);

  protected readonly isToday = computed(() => toIsoDate(this.selectedDate()) === toIsoDate(this.today));
  protected readonly gradedCount = computed(() => this.dailyActivities().filter((activity) => activity.score !== null).length);

  constructor() {
    this.load();
  }

  protected changeDate(date: Date | null): void {
    if (!date) return;

    const selectedDate = date > this.today ? this.today : date;
    this.selectedDate.set(selectedDate);
    this.dateChanged.emit(selectedDate);
    this.load();
  }

  protected shiftDate(days: number): void {
    this.changeDate(addDays(this.selectedDate(), days));
  }

  protected gradeActivity(activity: DailyActivity): void {
    this.dialogs
      .gradeActivity({
        activityId: activity.activityId,
        title: activity.title,
        date: toIsoDate(this.selectedDate()),
      })
      .subscribe((saved) => {
        if (!saved) return;
        this.load();
        this.activitiesChanged.emit();
      });
  }

  protected addActivity(): void {
    this.dialog
      .open(ActivityFormDialog, { data: null, width: '520px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((saved) => {
        if (!saved) return;
        this.load();
        this.activitiesChanged.emit();
      });
  }

  private load(): void {
    this.loading.set(true);
    this.activityService
      .getDaily(toIsoDate(this.selectedDate()))
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((activities) => this.dailyActivities.set(activities));
  }
}
