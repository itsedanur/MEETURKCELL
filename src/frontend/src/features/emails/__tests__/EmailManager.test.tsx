import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';

import { RecipientManager } from '../components/RecipientManager';
import { EmailManager } from '../components/EmailManager';
import { emailsApi } from '../api/emailsApi';
import { EmailDeliveryStatus, EmailType } from '../types/email';
import { MeetingStatus } from '../../meetings/types/meeting';
import { NotificationProvider } from '../../../contexts/NotificationContext';

vi.mock('../api/emailsApi');
vi.mock('../../participants/api/participantsApi', () => ({
  participantsApi: {
    getParticipants: vi.fn().mockResolvedValue([])
  }
}));

const createQueryClient = () => new QueryClient({
  defaultOptions: { queries: { retry: false } }
});

const defaultEmailStatus = {
  meetingId: '123',
  meetingStatus: MeetingStatus.Approved,
  meetingStatusDisplayName: 'Approved',
  isSummaryApproved: true,
  isApprovalStillValid: true,
  hasSentEmail: false,
  canPreview: true,
  canSend: true,
  canSendTestEmail: true,
};

const renderWithProviders = (ui: React.ReactElement) => {
  const queryClient = createQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <NotificationProvider>
        {ui}
      </NotificationProvider>
    </QueryClientProvider>
  );
};

describe('Email UI Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('EmailManager', () => {
    it('should disable send button when canSend is false', async () => {
      vi.mocked(emailsApi.getEmailStatus).mockResolvedValueOnce({
        ...defaultEmailStatus,
        canSend: false,
      });
      vi.mocked(emailsApi.getEmailLogs).mockResolvedValueOnce({ items: [], totalCount: 0, pageNumber: 1, pageSize: 10, totalPages: 0, hasNextPage: false, hasPreviousPage: false });

      renderWithProviders(<EmailManager meetingId="123" />);

      await waitFor(() => {
        expect(screen.getByText('E-posta Gönderim Durumu')).toBeInTheDocument();
      });

      const sendButton = screen.getByRole('button', { name: /^Gönder$/i });
      expect(sendButton).toBeDisabled();
    });

    it('should disable test mail button when canSendTestEmail is false', async () => {
      vi.mocked(emailsApi.getEmailStatus).mockResolvedValueOnce({
        ...defaultEmailStatus,
        canSendTestEmail: false,
      });
      vi.mocked(emailsApi.getEmailLogs).mockResolvedValueOnce({ items: [], totalCount: 0, pageNumber: 1, pageSize: 10, totalPages: 0, hasNextPage: false, hasPreviousPage: false });

      renderWithProviders(<EmailManager meetingId="123" />);

      await waitFor(() => {
        expect(screen.getByText('E-posta Gönderim Durumu')).toBeInTheDocument();
      });

      const testButton = screen.getByRole('button', { name: /Test Mail Gönder/i });
      expect(testButton).toBeDisabled();
    });

    it('should render email logs and open detail dialog', async () => {
      vi.mocked(emailsApi.getEmailStatus).mockResolvedValueOnce(defaultEmailStatus);
      vi.mocked(emailsApi.getEmailLogs).mockResolvedValueOnce({
        items: [
          {
            id: 'log1',
            meetingId: '123',
            emailType: EmailType.MeetingSummary,
            status: EmailDeliveryStatus.Sent,
            subject: 'Test Konu',
            requestedAt: '2023-10-10T10:00:00Z',
          }
        ],
        totalCount: 1,
        pageNumber: 1,
        totalPages: 1,
        pageSize: 10,
        hasNextPage: false,
        hasPreviousPage: false
      });
      vi.mocked(emailsApi.getEmailLogDetail).mockResolvedValueOnce({
        id: 'log1',
        meetingId: '123',
        emailType: EmailType.MeetingSummary,
        status: EmailDeliveryStatus.Sent,
        subject: 'Test Konu',
        requestedAt: '2023-10-10T10:00:00Z',
        bodyHtml: '<h1>Hello</h1>',
        bodyText: 'Hello'
      });

      renderWithProviders(<EmailManager meetingId="123" />);

      await waitFor(() => {
        expect(screen.getByText('Test Konu')).toBeInTheDocument();
      });

      // Click row
      fireEvent.click(screen.getByText('Test Konu'));

      await waitFor(() => {
        expect(screen.getByText('E-posta Log Detayı')).toBeInTheDocument();
        expect(screen.getByText('HTML İçerik')).toBeInTheDocument();
      });
    });
  });

  describe('RecipientManager', () => {
    it('should prevent adding duplicate email', async () => {
      renderWithProviders(<RecipientManager meetingId="123" emailStatus={defaultEmailStatus} />);

      const emailInput = screen.getByPlaceholderText('E-posta adresi ekle...');
      const addButton = screen.getAllByRole('button', { name: 'Ekle' })[0]; // TO Ekle butonu

      fireEvent.change(emailInput, { target: { value: 'test@test.com' } });
      fireEvent.click(addButton);

      await waitFor(() => {
        expect(screen.getByText('test@test.com')).toBeInTheDocument();
      });

      // Try duplicate
      fireEvent.change(emailInput, { target: { value: 'test@test.com' } });
      fireEvent.click(addButton);

      const chips = screen.getAllByText('test@test.com');
      expect(chips.length).toBe(1);
    });

    it('should require confirmation to send email', async () => {
      vi.mocked(emailsApi.sendMeetingEmail).mockResolvedValueOnce({});
      
      renderWithProviders(<RecipientManager meetingId="123" emailStatus={defaultEmailStatus} />);

      const sendButton = screen.getByRole('button', { name: /^Gönder$/i });
      fireEvent.click(sendButton);

      await waitFor(() => {
        expect(screen.getByText('E-posta Gönderim Onayı')).toBeInTheDocument();
      });

      const confirmButton = screen.getByRole('button', { name: /Tümüne Gönder/i });
      expect(confirmButton).toBeDisabled();

      const checkbox = screen.getByLabelText('Bu toplantı özetini göndermeyi onaylıyorum.');
      fireEvent.click(checkbox);

      expect(confirmButton).not.toBeDisabled();

      fireEvent.click(confirmButton);

      await waitFor(() => {
        expect(emailsApi.sendMeetingEmail).toHaveBeenCalledTimes(1);
      });
    });

    it('should render email preview with iframe', async () => {
      vi.mocked(emailsApi.generatePreview).mockResolvedValueOnce({
        subject: 'Preview Subject',
        htmlBody: '<p>Test</p>',
        textBody: 'Test'
      });

      renderWithProviders(<RecipientManager meetingId="123" emailStatus={defaultEmailStatus} />);

      const previewButton = screen.getByRole('button', { name: /Ön İzleme/i });
      fireEvent.click(previewButton);

      await waitFor(() => {
        expect(screen.getByText('E-posta Ön İzleme')).toBeInTheDocument();
      });

      const iframe = document.querySelector('iframe');
      expect(iframe).toBeInTheDocument();
      expect(iframe?.getAttribute('srcDoc')).toBe('<p>Test</p>');
      expect(iframe?.getAttribute('sandbox')).toBe('allow-same-origin');
    });
  });
});
