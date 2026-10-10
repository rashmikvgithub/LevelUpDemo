export type TrendPeriod = 'Daily' | 'Weekly' | 'Monthly' | 'Annual';

export interface ScoreSummary {
  from: string;
  to: string;
  points: number;
  possiblePoints: number;
  percentage: number;
  averageScore: number | null;
  evaluationCount: number;
}

export interface DashboardSummary {
  date: string;
  today: ScoreSummary;
  week: ScoreSummary;
  month: ScoreSummary;
  activeActivities: number;
  activeGoals: number;
  notStartedGoals: number;
  completedGoals: number;
  averageGoalProgress: number;
}

export interface TrendSeries {
  id: number | null;
  name: string;
  values: number[];
}

export interface TrendResponse {
  period: TrendPeriod;
  labels: string[];
  bucketStarts: string[];
  activitiesTotal: TrendSeries;
  activities: TrendSeries[];
  goalsTotal: TrendSeries;
  goals: TrendSeries[];
}
