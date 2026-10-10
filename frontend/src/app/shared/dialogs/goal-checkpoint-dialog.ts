import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSliderModule } from '@angular/material/slider';
import { finalize } from 'rxjs';
import { GoalService } from '../../core/services/goal.service';
import { NotificationService } from '../../core/services/notification.service';
import { startOfToday, toIsoDate } from '../../core/utils/date.utils';
import { HelpMe } from '../components/help-me';

export interface GoalCheckpointDialogData {
  goalId: number;
  title: string;
  currentProgress: number;
}

/** Popup for logging a % progress checkpoint (with a note) for a goal, with a "Help Me!" button. */
@Component({
  selector: 'app-goal-checkpoint-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressBarModule,
    MatSliderModule,
    HelpMe,
  ],
  template: `
    <h2 mat-dialog-title><mat-icon class="title-icon">flag</mat-icon> Progress: {{ data.title }}</h2>

    <mat-dialog-content>
      <div class="current-progress">
        <span>Current progress: {{ data.currentProgress }}%</span>
        <mat-progress-bar mode="determinate" [value]="data.currentProgress" />
      </div>

      <form [formGroup]="form" class="dialog-form" (ngSubmit)="save()" id="checkpoint-form">
        <span class="field-label">New progress</span>
        <div class="percent-row">
          <mat-slider min="0" max="100" step="5" discrete [displayWith]="formatPercent">
            <input
              matSliderThumb
              [value]="percent()"
              (valueChange)="form.controls.percentComplete.setValue($event)"
              aria-label="Percent complete"
            />
          </mat-slider>
          <mat-form-field appearance="outline" class="percent-input" subscriptSizing="dynamic">
            <input matInput type="number" min="0" max="100" formControlName="percentComplete" aria-label="Percent complete value" />
            <span matTextSuffix>%</span>
          </mat-form-field>
        </div>
        @if (form.controls.percentComplete.invalid) {
          <mat-error>Enter a value between 0 and 100.</mat-error>
        }

        <mat-form-field appearance="outline">
          <mat-label>Checkpoint date</mat-label>
          <input matInput [matDatepicker]="picker" formControlName="checkpointDate" [max]="today" />
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Note</mat-label>
          <textarea matInput formControlName="note" rows="3" maxlength="1000"
                    placeholder="What did you accomplish? What's next?"></textarea>
        </mat-form-field>
      </form>

      <app-help-me category="Goal" [goalId]="data.goalId" />
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button type="button" mat-dialog-close>Cancel</button>
      <button mat-flat-button type="submit" form="checkpoint-form" [disabled]="saving()">
        {{ saving() ? 'Saving...' : 'Log progress' }}
      </button>
    </mat-dialog-actions>
  `,
  styleUrl: './dialogs.scss',
})
export class GoalCheckpointDialog {
  protected readonly data = inject<GoalCheckpointDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<GoalCheckpointDialog, boolean>);
  private readonly goals = inject(GoalService);
  private readonly notifications = inject(NotificationService);

  protected readonly today = startOfToday();
  protected readonly saving = signal(false);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    percentComplete: [this.data.currentProgress, [Validators.required, Validators.min(0), Validators.max(100)]],
    checkpointDate: [startOfToday(), Validators.required],
    note: ['', Validators.maxLength(1000)],
  });

  protected formatPercent = (value: number) => `${value}%`;

  /** Keeps the slider in sync when the numeric input is typed into. */
  protected readonly percent = toSignal(this.form.controls.percentComplete.valueChanges, {
    initialValue: this.form.controls.percentComplete.value,
  });

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { percentComplete, checkpointDate, note } = this.form.getRawValue();
    this.saving.set(true);
    this.goals
      .addCheckpoint(this.data.goalId, {
        percentComplete: Math.round(Number(percentComplete)),
        checkpointDate: toIsoDate(checkpointDate),
        note: note || null,
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe(() => {
        this.notifications.success(`Logged ${percentComplete}% for "${this.data.title}"`);
        this.dialogRef.close(true);
      });
  }
}
