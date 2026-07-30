import apiClient from '../../../api/apiClient';
import { PagedResult, ApiResponse } from '../../../api/apiResponse';
import { MeetingDto, MeetingFilterDto, CreateMeetingRequest, UpdateMeetingRequest } from '../types/meeting';

export const meetingsApi = {
  getMeetings: async (filter: MeetingFilterDto) => {
    // Convert filter object to URLSearchParams
    const params = new URLSearchParams();
    Object.entries(filter).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params.append(key, value.toString());
      }
    });
    
    const response = await apiClient.get<PagedResult<MeetingDto>>(`/meetings?${params.toString()}`);
    // NOTE: Because our generic ApiResponse structure might differ from PagedResult root,
    // usually backend returns PagedResult directly or wrapped. Let's assume it returns PagedResult directly 
    // since the controller method is likely returning Ok(PaginatedList).
    return response.data;
  },

  getMeeting: async (id: string) => {
    const response = await apiClient.get<MeetingDto>(`/meetings/${id}`);
    return response.data;
  },

  createMeeting: async (data: CreateMeetingRequest) => {
    const response = await apiClient.post<MeetingDto>('/meetings', data);
    return response.data;
  },

  updateMeeting: async (id: string, data: UpdateMeetingRequest) => {
    const response = await apiClient.put<MeetingDto>(`/meetings/${id}`, data);
    return response.data;
  },

  archiveMeeting: async (id: string) => {
    const response = await apiClient.post(`/meetings/${id}/archive`);
    return response.data;
  },

  restoreMeeting: async (id: string) => {
    const response = await apiClient.post(`/meetings/${id}/restore`);
    return response.data;
  },

  deleteMeeting: async (id: string) => {
    const response = await apiClient.delete(`/meetings/${id}`);
    return response.data;
  },

  approveSummary: async (id: string, data: { expectedVersion: number; expectedManualRevisionNumber: number; confirmation: boolean; }) => {
    const response = await apiClient.post(`/meetings/${id}/approve`, data);
    return response.data;
  },

  revokeApproval: async (id: string, data: { reason: string; }) => {
    const response = await apiClient.post(`/meetings/${id}/revoke-approval`, data);
    return response.data;
  }
};
