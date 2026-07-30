export enum MeetingStatus {
  Draft = 'Draft',
  ReadyForAnalysis = 'ReadyForAnalysis',
  Analyzing = 'Analyzing',
  WaitingForApproval = 'WaitingForApproval',
  Approved = 'Approved',
  EmailSent = 'EmailSent',
  Archived = 'Archived',
  Failed = 'Failed'
}

export interface MeetingDto {
  id: string;
  title: string;
  meetingDate: string; // ISO 8601 UTC
  startTime: string; // "HH:mm:ss" TimeSpan format
  endTime: string;
  location?: string;
  description?: string;
  status: MeetingStatus;
  statusDisplayName: string;
  organizerUserId: string;
  createdAt: string;
  hasTranscript: boolean;
  canAnalyze: boolean;
  canReanalyze: boolean;
}

export interface MeetingFilterDto {
  pageNumber: number;
  pageSize: number;
  search?: string;
  startDate?: string;
  endDate?: string;
  status?: MeetingStatus;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface CreateMeetingRequest {
  title: string;
  meetingDate: string;
  startTime: string;
  endTime: string;
  location?: string;
  description?: string;
}

export interface UpdateMeetingRequest {
  title: string;
  meetingDate: string;
  startTime: string;
  endTime: string;
  location?: string;
  description?: string;
}
