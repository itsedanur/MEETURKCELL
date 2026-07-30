export interface MeetingParticipantDto {
  id: string;
  meetingId: string;
  fullName: string;
  email: string;
  company?: string;
  title?: string;
  isRequired: boolean;
  attended: boolean;
  createdAt: string;
}

export interface AddParticipantRequest {
  fullName: string;
  email: string;
  company?: string;
  title?: string;
  isRequired: boolean;
}

export interface UpdateParticipantRequest {
  fullName: string;
  email: string;
  company?: string;
  title?: string;
  isRequired: boolean;
  attended: boolean;
}
