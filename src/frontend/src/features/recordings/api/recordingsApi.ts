import apiClient from '../../../api/apiClient';
import { MeetingRecordingDto } from '../types';

export const uploadRecording = async (meetingId: string, file: File, language?: string): Promise<MeetingRecordingDto> => {
  const formData = new FormData();
  formData.append('file', file);
  if (language) {
    formData.append('language', language);
  }

  const response = await apiClient.post<MeetingRecordingDto>(`/api/meetings/${meetingId}/recordings`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  return response.data;
};

export const getRecordings = async (meetingId: string): Promise<MeetingRecordingDto[]> => {
  const response = await apiClient.get<MeetingRecordingDto[]>(`/api/meetings/${meetingId}/recordings`);
  return response.data;
};

export const getRecordingStatus = async (meetingId: string, recordingId: string): Promise<MeetingRecordingDto> => {
  const response = await apiClient.get<MeetingRecordingDto>(`/api/meetings/${meetingId}/recordings/${recordingId}/status`);
  return response.data;
};
