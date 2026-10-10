import { inject, Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { ConfirmDialog, ConfirmDialogData } from '../components/confirm-dialog';
import { GoalCheckpointDialog, GoalCheckpointDialogData } from './goal-checkpoint-dialog';
import { GradeActivityDialog, GradeActivityDialogData } from './grade-activity-dialog';

/** Central place for opening the app's popups with consistent sizing. */
@Injectable({ providedIn: 'root' })
export class DialogLauncher {
  private readonly dialog = inject(MatDialog);

  gradeActivity(data: GradeActivityDialogData): Observable<boolean | undefined> {
    return this.dialog
      .open<GradeActivityDialog, GradeActivityDialogData, boolean>(GradeActivityDialog, {
        data,
        width: '560px',
        maxWidth: '95vw',
        autoFocus: 'dialog',
      })
      .afterClosed();
  }

  logGoalProgress(data: GoalCheckpointDialogData): Observable<boolean | undefined> {
    return this.dialog
      .open<GoalCheckpointDialog, GoalCheckpointDialogData, boolean>(GoalCheckpointDialog, {
        data,
        width: '560px',
        maxWidth: '95vw',
        autoFocus: 'dialog',
      })
      .afterClosed();
  }

  confirm(data: ConfirmDialogData): Observable<boolean | undefined> {
    return this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data, width: '420px', maxWidth: '95vw' })
      .afterClosed();
  }
}
