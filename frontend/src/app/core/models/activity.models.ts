export type ActivityStatus = 'Active' | 'Retired';

export interface Activity {
  id: number;
  title: string;
  description: string | null;
  status: ActivityStatus;
  startDate: string;
  retiredDate: string | null;
  createdDate: string;
  modifiedDate: string | null;
}

export interface CreateActivityRequest {
  title: string;
  description: string | null;
  startDate: string | null;
}

export interface UpdateActivityRequest {
  title: string;
  description: string | null;
  startDate: string;
  status: ActivityStatus;
  retiredDate: string | null;
}

export interface ActivityQuery {
  search?: string | null;
  status?: ActivityStatus | null;
  addedFrom?: string | null;
  addedTo?: string | null;
  retiredFrom?: string | null;
  retiredTo?: string | null;
}

export interface ActivityScore {
  id: number;
  activityId: number;
  title: string;
  description: string | null;
  status: ActivityStatus;
  evaluationDate: string;
  score: number;
  notes: string | null;
  createdDate: string;
  modifiedDate: string | null;
}

export interface SaveActivityScoreRequest {
  evaluationDate: string;
  score: number;
  notes: string | null;
}

export interface SaveActivityScoreResult {
  score: ActivityScore;
  suggestHelp: boolean;
  helpReason: string | null;
}

export interface DailyActivity {
  activityId: number;
  title: string;
  description: string | null;
  status: ActivityStatus;
  date: string;
  scoreId: number | null;
  score: number | null;
  notes: string | null;
}

/** Labels shown for each grade value; 0 means skipped / failed. */
export const SCORE_LABELS: Record<number, string> = {
  0: 'Skipped',
  1: 'Poor',
  2: 'Fair',
  3: 'Good',
  4: 'Great',
  5: 'Excellent',
};
