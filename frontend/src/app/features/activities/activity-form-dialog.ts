import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Observable, finalize } from 'rxjs';
import { Activity, ActivityStatus } from '../../core/models/activity.models';
import { ActivityService } from '../../core/services/activity.service';
import { NotificationService } from '../../core/services/notification.service';
import { fromIsoDate, startOfToday, toIsoDate } from '../../core/utils/date.utils';

/** Add / edit an activity. Pass an existing activity to edit it. */
@Component({
  selector: 'app-activity-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ activity ? 'Edit activity' : 'New activity' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form" id="activity-form" (ngSubmit)="save()">
        <mat-form-field appearance="outline">
          <mat-label>Title</mat-label>
          <input matInput formControlName="title" maxlength="200" placeholder="e.g. Read 10 pages of a book" cdkFocusInitial />
          @if (form.controls.title.hasError('required')) { <mat-error>Title is required.</mat-error> }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Description</mat-label>
          <textarea matInput formControlName="description" rows="3" maxlength="1000"></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Date added (tracking starts)</mat-label>
          <input matInput [matDatepicker]="startPicker" formControlName="startDate" />
          <mat-datepicker-toggle matIconSuffix [for]="startPicker" />
          <mat-datepicker #startPicker />
          <mat-hint>Set an earlier date to back-fill grades for past days.</mat-hint>
        </mat-form-field>

        @if (activity) {
          <mat-button-toggle-group formControlName="status" aria-label="Status">
            <mat-button-toggle value="Active">Active</mat-button-toggle>
            <mat-button-toggle value="Retired">Retired</mat-button-toggle>
          </mat-button-toggle-group>

          @if (status() === 'Retired') {
            <mat-form-field appearance="outline">
              <mat-label>Date retired</mat-label>
              <input matInput [matDatepicker]="retiredPicker" formControlName="retiredDate" [min]="form.controls.startDate.value" />
              <mat-datepicker-toggle matIconSuffix [for]="retiredPicker" />
              <mat-datepicker #retiredPicker />
            </mat-form-field>
          }
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" mat-dialog-close>Cancel</button>
      <button mat-flat-button type="submit" form="activity-form" [disabled]="saving()">
        {{ saving() ? 'Saving...' : 'Save' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `.form { display: flex; flex-direction: column; gap: 8px; padding-top: 8px; } mat-form-field { width: 100%; } mat-button-toggle-group { margin-bottom: 16px; }`,
})
export class ActivityFormDialog {
  protected readonly activity = inject<Activity | null>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ActivityFormDialog, Activity>);
  private readonly service = inject(ActivityService);
  private readonly notifications = inject(NotificationService);
  protected readonly saving = signal(false);

  protected readonly form = inject(FormBuilder).group({
    title: [this.activity?.title ?? '', [Validators.required, Validators.maxLength(200)]],
    description: [this.activity?.description ?? '', Validators.maxLength(1000)],
    startDate: [this.activity ? fromIsoDate(this.activity.startDate) : startOfToday(), Validators.required],
    status: [this.activity?.status ?? ('Active' as ActivityStatus), Validators.required],
    retiredDate: [this.activity?.retiredDate ? fromIsoDate(this.activity.retiredDate) : (null as Date | null)],
  });

  protected readonly status = toSignal(this.form.controls.status.valueChanges, {
    initialValue: this.form.controls.status.value,
  });

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const title = v.title!.trim();
    const description = v.description?.trim() || null;
    const startDate = toIsoDate(v.startDate!);

    let request$: Observable<Activity>;
    if (this.activity) {
      const retired = v.status === 'Retired';
      request$ = this.service.update(this.activity.id, {
        title,
        description,
        startDate,
        status: v.status!,
        retiredDate: retired ? toIsoDate(v.retiredDate ?? startOfToday()) : null,
      });
    } else {
      request$ = this.service.create({ title, description, startDate });
    }

    this.saving.set(true);
    request$.pipe(finalize(() => this.saving.set(false))).subscribe((saved) => {
      this.notifications.success(`Activity "${saved.title}" saved`);
      this.dialogRef.close(saved);
    });
  }
}
