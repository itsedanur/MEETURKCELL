import apiClient from '../../../api/apiClient';
import { MeetingAnalysisStatusDto } from '../types/analysis';
import { MeetingSummaryDto } from '../../summaries/types/summary';

export const analysisApi = {
  getStatus: async (meetingId: string) => {
    const response = await apiClient.get<MeetingAnalysisStatusDto>(`/analysis/${meetingId}/status`);
    return response.data;
  },

  startAnalysis: async (meetingId: string) => {
    const response = await apiClient.post<{ message: string }>(`/analysis/${meetingId}/start`);
    return response.data;
  },

  getSummary: async (meetingId: string, summaryId?: string) => {
    const url = summaryId ? `/analysis/${meetingId}/summary/${summaryId}` : `/analysis/${meetingId}/summary/latest`;
    const response = await apiClient.get<MeetingSummaryDto>(url);
    return response.data;
  }
};
