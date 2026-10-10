import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize } from 'rxjs';
import { Activity, SCORE_LABELS } from '../../core/models/activity.models';
import { ActivityService } from '../../core/services/activity.service';
import { NotificationService } from '../../core/services/notification.service';
import { addDays, fromIsoDate, startOfToday, toIsoDate } from '../../core/utils/date.utils';
import { HelpMe } from '../components/help-me';

export interface GradeActivityDialogData {
  activityId: number;
  title: string;
  /** yyyy-MM-dd; defaults to today. */
  date?: string;
}

/** Popup for grading an activity (0-5) for today or a past day, with a "Help Me!" recommendation button. */
@Component({
  selector: 'app-grade-activity-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressBarModule,
    HelpMe,
  ],
  templateUrl: './grade-activity-dialog.html',
  styleUrl: './dialogs.scss',
})
export class GradeActivityDialog {
  protected readonly data = inject<GradeActivityDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<GradeActivityDialog, boolean>);
  private readonly activities = inject(ActivityService);
  private readonly notifications = inject(NotificationService);

  private readonly helpMe = viewChild.required(HelpMe);

  protected readonly scoreOptions = [0, 1, 2, 3, 4, 5];
  protected readonly scoreLabels = SCORE_LABELS;
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly existingScoreId = signal<number | null>(null);
  protected readonly helpPrompt = signal<string | null>(null);
  protected readonly activity = signal<Activity | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    date: [this.data.date ? fromIsoDate(this.data.date) : startOfToday(), Validators.required],
    score: [null as number | null, Validators.required],
    notes: ['', Validators.maxLength(1000)],
  });

  private readonly selectedDate = toSignal(this.form.controls.date.valueChanges, {
    initialValue: this.form.controls.date.value,
  });

  protected readonly minDate = computed(() => {
    const a = this.activity();
    return a ? fromIsoDate(a.startDate) : null;
  });

  protected readonly maxDate = computed(() => {
    const retired = this.activity()?.retiredDate;
    const today = startOfToday();
    return retired && fromIsoDate(retired) < today ? fromIsoDate(retired) : today;
  });

  protected readonly canGoBack = computed(() => {
    const min = this.minDate();
    const d = this.selectedDate();
    return !!d && (!min || addDays(d, -1) >= min);
  });

  protected readonly canGoForward = computed(() => {
    const d = this.selectedDate();
    return !!d && addDays(d, 1) <= this.maxDate();
  });

  constructor() {
    this.activities.getById(this.data.activityId).subscribe((a) => this.activity.set(a));
    this.loadScore();
    this.form.controls.date.valueChanges.subscribe(() => this.loadScore());
  }

  protected shiftDay(days: number): void {
    const d = this.form.controls.date.value;
    if (d) this.form.controls.date.setValue(addDays(d, days));
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { date, score, notes } = this.form.getRawValue();
    this.saving.set(true);
    this.activities
      .saveScore(this.data.activityId, { evaluationDate: toIsoDate(date), score: score!, notes: notes || null })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe((result) => {
        this.saved.set(true);
        this.existingScoreId.set(result.score.id);
        this.notifications.success(`Saved ${score}/5 for "${this.data.title}"`);
        if (result.suggestHelp) {
          this.helpPrompt.set(result.helpReason ?? 'This activity has been tough lately.');
        } else {
          this.dialogRef.close(true);
        }
      });
  }

  protected acceptHelp(): void {
    this.helpPrompt.set(null);
    this.helpMe().fetch('LowScorePrompt');
  }

  protected close(): void {
    this.dialogRef.close(this.saved());
  }

  private loadScore(): void {
    const date = this.form.controls.date.value;
    if (!date) return;
    const iso = toIsoDate(date);
    this.loading.set(true);
    this.activities
      .getScores(this.data.activityId, iso, iso)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((scores) => {
        const existing = scores[0];
        this.existingScoreId.set(existing?.id ?? null);
        this.form.patchValue({ score: existing?.score ?? null, notes: existing?.notes ?? '' }, { emitEvent: false });
      });
  }
}
