import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { finalize } from 'rxjs';
import { Recommendation, RecommendationCategory } from '../../core/models/recommendation.models';
import { NotificationService } from '../../core/services/notification.service';
import { RecommendationService } from '../../core/services/recommendation.service';

@Component({
  selector: 'app-recommendation-form-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatSlideToggleModule],
  template: `
    <h2 mat-dialog-title>{{ recommendation ? 'Edit recommendation' : 'New recommendation' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form" id="rec-form" (ngSubmit)="save()">
        <mat-form-field appearance="outline">
          <mat-label>Recommendation</mat-label>
          <textarea matInput formControlName="text" rows="3" maxlength="500" cdkFocusInitial></textarea>
          @if (form.controls.text.hasError('required')) { <mat-error>Text is required.</mat-error> }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Applies to</mat-label>
          <mat-select formControlName="category">
            <mat-option value="Activity">Daily activities</mat-option>
            <mat-option value="Goal">Goals</mat-option>
            <mat-option value="General">Both (general)</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-slide-toggle formControlName="isActive">Active</mat-slide-toggle>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" mat-dialog-close>Cancel</button>
      <button mat-flat-button type="submit" form="rec-form" [disabled]="saving()">{{ saving() ? 'Saving...' : 'Save' }}</button>
    </mat-dialog-actions>
  `,
  styles: `.form { display: flex; flex-direction: column; gap: 8px; padding-top: 8px; } mat-form-field { width: 100%; }`,
})
export class RecommendationFormDialog {
  protected readonly recommendation = inject<Recommendation | null>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<RecommendationFormDialog, Recommendation>);
  private readonly service = inject(RecommendationService);
  private readonly notifications = inject(NotificationService);
  protected readonly saving = signal(false);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    text: [this.recommendation?.text ?? '', [Validators.required, Validators.maxLength(500)]],
    category: [this.recommendation?.category ?? ('Activity' as RecommendationCategory), Validators.required],
    isActive: [this.recommendation?.isActive ?? true],
  });

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const request = { ...v, text: v.text.trim() };
    const request$ = this.recommendation
      ? this.service.update(this.recommendation.id, request)
      : this.service.create(request);

    this.saving.set(true);
    request$.pipe(finalize(() => this.saving.set(false))).subscribe((saved) => {
      this.notifications.success('Recommendation saved');
      this.dialogRef.close(saved);
    });
  }
}
