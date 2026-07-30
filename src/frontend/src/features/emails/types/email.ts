import { MeetingStatus } from '../../meetings/types/meeting';

export enum EmailDeliveryStatus {
  Pending = 1,
  Sent = 2,
  Failed = 3
}

export enum EmailType {
  MeetingSummary = 1,
  ActionItemAssignment = 2,
  Test = 3
}

export interface EmailStatusDto {
  meetingId: string;
  meetingStatus: MeetingStatus;
  meetingStatusDisplayName: string;
  isSummaryApproved: boolean;
  isApprovalStillValid: boolean;
  hasSentEmail: boolean;
  lastEmailStatus?: EmailDeliveryStatus;
  lastEmailStatusDisplayName?: string;
  lastEmailSentAt?: string;
  lastEmailError?: string;
  canPreview: boolean;
  canSend: boolean;
  canSendTestEmail: boolean;
}

export interface EmailPreviewDto {
  subject: string;
  htmlBody?: string;
  textBody?: string;
}

export interface EmailRecipientRequest {
  email: string;
  name?: string;
}

export interface GenerateEmailPreviewRequest {
  subjectOverride?: string;
  introText?: string;
  closingText?: string;
  includeParticipants: boolean;
  includeTopics: boolean;
  includeDecisions: boolean;
  includeActionItems: boolean;
  includeOpenIssues: boolean;
  includeActionStatus: boolean;
  includeEvidence: boolean;
}

export interface SendMeetingEmailRequest extends GenerateEmailPreviewRequest {
  additionalTo?: EmailRecipientRequest[];
  cc?: EmailRecipientRequest[];
  idempotencyKey: string;
}

export interface SendTestEmailRequest {
  toEmail: string;
}

export interface EmailLogDto {
  id: string;
  meetingId: string;
  emailType: EmailType;
  status: EmailDeliveryStatus;
  subject?: string;
  toRecipientsJson?: string;
  ccRecipientsJson?: string;
  requestedAt: string;
  sentAt?: string;
  errorMessage?: string;
}

export interface EmailLogDetailDto extends EmailLogDto {
  bodyHtml?: string;
  bodyText?: string;
}
