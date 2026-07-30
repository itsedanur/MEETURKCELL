import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Box, Typography, Paper, CircularProgress, Chip, Alert, Grid, Tooltip } from '@mui/material';
import dayjs from 'dayjs';
import { CheckCircle as CheckCircleIcon, Cancel as CancelIcon, Email as EmailIcon } from '@mui/icons-material';

import { emailsApi } from '../api/emailsApi';
import { queryKeys } from '../../../api/queryKeys';
import { EmailDeliveryStatus } from '../types/email';
import { RecipientManager } from './RecipientManager';
import { EmailLogsTable } from './EmailLogsTable';

interface Props {
  meetingId: string;
}

export const EmailManager: React.FC<Props> = ({ meetingId }) => {
  const { data: status, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings.emailStatus(meetingId),
    queryFn: () => emailsApi.getEmailStatus(meetingId)
  });

  if (isLoading) {
    return <Box sx={{ display: 'flex', justifyContent: 'center', p: 5 }}><CircularProgress /></Box>;
  }

  if (isError || !status) {
    return <Alert severity="error">E-posta durumu yüklenemedi.</Alert>;
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      <Paper variant="outlined" sx={{ p: 3, bgcolor: status.hasSentEmail ? 'success.50' : 'background.paper', borderColor: status.hasSentEmail ? 'success.main' : 'divider' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
          <Box>
            <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <EmailIcon /> E-posta Gönderim Durumu
            </Typography>
            
            <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>Özet Onayı:</Typography>
                {status.isSummaryApproved ? (
                  <Chip label="Onaylı" color="success" size="small" icon={<CheckCircleIcon />} />
                ) : (
                  <Chip label="Onaylanmadı" color="warning" size="small" />
                )}
                
                {status.isSummaryApproved && !status.isApprovalStillValid && (
                  <Tooltip title="İçerik değiştiği için onay geçersiz">
                    <Chip label="Geçersiz" color="error" size="small" icon={<CancelIcon />} />
                  </Tooltip>
                )}
              </Box>

              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>Son Gönderim:</Typography>
                {status.hasSentEmail ? (
                  <>
                    <Chip 
                      label={status.lastEmailStatusDisplayName || 'Bilinmiyor'} 
                      color={status.lastEmailStatus === EmailDeliveryStatus.Sent ? 'success' : (status.lastEmailStatus === EmailDeliveryStatus.Failed ? 'error' : 'default')} 
                      size="small" 
                    />
                    <Typography variant="body2" color="text.secondary">
                      ({status.lastEmailSentAt ? dayjs(status.lastEmailSentAt).format('DD.MM.YYYY HH:mm') : '-'})
                    </Typography>
                  </>
                ) : (
                  <Typography variant="body2" color="text.secondary">Henüz gönderilmedi</Typography>
                )}
              </Box>
            </Box>
          </Box>
        </Box>
        
        {(!status.isSummaryApproved || !status.isApprovalStillValid) && !status.hasSentEmail && (
          <Alert severity="warning" sx={{ mt: 2 }}>
            Toplantı özeti gönderilebilmesi için öncelikle yöneticinin özeti onaylaması gerekmektedir.
          </Alert>
        )}
      </Paper>

      <RecipientManager meetingId={meetingId} emailStatus={status} />

      <Box>
        <Typography variant="h6" sx={{ mb: 2 }}>E-posta Logları</Typography>
        <EmailLogsTable meetingId={meetingId} />
      </Box>
    </Box>
  );
};
