import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { Goal } from '../../core/models/goal.models';
import { GoalService } from '../../core/services/goal.service';
import { NotificationService } from '../../core/services/notification.service';
import { fromIsoDate, toIsoDate } from '../../core/utils/date.utils';

/** Add / edit a long-term goal. */
@Component({
  selector: 'app-goal-form-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatDatepickerModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>{{ goal ? 'Edit goal' : 'New goal' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form" id="goal-form" (ngSubmit)="save()">
        <mat-form-field appearance="outline">
          <mat-label>Title</mat-label>
          <input matInput formControlName="title" maxlength="200" placeholder="e.g. Get AI certification" cdkFocusInitial />
          @if (form.controls.title.hasError('required')) { <mat-error>Title is required.</mat-error> }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Description</mat-label>
          <textarea matInput formControlName="description" rows="3" maxlength="1000"></textarea>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Target date</mat-label>
          <input matInput [matDatepicker]="picker" formControlName="targetDate" />
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" mat-dialog-close>Cancel</button>
      <button mat-flat-button type="submit" form="goal-form" [disabled]="saving()">{{ saving() ? 'Saving...' : 'Save' }}</button>
    </mat-dialog-actions>
  `,
  styles: `.form { display: flex; flex-direction: column; gap: 8px; padding-top: 8px; } mat-form-field { width: 100%; }`,
})
export class GoalFormDialog {
  protected readonly goal = inject<Goal | null>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<GoalFormDialog, Goal>);
  private readonly service = inject(GoalService);
  private readonly notifications = inject(NotificationService);
  protected readonly saving = signal(false);

  protected readonly form = inject(FormBuilder).group({
    title: [this.goal?.title ?? '', [Validators.required, Validators.maxLength(200)]],
    description: [this.goal?.description ?? '', Validators.maxLength(1000)],
    targetDate: [this.goal?.targetDate ? fromIsoDate(this.goal.targetDate) : (null as Date | null)],
  });

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const request = {
      title: v.title!.trim(),
      description: v.description?.trim() || null,
      targetDate: v.targetDate ? toIsoDate(v.targetDate) : null,
    };
    const request$ = this.goal ? this.service.update(this.goal.id, request) : this.service.create(request);

    this.saving.set(true);
    request$.pipe(finalize(() => this.saving.set(false))).subscribe((saved) => {
      this.notifications.success(`Goal "${saved.title}" saved`);
      this.dialogRef.close(saved);
    });
  }
}
