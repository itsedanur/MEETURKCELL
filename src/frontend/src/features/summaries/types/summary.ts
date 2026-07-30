export enum SourceType {
  Manual = 0,
  AI = 1
}

export enum ActionPriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3
}

export enum ActionItemStatus {
  Open = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}

export interface MeetingTopicDto {
  id: string;
  title: string;
  description?: string;
  sortOrder: number;
  canEdit: boolean;
}

export interface MeetingDecisionDto {
  id: string;
  description: string;
  relatedTopic?: string;
  confidenceScore: number;
  requiresReview: boolean;
  evidence?: string;
  sortOrder: number;
  sourceType: SourceType;
  sourceTypeDisplayName: string;
  lastEditedAt?: string;
}

export interface ActionItemDto {
  id: string;
  description: string;
  ownerName?: string;
  ownerEmail?: string;
  dueDate?: string;
  priority: ActionPriority;
  priorityDisplayName: string;
  status: ActionItemStatus;
  statusDisplayName: string;
  confidenceScore: number;
  requiresReview: boolean;
  evidence?: string;
  createdAt: string;
  assignedParticipantId?: string;
  sourceType: SourceType;
  sourceTypeDisplayName: string;
  isOverdue: boolean;
  lastEditedAt?: string;
  completedAt?: string;
  completedBy?: string;
  cancellationReason?: string;
  meetingId?: string;
  meetingTitle?: string;
}

export interface OpenIssueDto {
  id: string;
  description: string;
  ownerName?: string;
  sortOrder: number;
  canEdit: boolean;
}

export interface MeetingSummaryDto {
  meetingId: string;
  summaryId: string;
  version: number;
  meetingPurpose?: string;
  executiveSummary: string;
  isApproved: boolean;
  approvedAt?: string;
  approvedBy?: string;
  manualRevisionNumber: number;
  lastEditedAt?: string;
  lastEditedBy?: string;
  lowConfidenceItemCount: number;
  requiresReview: boolean;
  canEdit: boolean;
  canApprove: boolean;
  provider: string;
  modelName?: string;
  promptVersion: string;
  topics: MeetingTopicDto[];
  decisions: MeetingDecisionDto[];
  actionItems: ActionItemDto[];
  openIssues: OpenIssueDto[];
  createdAt: string;
  updatedAt?: string;
}

export interface UpdateMeetingSummaryRequest {
  meetingPurpose?: string;
  executiveSummary: string;
  expectedVersion: number;
  expectedManualRevisionNumber: number;
}

export interface CreateMeetingTopicRequest {
  title: string;
  description?: string;
  sortOrder: number;
}

export interface UpdateMeetingTopicRequest {
  title: string;
  description?: string;
  sortOrder: number;
}

export interface CreateMeetingDecisionRequest {
  description: string;
  relatedTopic?: string;
  confidenceScore: number;
  evidence?: string;
  sortOrder: number;
}

export interface UpdateMeetingDecisionRequest {
  description: string;
  relatedTopic?: string;
  confidenceScore: number;
  evidence?: string;
  sortOrder: number;
}

export interface CreateOpenIssueRequest {
  description: string;
  ownerName?: string;
  sortOrder: number;
}

export interface UpdateOpenIssueRequest {
  description: string;
  ownerName?: string;
  sortOrder: number;
}
