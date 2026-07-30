import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Box, Typography, Button, Paper, CircularProgress, Alert, TextField,
  Accordion, AccordionSummary, AccordionDetails, IconButton,
  Divider, Tooltip
} from '@mui/material';
import { 
  ExpandMore as ExpandMoreIcon, Edit as EditIcon, Save as SaveIcon,
  Warning as WarningIcon, Delete as DeleteIcon
} from '@mui/icons-material';

import { analysisApi } from '../../analysis/api/analysisApi';
import { summariesApi } from '../api/summariesApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { ApiError } from '../../../api/apiError';
import { MeetingStatus } from '../../meetings/types/meeting';

import { TopicManager } from './TopicManager';
import { DecisionManager } from './DecisionManager';
import { OpenIssueManager } from './OpenIssueManager';

interface Props {
  meetingId: string;
  meetingStatus: MeetingStatus;
}

export const SummaryManager: React.FC<Props> = ({ meetingId, meetingStatus }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();

  const [isEditingExecutive, setIsEditingExecutive] = useState(false);
  const [executiveSummary, setExecutiveSummary] = useState('');

  const canEdit = meetingStatus === MeetingStatus.WaitingForApproval || meetingStatus === MeetingStatus.Approved;

  const { data: summary, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings.summary(meetingId),
    queryFn: () => analysisApi.getSummary(meetingId),
    enabled: meetingStatus >= MeetingStatus.WaitingForApproval
  });

  const updateSummaryMutation = useMutation({
    mutationFn: (data: { execSummary: string }) => {
      if (!summary) throw new Error('Summary is not loaded');
      return summariesApi.updateSummary(meetingId, summary.summaryId, {
        expectedVersion: summary.version,
        expectedManualRevisionNumber: summary.manualRevisionNumber,
        executiveSummary: data.execSummary,
        meetingPurpose: summary.meetingPurpose
      });
    },
    onSuccess: () => {
      showNotification('Özet güncellendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      setIsEditingExecutive(false);
    },
    onError: (error: ApiError) => {
      if (error.statusCode === 409) {
        showNotification('Başka bir kullanıcı güncelleme yapmış, lütfen sayfayı yenileyin.', 'error');
      } else {
        showNotification(error.message, 'error');
      }
    }
  });

  const deleteTopicMutation = useMutation({
    mutationFn: (id: string) => summariesApi.deleteTopic(meetingId, summary!.summaryId, id),
    onSuccess: () => { showNotification('Silindi', 'success'); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) }); }
  });

  const deleteDecisionMutation = useMutation({
    mutationFn: (id: string) => summariesApi.deleteDecision(meetingId, summary!.summaryId, id),
    onSuccess: () => { showNotification('Silindi', 'success'); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) }); }
  });

  const deleteOpenIssueMutation = useMutation({
    mutationFn: (id: string) => summariesApi.deleteOpenIssue(meetingId, summary!.summaryId, id),
    onSuccess: () => { showNotification('Silindi', 'success'); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) }); }
  });

  const handleEditExecutive = () => {
    setExecutiveSummary(summary?.executiveSummary || '');
    setIsEditingExecutive(true);
  };

  const handleSaveExecutive = () => {
    updateSummaryMutation.mutate({ execSummary: executiveSummary });
  };

  if (meetingStatus < MeetingStatus.WaitingForApproval) {
    return (
      <Alert severity="info">
        Özet ve kararlar, toplantı analiz edildikten sonra burada görüntülenecektir.
      </Alert>
    );
  }

  if (isLoading) {
    return <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>;
  }

  if (isError || !summary) {
    return <Typography color="error">Özet bilgileri yüklenemedi.</Typography>;
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      {/* Yönetici Özeti */}
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
          <Typography variant="h6">Yönetici Özeti</Typography>
          {canEdit && !isEditingExecutive && (
            <Button startIcon={<EditIcon />} onClick={handleEditExecutive} size="small">
              Düzenle
            </Button>
          )}
        </Box>

        {isEditingExecutive ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              multiline
              rows={5}
              fullWidth
              value={executiveSummary}
              onChange={(e) => setExecutiveSummary(e.target.value)}
            />
            <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1 }}>
              <Button onClick={() => setIsEditingExecutive(false)} disabled={updateSummaryMutation.isPending}>
                İptal
              </Button>
              <Button 
                variant="contained" 
                startIcon={<SaveIcon />} 
                onClick={handleSaveExecutive}
                disabled={updateSummaryMutation.isPending}
              >
                Kaydet
              </Button>
            </Box>
          </Box>
        ) : (
          <Typography variant="body1" sx={{ whiteSpace: 'pre-wrap' }}>
            {summary.executiveSummary || 'Henüz özet oluşturulmamış.'}
          </Typography>
        )}
      </Paper>

      {/* Konular ve Kararlar */}
      <Box>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">Konular ve Kararlar</Typography>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <TopicManager meetingId={meetingId} summaryId={summary.summaryId} topics={summary.topics} canEdit={canEdit} />
            <DecisionManager meetingId={meetingId} summaryId={summary.summaryId} topics={summary.topics} decisions={summary.decisions} canEdit={canEdit} />
          </Box>
        </Box>
        
        {summary.topics.length === 0 ? (
          <Typography color="text.secondary">Konu başlığı bulunamadı.</Typography>
        ) : (
          summary.topics.map((topic) => (
            <Accordion key={topic.id} defaultExpanded>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', width: '100%', pr: 2 }}>
                  <Typography sx={{ fontWeight: 600 }}>{topic.title}</Typography>
                  {canEdit && (
                    <Box>
                      <IconButton size="small" onClick={(e) => { e.stopPropagation(); deleteTopicMutation.mutate(topic.id); }} color="error">
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Box>
                  )}
                </Box>
              </AccordionSummary>
              <AccordionDetails>
                {topic.description && (
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    {topic.description}
                  </Typography>
                )}
                
                <Typography variant="subtitle2" sx={{ mb: 1 }}>Alınan Kararlar:</Typography>
                {summary.decisions.filter(d => d.relatedTopic === topic.title).length > 0 ? (
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                    {summary.decisions.filter(d => d.relatedTopic === topic.title).map(decision => (
                      <Paper key={decision.id} variant="outlined" sx={{ p: 2, bgcolor: decision.requiresReview ? 'warning.light' : 'background.paper' }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                          <Typography variant="body2">{decision.description}</Typography>
                          <Box sx={{ display: 'flex', gap: 1 }}>
                            {decision.requiresReview && (
                              <Tooltip title="Düşük Güven Skoru">
                                <WarningIcon color="warning" fontSize="small" />
                              </Tooltip>
                            )}
                            {canEdit && (
                              <IconButton size="small" onClick={() => deleteDecisionMutation.mutate(decision.id)} color="error" sx={{ p: 0 }}>
                                <DeleteIcon fontSize="small" />
                              </IconButton>
                            )}
                          </Box>
                        </Box>
                      </Paper>
                    ))}
                  </Box>
                ) : (
                  <Typography variant="body2" color="text.secondary">Bu konuya ait karar bulunamadı.</Typography>
                )}
              </AccordionDetails>
            </Accordion>
          ))
        )}
      </Box>

      {/* Açık Konular */}
      <Box>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">Açık Kalan Konular</Typography>
          <OpenIssueManager meetingId={meetingId} summaryId={summary.summaryId} openIssues={summary.openIssues} canEdit={canEdit} />
        </Box>
        <Paper variant="outlined">
          {summary.openIssues.length === 0 ? (
            <Typography color="text.secondary" sx={{ p: 2 }}>Açık konu bulunamadı.</Typography>
          ) : (
            summary.openIssues.map((issue, index) => (
              <Box key={issue.id}>
                {index > 0 && <Divider />}
                <Box sx={{ p: 2, display: 'flex', justifyContent: 'space-between' }}>
                  <Box>
                    <Typography variant="body2">{issue.description}</Typography>
                    {issue.ownerName && (
                      <Typography variant="caption" color="primary" sx={{ mt: 1, display: 'block' }}>
                        Sorumlu: {issue.ownerName}
                      </Typography>
                    )}
                  </Box>
                  {canEdit && (
                    <IconButton size="small" onClick={() => deleteOpenIssueMutation.mutate(issue.id)} color="error">
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  )}
                </Box>
              </Box>
            ))
          )}
        </Paper>
      </Box>
    </Box>
  );
};
