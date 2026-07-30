import apiClient from '../../../api/apiClient';
import { 
  UpdateMeetingSummaryRequest, 
  CreateMeetingTopicRequest, 
  UpdateMeetingTopicRequest,
  CreateMeetingDecisionRequest,
  UpdateMeetingDecisionRequest,
  CreateOpenIssueRequest,
  UpdateOpenIssueRequest,
  MeetingTopicDto,
  MeetingDecisionDto,
  OpenIssueDto
} from '../types/summary';

export const summariesApi = {
  updateSummary: async (meetingId: string, summaryId: string, data: UpdateMeetingSummaryRequest) => {
    const response = await apiClient.put(`/analysis/${meetingId}/summary/${summaryId}`, data);
    return response.data;
  },

  // Topics
  addTopic: async (meetingId: string, summaryId: string, data: CreateMeetingTopicRequest) => {
    const response = await apiClient.post<MeetingTopicDto>(`/analysis/${meetingId}/summary/${summaryId}/topics`, data);
    return response.data;
  },

  updateTopic: async (meetingId: string, summaryId: string, topicId: string, data: UpdateMeetingTopicRequest) => {
    const response = await apiClient.put<MeetingTopicDto>(`/analysis/${meetingId}/summary/${summaryId}/topics/${topicId}`, data);
    return response.data;
  },

  deleteTopic: async (meetingId: string, summaryId: string, topicId: string) => {
    const response = await apiClient.delete(`/analysis/${meetingId}/summary/${summaryId}/topics/${topicId}`);
    return response.data;
  },

  // Decisions
  addDecision: async (meetingId: string, summaryId: string, data: CreateMeetingDecisionRequest) => {
    const response = await apiClient.post<MeetingDecisionDto>(`/analysis/${meetingId}/summary/${summaryId}/decisions`, data);
    return response.data;
  },

  updateDecision: async (meetingId: string, summaryId: string, decisionId: string, data: UpdateMeetingDecisionRequest) => {
    const response = await apiClient.put<MeetingDecisionDto>(`/analysis/${meetingId}/summary/${summaryId}/decisions/${decisionId}`, data);
    return response.data;
  },

  deleteDecision: async (meetingId: string, summaryId: string, decisionId: string) => {
    const response = await apiClient.delete(`/analysis/${meetingId}/summary/${summaryId}/decisions/${decisionId}`);
    return response.data;
  },

  // Open Issues
  addOpenIssue: async (meetingId: string, summaryId: string, data: CreateOpenIssueRequest) => {
    const response = await apiClient.post<OpenIssueDto>(`/analysis/${meetingId}/summary/${summaryId}/open-issues`, data);
    return response.data;
  },

  updateOpenIssue: async (meetingId: string, summaryId: string, openIssueId: string, data: UpdateOpenIssueRequest) => {
    const response = await apiClient.put<OpenIssueDto>(`/analysis/${meetingId}/summary/${summaryId}/open-issues/${openIssueId}`, data);
    return response.data;
  },

  deleteOpenIssue: async (meetingId: string, summaryId: string, openIssueId: string) => {
    const response = await apiClient.delete(`/analysis/${meetingId}/summary/${summaryId}/open-issues/${openIssueId}`);
    return response.data;
  }
};
