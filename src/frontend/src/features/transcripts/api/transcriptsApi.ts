import apiClient from '../../../api/apiClient';
import { TranscriptDto, ManualTranscriptRequest, UploadTranscriptRequest } from '../types/transcript';

export const transcriptsApi = {
  getTranscript: async (meetingId: string) => {
    try {
      const response = await apiClient.get<TranscriptDto>(`/meetings/${meetingId}/transcript`);
      return response.data;
    } catch (error: any) {
      if (error?.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  uploadFile: async (meetingId: string, data: UploadTranscriptRequest) => {
    const formData = new FormData();
    formData.append('file', data.file);
    const response = await apiClient.post<TranscriptDto>(`/meetings/${meetingId}/transcript/upload`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
    return response.data;
  },

  submitManual: async (meetingId: string, data: ManualTranscriptRequest) => {
    const response = await apiClient.post<TranscriptDto>(`/meetings/${meetingId}/transcript/manual`, data);
    return response.data;
  }
};
