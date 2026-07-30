import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SummaryManager } from '../components/SummaryManager';
import { MeetingStatus } from '../../meetings/types/meeting';
import { NotificationProvider } from '../../../contexts/NotificationContext';
import * as analysisApi from '../../analysis/api/analysisApi';

// Mock API
vi.mock('../../analysis/api/analysisApi');

const renderComponent = (meetingStatus: MeetingStatus) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <NotificationProvider>
        <SummaryManager meetingId="test-123" meetingStatus={meetingStatus} />
      </NotificationProvider>
    </QueryClientProvider>
  );
};

describe('SummaryManager', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows info alert when meeting is in draft', () => {
    renderComponent(MeetingStatus.Draft);
    expect(screen.getByText(/Özet ve kararlar, toplantı analiz edildikten sonra/i)).toBeInTheDocument();
  });

  it('shows loading state initially when ready', () => {
    vi.mocked(analysisApi.analysisApi.getSummary).mockReturnValue(new Promise(() => {})); // Never resolves
    renderComponent(MeetingStatus.WaitingForApproval);
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('renders summary data when loaded', async () => {
    vi.mocked(analysisApi.analysisApi.getSummary).mockResolvedValue({
      meetingId: 'test-123',
      summaryId: 'sum-123',
      version: 1,
      executiveSummary: 'Test Özet',
      isApproved: false,
      manualRevisionNumber: 0,
      lowConfidenceItemCount: 0,
      requiresReview: false,
      canEdit: true,
      canApprove: true,
      provider: 'mock',
      promptVersion: '1.0',
      topics: [],
      decisions: [],
      actionItems: [],
      openIssues: [],
      createdAt: new Date().toISOString()
    });

    renderComponent(MeetingStatus.WaitingForApproval);
    
    // Wait for render
    expect(await screen.findByText('Test Özet')).toBeInTheDocument();
    expect(screen.getByText('Yönetici Özeti')).toBeInTheDocument();
  });
});
