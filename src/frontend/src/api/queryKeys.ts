export const queryKeys = {
  auth: {
    me: () => ['auth', 'me'] as const,
  },
  meetings: {
    all: () => ['meetings'] as const,
    list: (filters: Record<string, any>) => ['meetings', 'list', filters] as const,
    detail: (meetingId: string) => ['meetings', 'detail', meetingId] as const,
    participants: (meetingId: string) => ['meetings', meetingId, 'participants'] as const,
    transcript: (meetingId: string) => ['meetings', meetingId, 'transcript'] as const,
    analysisStatus: (meetingId: string) => ['meetings', meetingId, 'analysisStatus'] as const,
    summary: (meetingId: string) => ['meetings', meetingId, 'summary'] as const,
    summaryVersions: (meetingId: string) => ['meetings', meetingId, 'summaryVersions'] as const,
    emailStatus: (meetingId: string) => ['meetings', meetingId, 'emailStatus'] as const,
    emailLogs: (meetingId: string, filters?: Record<string, any>) => ['meetings', meetingId, 'emailLogs', filters] as const,
    emailLogDetail: (meetingId: string, logId: string) => ['meetings', meetingId, 'emailLogs', logId] as const,
    emailDetail: (meetingId: string, logId: string) => ['meetings', meetingId, 'emailLogs', logId] as const,
  },
  actionItems: {
    all: () => ['actionItems'] as const,
    list: (filters: Record<string, any>) => ['actionItems', 'list', filters] as const,
    myActions: (filters: Record<string, any>) => ['actionItems', 'myActions', filters] as const,
  }
};
