import { ActionPriority, ActionItemStatus } from '../../summaries/types/summary';

export interface CreateActionItemRequest {
  description: string;
  assignedParticipantId?: string;
  ownerName?: string;
  ownerEmail?: string;
  dueDate?: string;
  priority: ActionPriority;
  status?: ActionItemStatus;
  confidenceScore?: number;
  evidence?: string;
}

export interface UpdateActionItemRequest {
  description: string;
  assignedParticipantId?: string;
  ownerName?: string;
  ownerEmail?: string;
  dueDate?: string;
  priority: ActionPriority;
  confidenceScore: number;
  evidence?: string;
  expectedUpdatedAt?: string;
}

export interface UpdateActionItemStatusRequest {
  status: ActionItemStatus;
  cancellationReason?: string;
  expectedUpdatedAt?: string;
}
