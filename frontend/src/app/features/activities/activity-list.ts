import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterLink } from '@angular/router';
import { debounceTime, filter, finalize, switchMap } from 'rxjs';
import { Activity, ActivityQuery, ActivityStatus } from '../../core/models/activity.models';
import { ActivityService } from '../../core/services/activity.service';
import { NotificationService } from '../../core/services/notification.service';
import { toIsoDate } from '../../core/utils/date.utils';
import { DialogLauncher } from '../../shared/dialogs/dialog-launcher.service';
import { ActivityFormDialog } from './activity-form-dialog';

@Component({
  selector: 'app-activity-list',
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
    MatMenuModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: './activity-list.html',
})
export class ActivityList {
  private readonly service = inject(ActivityService);
  private readonly dialog = inject(MatDialog);
  private readonly dialogs = inject(DialogLauncher);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly columns = ['title', 'status', 'startDate', 'retiredDate', 'actions'];
  protected readonly activities = signal<Activity[]>([]);
  protected readonly loading = signal(false);

  protected readonly filters = inject(FormBuilder).group({
    search: [''],
    status: ['' as ActivityStatus | ''],
    addedFrom: [null as Date | null],
    addedTo: [null as Date | null],
    retiredFrom: [null as Date | null],
    retiredTo: [null as Date | null],
  });

  constructor() {
    this.load();
    this.filters.valueChanges.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(() => this.load());
  }

  protected clearFilters(): void {
    this.filters.reset({ search: '', status: '' });
  }

  protected open(activity: Activity): void {
    this.router.navigate(['/activities', activity.id]);
  }

  protected add(): void {
    this.openForm(null);
  }

  protected edit(activity: Activity): void {
    this.openForm(activity);
  }

  protected grade(activity: Activity): void {
    this.dialogs.gradeActivity({ activityId: activity.id, title: activity.title }).subscribe();
  }

  protected remove(activity: Activity): void {
    this.dialogs
      .confirm({
        title: 'Delete activity?',
        message: `"${activity.title}" and all of its daily grades will be permanently deleted. Consider retiring it instead to keep history.`,
        confirmText: 'Delete',
        danger: true,
      })
      .pipe(
        filter(Boolean),
        switchMap(() => this.service.delete(activity.id)),
      )
      .subscribe(() => {
        this.notifications.success('Activity deleted');
        this.load();
      });
  }

  private openForm(activity: Activity | null): void {
    this.dialog
      .open(ActivityFormDialog, { data: activity, width: '520px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((saved) => saved && this.load());
  }

  private load(): void {
    const f = this.filters.getRawValue();
    const iso = (d: Date | null) => (d ? toIsoDate(d) : null);
    const query: ActivityQuery = {
      search: f.search?.trim() || null,
      status: f.status || null,
      addedFrom: iso(f.addedFrom),
      addedTo: iso(f.addedTo),
      retiredFrom: iso(f.retiredFrom),
      retiredTo: iso(f.retiredTo),
    };
    this.loading.set(true);
    this.service
      .search(query)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((items) => this.activities.set(items));
  }
}
