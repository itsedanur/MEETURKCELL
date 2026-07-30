import React, { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Box, Typography, Button, Paper, Alert, Dialog, DialogTitle, 
  DialogContent, DialogActions, TextField, CircularProgress, Chip,
  Checkbox, FormControlLabel, Tooltip
} from '@mui/material';
import { 
  CheckCircle as CheckCircleIcon, 
  Cancel as CancelIcon, 
  Warning as WarningIcon
} from '@mui/icons-material';
import dayjs from 'dayjs';

import { meetingsApi } from '../../meetings/api/meetingsApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { MeetingSummaryDto } from '../types/summary';
import { MeetingStatus } from '../../meetings/types/meeting';

interface Props {
  meetingId: string;
  meetingStatus: MeetingStatus;
  summary: MeetingSummaryDto;
}

export const ApprovalPanel: React.FC<Props> = ({ meetingId, meetingStatus, summary }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();
  
  const [approveOpen, setApproveOpen] = useState(false);
  const [revokeOpen, setRevokeOpen] = useState(false);
  const [revokeReason, setRevokeReason] = useState('');
  const [confirmationChecked, setConfirmationChecked] = useState(false);

  const isApproved = summary.isApproved;
  const canApprove = summary.canApprove && meetingStatus === MeetingStatus.WaitingForApproval;
  const canRevoke = isApproved && meetingStatus !== MeetingStatus.EmailSent && meetingStatus !== MeetingStatus.Archived;

  const approveMutation = useMutation({
    mutationFn: () => meetingsApi.approveSummary(meetingId, {
      expectedVersion: summary.version,
      expectedManualRevisionNumber: summary.manualRevisionNumber,
      confirmation: confirmationChecked
    }),
    onSuccess: () => {
      showNotification('Özet onaylandı.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.analysisStatus(meetingId) });
      setApproveOpen(false);
    },
    onError: (error: any) => {
      if (error.statusCode === 409) {
        showNotification('İçerik siz inceledikten sonra değiştirilmiş. Lütfen sayfayı yenileyip değişiklikleri kontrol edin.', 'error');
      } else {
        showNotification(error.message, 'error');
      }
    }
  });

  const revokeMutation = useMutation({
    mutationFn: () => meetingsApi.revokeApproval(meetingId, { reason: revokeReason }),
    onSuccess: () => {
      showNotification('Onay geri alındı.', 'warning');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.analysisStatus(meetingId) });
      setRevokeOpen(false);
      setRevokeReason('');
    },
    onError: (error: any) => {
      showNotification(error.message, 'error');
    }
  });

  if (meetingStatus < MeetingStatus.WaitingForApproval) {
    return null;
  }

  return (
    <Box sx={{ mb: 4 }}>
      <Paper 
        variant="outlined" 
        sx={{ 
          p: 3, 
          bgcolor: isApproved ? 'success.50' : (canApprove ? 'info.50' : 'background.paper'),
          borderColor: isApproved ? 'success.main' : (canApprove ? 'info.main' : 'divider')
        }}
      >
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Box>
            <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              Onay Durumu: 
              {isApproved ? (
                <Chip icon={<CheckCircleIcon />} label="Onaylandı" color="success" size="small" />
              ) : (
                <Chip label="Onay Bekliyor" color="warning" size="small" />
              )}
            </Typography>
            
            <Box sx={{ mt: 2, display: 'flex', gap: 3, flexWrap: 'wrap' }}>
              <Typography variant="body2">
                <strong>Versiyon:</strong> v{summary.version}.{summary.manualRevisionNumber}
              </Typography>
              {isApproved && summary.approvedBy && (
                <>
                  <Typography variant="body2">
                    <strong>Onaylayan:</strong> {summary.approvedBy}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Onay Zamanı:</strong> {dayjs(summary.approvedAt).format('DD.MM.YYYY HH:mm')}
                  </Typography>
                </>
              )}
            </Box>

            {summary.lowConfidenceItemCount > 0 && !isApproved && (
              <Alert severity="warning" sx={{ mt: 2 }} icon={<WarningIcon />}>
                Dikkat: Analiz sonucunda yapay zekanın emin olmadığı {summary.lowConfidenceItemCount} içerik bulunmaktadır. Lütfen bu içerikleri (sarı uyarı ikonlu) kontrol ediniz.
              </Alert>
            )}

            {!isApproved && !canApprove && (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
                Yalnızca toplantı sahibi veya admin onay verebilir.
              </Typography>
            )}
          </Box>
          
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1, minWidth: 150 }}>
            {!isApproved && canApprove && (
              <Button 
                variant="contained" 
                color="primary" 
                startIcon={<CheckCircleIcon />}
                onClick={() => { setConfirmationChecked(false); setApproveOpen(true); }}
              >
                Onayla
              </Button>
            )}
            
            {isApproved && canRevoke && (
              <Button 
                variant="outlined" 
                color="error" 
                startIcon={<CancelIcon />}
                onClick={() => { setRevokeReason(''); setRevokeOpen(true); }}
              >
                Onayı Geri Al
              </Button>
            )}
            
            {isApproved && meetingStatus === MeetingStatus.EmailSent && (
              <Tooltip title="Toplantı özeti e-posta ile gönderildiği için onay geri alınamaz.">
                <span>
                  <Button variant="outlined" color="error" disabled fullWidth>
                    Onayı Geri Al
                  </Button>
                </span>
              </Tooltip>
            )}
          </Box>
        </Box>
      </Paper>

      {/* Onay Dialog */}
      <Dialog open={approveOpen} onClose={() => setApproveOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Özeti Onayla</DialogTitle>
        <DialogContent dividers>
          <Typography variant="body1" sx={{ mb: 2 }}>
            Bu toplantı özetini ve üretilen aksiyonları onaylamak üzeresiniz. Onaylandıktan sonra içerik değişikliği yapıldığında onay otomatik olarak kalkacaktır.
          </Typography>
          {summary.lowConfidenceItemCount > 0 && (
            <Alert severity="warning" sx={{ mb: 2 }}>
              Düşük güven skoruna sahip {summary.lowConfidenceItemCount} adet içerik bulunuyor. Bunları kontrol ettiğinizden emin olun.
            </Alert>
          )}
          <FormControlLabel
            control={<Checkbox checked={confirmationChecked} onChange={(e) => setConfirmationChecked(e.target.checked)} />}
            label="Tüm kararları ve aksiyonları kontrol ettim, toplantı özetini onaylıyorum."
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setApproveOpen(false)} disabled={approveMutation.isPending}>İptal</Button>
          <Button 
            variant="contained" 
            onClick={() => approveMutation.mutate()} 
            disabled={!confirmationChecked || approveMutation.isPending}
            startIcon={approveMutation.isPending ? <CircularProgress size={20} /> : undefined}
          >
            Onayla
          </Button>
        </DialogActions>
      </Dialog>

      {/* Onay İptal Dialog */}
      <Dialog open={revokeOpen} onClose={() => setRevokeOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Onayı Geri Al</DialogTitle>
        <DialogContent dividers>
          <Typography variant="body1" sx={{ mb: 2 }}>
            Özet onayını geri almak üzeresiniz. Bu işlem durumu "Onay Bekliyor" olarak güncelleyecektir.
          </Typography>
          <TextField
            fullWidth
            required
            label="Geri Alma Nedeni"
            multiline
            rows={3}
            value={revokeReason}
            onChange={(e) => setRevokeReason(e.target.value)}
            error={revokeReason.trim() === ''}
            helperText={revokeReason.trim() === '' ? 'İptal nedeni belirtmelisiniz' : ''}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRevokeOpen(false)} disabled={revokeMutation.isPending}>İptal</Button>
          <Button 
            variant="contained" 
            color="error"
            onClick={() => revokeMutation.mutate()} 
            disabled={revokeReason.trim() === '' || revokeMutation.isPending}
            startIcon={revokeMutation.isPending ? <CircularProgress size={20} /> : undefined}
          >
            Geri Al
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
