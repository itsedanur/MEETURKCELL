import apiClient from '../../../api/apiClient';
import { ActionItemDto } from '../../summaries/types/summary';
import { 
  CreateActionItemRequest, 
  UpdateActionItemRequest, 
  UpdateActionItemStatusRequest 
} from '../types/action';

export const actionsApi = {
  addAction: async (meetingId: string, summaryId: string, data: CreateActionItemRequest) => {
    const response = await apiClient.post<ActionItemDto>(`/analysis/${meetingId}/summary/${summaryId}/actions`, data);
    return response.data;
  },

  updateAction: async (meetingId: string, summaryId: string, actionId: string, data: UpdateActionItemRequest) => {
    const response = await apiClient.put<ActionItemDto>(`/analysis/${meetingId}/summary/${summaryId}/actions/${actionId}`, data);
    return response.data;
  },

  updateActionStatus: async (meetingId: string, summaryId: string, actionId: string, data: UpdateActionItemStatusRequest) => {
    const response = await apiClient.patch<ActionItemDto>(`/analysis/${meetingId}/summary/${summaryId}/actions/${actionId}/status`, data);
    return response.data;
  },

  deleteAction: async (meetingId: string, summaryId: string, actionId: string) => {
    const response = await apiClient.delete(`/analysis/${meetingId}/summary/${summaryId}/actions/${actionId}`);
    return response.data;
  }
};
