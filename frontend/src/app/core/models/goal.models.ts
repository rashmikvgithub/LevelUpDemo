export type GoalStatus = 'NotStarted' | 'InProgress' | 'Completed';

export interface GoalCheckpoint {
  id: number;
  goalId: number;
  checkpointDate: string;
  percentComplete: number;
  note: string | null;
  createdDate: string;
}

export interface Goal {
  id: number;
  title: string;
  description: string | null;
  targetDate: string | null;
  progressPercentage: number;
  status: GoalStatus;
  completedDate: string | null;
  lastCheckpointDate: string | null;
  checkpointCount: number;
  createdDate: string;
  modifiedDate: string | null;
  checkpoints: GoalCheckpoint[] | null;
}

export interface SaveGoalRequest {
  title: string;
  description: string | null;
  targetDate: string | null;
}

export interface CreateGoalCheckpointRequest {
  percentComplete: number;
  note: string | null;
  checkpointDate: string | null;
}

export interface GoalQuery {
  search?: string | null;
  status?: GoalStatus | null;
}

export const GOAL_STATUS_LABELS: Record<GoalStatus, string> = {
  NotStarted: 'Not started',
  InProgress: 'In progress',
  Completed: 'Completed',
};
