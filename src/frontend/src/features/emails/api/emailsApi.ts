import apiClient from '../../../api/apiClient';
import { PagedResult } from '../../../api/apiResponse';
import { 
  EmailStatusDto, 
  GenerateEmailPreviewRequest, 
  EmailPreviewDto, 
  SendMeetingEmailRequest, 
  SendTestEmailRequest, 
  EmailLogDto, 
  EmailLogDetailDto 
} from '../types/email';

export const emailsApi = {
  getEmailStatus: async (meetingId: string) => {
    const response = await apiClient.get<EmailStatusDto>(`/meetings/${meetingId}/email/status`);
    return response.data;
  },

  generatePreview: async (meetingId: string, data: GenerateEmailPreviewRequest) => {
    const response = await apiClient.post<EmailPreviewDto>(`/meetings/${meetingId}/email/preview`, data);
    return response.data;
  },

  sendMeetingEmail: async (meetingId: string, data: SendMeetingEmailRequest) => {
    const response = await apiClient.post(`/meetings/${meetingId}/email/send`, data);
    return response.data;
  },

  sendTestEmail: async (meetingId: string, data: SendTestEmailRequest) => {
    const response = await apiClient.post(`/meetings/${meetingId}/email/test`, data);
    return response.data;
  },

  getEmailLogs: async (meetingId: string) => {
    const response = await apiClient.get<PagedResult<EmailLogDto>>(`/meetings/${meetingId}/email/logs`);
    return response.data;
  },

  getEmailLogDetail: async (meetingId: string, emailLogId: string) => {
    const response = await apiClient.get<EmailLogDetailDto>(`/meetings/${meetingId}/email/logs/${emailLogId}`);
    return response.data;
  }
};
