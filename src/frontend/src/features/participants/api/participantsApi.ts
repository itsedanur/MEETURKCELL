import apiClient from '../../../api/apiClient';
import { MeetingParticipantDto, AddParticipantRequest, UpdateParticipantRequest } from '../types/participant';

export const participantsApi = {
  getParticipants: async (meetingId: string) => {
    const response = await apiClient.get<MeetingParticipantDto[]>(`/meetings/${meetingId}/participants`);
    return response.data;
  },

  addParticipant: async (meetingId: string, data: AddParticipantRequest) => {
    const response = await apiClient.post<MeetingParticipantDto>(`/meetings/${meetingId}/participants`, data);
    return response.data;
  },

  updateParticipant: async (meetingId: string, participantId: string, data: UpdateParticipantRequest) => {
    const response = await apiClient.put<MeetingParticipantDto>(`/meetings/${meetingId}/participants/${participantId}`, data);
    return response.data;
  },

  removeParticipant: async (meetingId: string, participantId: string) => {
    const response = await apiClient.delete(`/meetings/${meetingId}/participants/${participantId}`);
    return response.data;
  }
};
