import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { filter, finalize, switchMap } from 'rxjs';
import { Recommendation, RecommendationCategory, RecommendationHistory } from '../../core/models/recommendation.models';
import { NotificationService } from '../../core/services/notification.service';
import { RecommendationService } from '../../core/services/recommendation.service';
import { DialogLauncher } from '../../shared/dialogs/dialog-launcher.service';
import { RecommendationFormDialog } from './recommendation-form-dialog';

@Component({
  selector: 'app-recommendation-list',
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatTableModule,
    MatTabsModule,
    MatTooltipModule,
  ],
  templateUrl: './recommendation-list.html',
})
export class RecommendationList {
  private readonly service = inject(RecommendationService);
  private readonly dialog = inject(MatDialog);
  private readonly dialogs = inject(DialogLauncher);
  private readonly notifications = inject(NotificationService);

  protected readonly columns = ['text', 'category', 'isActive', 'actions'];
  protected readonly historyColumns = ['shownDate', 'text', 'context', 'trigger'];
  protected readonly categoryLabels: Record<string, string> = {
    Activity: 'Daily activities',
    Goal: 'Goals',
    General: 'General',
  };

  protected readonly category = signal<RecommendationCategory | ''>('');
  protected readonly recommendations = signal<Recommendation[]>([]);
  protected readonly history = signal<RecommendationHistory[]>([]);
  protected readonly loading = signal(false);
  protected readonly activeCount = computed(() => this.recommendations().filter((r) => r.isActive).length);

  constructor() {
    this.load();
    this.loadHistory();
  }

  protected setCategory(category: RecommendationCategory | ''): void {
    this.category.set(category);
    this.load();
  }

  protected add(): void {
    this.openForm(null);
  }

  protected edit(rec: Recommendation): void {
    this.openForm(rec);
  }

  protected toggleActive(rec: Recommendation, isActive: boolean): void {
    this.service.update(rec.id, { text: rec.text, category: rec.category, isActive }).subscribe({
      next: (updated) => this.recommendations.update((list) => list.map((r) => (r.id === updated.id ? updated : r))),
      error: () => this.load(),
    });
  }

  protected remove(rec: Recommendation): void {
    this.dialogs
      .confirm({
        title: 'Delete recommendation?',
        message: 'It will no longer be suggested. Past history entries keep their text.',
        confirmText: 'Delete',
        danger: true,
      })
      .pipe(
        filter(Boolean),
        switchMap(() => this.service.delete(rec.id)),
      )
      .subscribe(() => {
        this.notifications.success('Recommendation deleted');
        this.load();
      });
  }

  private openForm(rec: Recommendation | null): void {
    this.dialog
      .open(RecommendationFormDialog, { data: rec, width: '520px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((saved) => saved && this.load());
  }

  private load(): void {
    this.loading.set(true);
    this.service
      .list(this.category() || null, true)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((items) => this.recommendations.set(items));
  }

  protected loadHistory(): void {
    this.service.history(null, null, 100).subscribe((h) => this.history.set(h));
  }
}
