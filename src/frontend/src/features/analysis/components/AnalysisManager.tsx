import React from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Box, Typography, Button, Paper, CircularProgress, Alert, Stepper, Step, StepLabel 
} from '@mui/material';
import { PlayArrow as PlayArrowIcon } from '@mui/icons-material';
import dayjs from 'dayjs';

import { analysisApi } from '../api/analysisApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { ApiError } from '../../../api/apiError';
import { MeetingStatus } from '../../meetings/types/meeting';

interface Props {
  meetingId: string;
  meetingStatus: MeetingStatus;
}

const steps = ['Taslak', 'Analize Hazır', 'Analiz Ediliyor', 'Onay Bekliyor', 'Onaylandı'];

export const AnalysisManager: React.FC<Props> = ({ meetingId, meetingStatus }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();

  const { data: status, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings.analysisStatus(meetingId),
    queryFn: () => analysisApi.getStatus(meetingId)
  });

  const analyzeMutation = useMutation({
    mutationFn: () => analysisApi.startAnalysis(meetingId),
    onSuccess: () => {
      showNotification('Analiz başlatıldı.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.analysisStatus(meetingId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
    },
    onError: (error: ApiError) => {
      showNotification(error.message, 'error');
    }
  });

  if (isLoading) {
    return <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>;
  }

  if (isError || !status) {
    return <Typography color="error">Analiz durumu yüklenemedi.</Typography>;
  }

  const getActiveStep = () => {
    switch (status.status) {
      case MeetingStatus.Draft: return 0;
      case MeetingStatus.ReadyForAnalysis: return 1;
      case MeetingStatus.Analyzing: return 2;
      case MeetingStatus.WaitingForApproval: return 3;
      case MeetingStatus.Approved: 
      case MeetingStatus.EmailSent:
      case MeetingStatus.Archived: return 4;
      default: return 0;
    }
  };

  return (
    <Box>
      <Typography variant="h6" sx={{ mb: 3 }}>Analiz Durumu</Typography>

      <Paper sx={{ p: 4, mb: 4 }}>
        <Stepper activeStep={getActiveStep()} alternativeLabel>
          {steps.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>
      </Paper>

      <Paper sx={{ p: 4 }}>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
          <Box>
            <Typography variant="subtitle2" color="text.secondary">Mevcut Durum</Typography>
            <Typography variant="body1" sx={{ fontWeight: 500 }}>
              {status.statusDisplayName}
            </Typography>
          </Box>

          <Box>
            <Typography variant="subtitle2" color="text.secondary">Transcript Durumu</Typography>
            {status.hasTranscript ? (
              <Typography variant="body1" color="success.main">Yüklendi</Typography>
            ) : (
              <Typography variant="body1" color="error.main">Henüz transcript eklenmedi</Typography>
            )}
          </Box>

          {status.analysisStartedAt && (
            <Box>
              <Typography variant="subtitle2" color="text.secondary">Analiz Başlangıcı</Typography>
              <Typography variant="body1">
                {dayjs(status.analysisStartedAt).format('DD.MM.YYYY HH:mm:ss')}
              </Typography>
            </Box>
          )}

          {status.analysisCompletedAt && (
            <Box>
              <Typography variant="subtitle2" color="text.secondary">Analiz Bitişi</Typography>
              <Typography variant="body1">
                {dayjs(status.analysisCompletedAt).format('DD.MM.YYYY HH:mm:ss')}
              </Typography>
            </Box>
          )}

          {status.lastErrorMessage && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {status.lastErrorMessage}
            </Alert>
          )}

          <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
            <Button
              variant="contained"
              size="large"
              startIcon={<PlayArrowIcon />}
              onClick={() => analyzeMutation.mutate()}
              disabled={!status.canAnalyze || analyzeMutation.isPending}
            >
              {analyzeMutation.isPending ? <CircularProgress size={24} /> : 'Analizi Başlat'}
            </Button>

            {status.canReanalyze && (
              <Button
                variant="outlined"
                size="large"
                onClick={() => analyzeMutation.mutate()}
                disabled={analyzeMutation.isPending}
              >
                Yeniden Analiz Et
              </Button>
            )}
          </Box>
        </Box>
      </Paper>
    </Box>
  );
};
