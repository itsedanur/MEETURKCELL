export enum RecordingStatus {
  Uploaded = 1,
  Validating = 2,
  Queued = 3,
  Processing = 4,
  Completed = 5,
  Failed = 6,
  Cancelled = 7
}

export interface MeetingRecordingDto {
  id: string;
  meetingId: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  status: RecordingStatus;
  errorCode?: string;
  safeErrorMessage?: string;
  createdAt: string;
  processingStartedAt?: string;
  processingCompletedAt?: string;
}
