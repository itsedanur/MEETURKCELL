import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, 
  Paper, Typography, CircularProgress, Box, Chip
} from '@mui/material';
import dayjs from 'dayjs';

import { emailsApi } from '../api/emailsApi';
import { queryKeys } from '../../../api/queryKeys';
import { EmailDeliveryStatus, EmailType } from '../types/email';
import { EmailLogDetailDialog } from './EmailLogDetailDialog';

interface Props {
  meetingId: string;
}

export const EmailLogsTable: React.FC<Props> = ({ meetingId }) => {
  const [selectedLogId, setSelectedLogId] = useState<string | null>(null);

  const { data: result, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings.emailLogs(meetingId),
    queryFn: () => emailsApi.getEmailLogs(meetingId)
  });

  const getStatusChipColor = (status: EmailDeliveryStatus) => {
    switch (status) {
      case EmailDeliveryStatus.Sent: return 'success';
      case EmailDeliveryStatus.Failed: return 'error';
      case EmailDeliveryStatus.Pending: return 'warning';
      default: return 'default';
    }
  };

  const getStatusText = (status: EmailDeliveryStatus) => {
    switch (status) {
      case EmailDeliveryStatus.Sent: return 'Başarılı';
      case EmailDeliveryStatus.Failed: return 'Hata';
      case EmailDeliveryStatus.Pending: return 'Bekliyor';
      default: return 'Bilinmiyor';
    }
  };

  const getTypeChipColor = (type: EmailType) => {
    switch (type) {
      case EmailType.MeetingSummary: return 'primary';
      case EmailType.ActionItemAssignment: return 'info';
      case EmailType.Test: return 'secondary';
      default: return 'default';
    }
  };

  const getTypeText = (type: EmailType) => {
    switch (type) {
      case EmailType.MeetingSummary: return 'Toplantı Özeti';
      case EmailType.ActionItemAssignment: return 'Görev Atama';
      case EmailType.Test: return 'Test Mail';
      default: return 'Bilinmiyor';
    }
  };

  if (isLoading) {
    return <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress size={30} /></Box>;
  }

  if (isError) {
    return <Typography color="error">Loglar yüklenemedi.</Typography>;
  }

  if (!result || !result.items || result.items.length === 0) {
    return (
      <Paper variant="outlined" sx={{ p: 3, textAlign: 'center' }}>
        <Typography color="text.secondary">E-posta gönderim logu bulunamadı.</Typography>
      </Paper>
    );
  }

  return (
    <>
      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead sx={{ bgcolor: 'background.default' }}>
            <TableRow>
              <TableCell>Tarih</TableCell>
              <TableCell>Tip</TableCell>
              <TableCell>Konu</TableCell>
              <TableCell>Durum</TableCell>
              <TableCell>Hata Detayı</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {result.items.map((log) => (
              <TableRow 
                key={log.id} 
                hover 
                onClick={() => setSelectedLogId(log.id)}
                sx={{ cursor: 'pointer' }}
              >
                <TableCell>{dayjs(log.requestedAt).format('DD.MM.YYYY HH:mm')}</TableCell>
                <TableCell>
                  <Chip size="small" label={getTypeText(log.emailType)} color={getTypeChipColor(log.emailType)} variant="outlined" />
                </TableCell>
                <TableCell sx={{ maxWidth: 250, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {log.subject || '-'}
                </TableCell>
                <TableCell>
                  <Chip size="small" label={getStatusText(log.status)} color={getStatusChipColor(log.status)} />
                </TableCell>
                <TableCell sx={{ maxWidth: 200, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', color: 'error.main' }}>
                  {log.errorMessage || '-'}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {selectedLogId && (
        <EmailLogDetailDialog 
          meetingId={meetingId}
          logId={selectedLogId}
          open={true}
          onClose={() => setSelectedLogId(null)}
        />
      )}
    </>
  );
};
