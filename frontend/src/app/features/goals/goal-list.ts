import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { debounceTime, filter, finalize, switchMap } from 'rxjs';
import { Goal, GOAL_STATUS_LABELS, GoalStatus } from '../../core/models/goal.models';
import { GoalService } from '../../core/services/goal.service';
import { NotificationService } from '../../core/services/notification.service';
import { DialogLauncher } from '../../shared/dialogs/dialog-launcher.service';
import { GoalFormDialog } from './goal-form-dialog';

@Component({
  selector: 'app-goal-list',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatProgressBarModule,
    MatSelectModule,
  ],
  templateUrl: './goal-list.html',
  styles: `
    .goal-card { display: flex; flex-direction: column; height: 100%; }
    .goal-card mat-card-content { flex: 1; display: flex; flex-direction: column; gap: 12px; }
    .goal-card mat-card-title a { color: inherit; text-decoration: none; }
    .goal-card mat-card-title a:hover { text-decoration: underline; }
    .meta { display: flex; flex-wrap: wrap; gap: 4px 16px; font: var(--mat-sys-body-small); }
    .description { margin: 0; }
    mat-card-actions { flex-wrap: wrap; }
  `,
})
export class GoalList {
  private readonly service = inject(GoalService);
  private readonly dialog = inject(MatDialog);
  private readonly dialogs = inject(DialogLauncher);
  private readonly notifications = inject(NotificationService);

  protected readonly statusLabels = GOAL_STATUS_LABELS;
  protected readonly statuses = Object.keys(GOAL_STATUS_LABELS) as GoalStatus[];
  protected readonly goals = signal<Goal[]>([]);
  protected readonly loading = signal(false);

  protected readonly filters = inject(FormBuilder).nonNullable.group({
    search: [''],
    status: ['' as GoalStatus | ''],
  });

  constructor() {
    this.load();
    this.filters.valueChanges.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(() => this.load());
  }

  protected add(): void {
    this.openForm(null);
  }

  protected edit(goal: Goal): void {
    this.openForm(goal);
  }

  protected logProgress(goal: Goal): void {
    this.dialogs
      .logGoalProgress({ goalId: goal.id, title: goal.title, currentProgress: goal.progressPercentage })
      .subscribe((saved) => saved && this.load());
  }

  protected complete(goal: Goal): void {
    this.dialogs
      .confirm({ title: 'Mark goal completed?', message: `This logs a 100% checkpoint for "${goal.title}". Congratulations!`, confirmText: 'Mark completed' })
      .pipe(
        filter(Boolean),
        switchMap(() => this.service.complete(goal.id)),
      )
      .subscribe(() => {
        this.notifications.success(`"${goal.title}" completed!`);
        this.load();
      });
  }

  protected remove(goal: Goal): void {
    this.dialogs
      .confirm({
        title: 'Delete goal?',
        message: `"${goal.title}" and all of its progress checkpoints will be permanently deleted.`,
        confirmText: 'Delete',
        danger: true,
      })
      .pipe(
        filter(Boolean),
        switchMap(() => this.service.delete(goal.id)),
      )
      .subscribe(() => {
        this.notifications.success('Goal deleted');
        this.load();
      });
  }

  private openForm(goal: Goal | null): void {
    this.dialog
      .open(GoalFormDialog, { data: goal, width: '520px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((saved) => saved && this.load());
  }

  private load(): void {
    const f = this.filters.getRawValue();
    this.loading.set(true);
    this.service
      .search({ search: f.search.trim() || null, status: f.status || null })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((goals) => this.goals.set(goals));
  }
}
