import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ActionManager } from '../components/ActionManager';
import { MeetingStatus } from '../../meetings/types/meeting';
import { NotificationProvider } from '../../../contexts/NotificationContext';
import * as analysisApi from '../../analysis/api/analysisApi';
import * as participantsApi from '../../participants/api/participantsApi';
import { ActionItemStatus, ActionPriority, SourceType } from '../../summaries/types/summary';

vi.mock('../../analysis/api/analysisApi');
vi.mock('../../participants/api/participantsApi');

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
        <ActionManager meetingId="test-123" meetingStatus={meetingStatus} />
      </NotificationProvider>
    </QueryClientProvider>
  );
};

describe('ActionManager', () => {
  it('renders actions correctly', async () => {
    vi.mocked(participantsApi.participantsApi.getParticipants).mockResolvedValue([]);
    vi.mocked(analysisApi.analysisApi.getSummary).mockResolvedValue({
      meetingId: 'test-123',
      summaryId: 'sum-123',
      version: 1,
      executiveSummary: '',
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
      actionItems: [
        {
          id: 'action-1',
          description: 'Test Aksiyon',
          priority: ActionPriority.High,
          priorityDisplayName: 'High',
          status: ActionItemStatus.Open,
          statusDisplayName: 'Open',
          confidenceScore: 1.0,
          requiresReview: false,
          createdAt: new Date().toISOString(),
          sourceType: SourceType.Manual,
          sourceTypeDisplayName: 'Manual',
          isOverdue: false,
          ownerName: 'Ali Yılmaz'
        }
      ],
      openIssues: [],
      createdAt: new Date().toISOString()
    });

    renderComponent(MeetingStatus.WaitingForApproval);
    
    expect(await screen.findByText('Test Aksiyon')).toBeInTheDocument();
    expect(screen.getByText('Ali Yılmaz')).toBeInTheDocument();
    expect(screen.getByText('High')).toBeInTheDocument();
  });
});
