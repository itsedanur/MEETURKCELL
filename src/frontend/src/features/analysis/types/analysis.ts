import { MeetingStatus } from '../../meetings/types/meeting';

export interface MeetingAnalysisStatusDto {
  meetingId: string;
  status: MeetingStatus;
  statusDisplayName: string;
  hasTranscript: boolean;
  hasAnalysis: boolean;
  currentSummaryId?: string;
  currentVersion: number;
  analysisStartedAt?: string;
  analysisCompletedAt?: string;
  lastErrorMessage?: string;
  canAnalyze: boolean;
  canReanalyze: boolean;
}

export interface AnalysisParticipantDto {
  fullName: string;
  email?: string;
  department?: string;
  title?: string;
}

export interface MeetingAnalysisInput {
  meetingId: string;
  meetingTitle: string;
  meetingDate: string;
  transcriptText: string;
  participants: AnalysisParticipantDto[];
  language: string;
  promptVersion: string;
}
