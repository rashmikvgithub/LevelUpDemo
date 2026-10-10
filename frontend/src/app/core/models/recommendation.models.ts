export type RecommendationCategory = 'General' | 'Activity' | 'Goal';
export type RecommendationTrigger = 'Manual' | 'LowScorePrompt';

export interface Recommendation {
  id: number;
  text: string;
  category: RecommendationCategory;
  isActive: boolean;
  createdDate: string;
  modifiedDate: string | null;
}

export interface SaveRecommendationRequest {
  text: string;
  category: RecommendationCategory;
  isActive: boolean;
}

export interface RecommendationRequest {
  category: RecommendationCategory;
  activityId?: number | null;
  goalId?: number | null;
  trigger?: RecommendationTrigger;
}

export interface ShownRecommendation {
  historyId: number;
  recommendationId: number;
  text: string;
  category: RecommendationCategory;
  shownDate: string;
}

export interface RecommendationHistory {
  id: number;
  recommendationId: number | null;
  text: string;
  activityId: number | null;
  activityTitle: string | null;
  goalId: number | null;
  goalTitle: string | null;
  trigger: RecommendationTrigger;
  shownDate: string;
}
