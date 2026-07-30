export interface TranscriptDto {
  id: string;
  meetingId: string;
  originalText: string;
  cleanedText?: string;
  uploadDate: string;
}

export interface UploadTranscriptRequest {
  file: File;
}

export interface ManualTranscriptRequest {
  text: string;
}
