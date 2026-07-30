import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  Dialog, DialogTitle, DialogContent, DialogActions, Button, 
  Box, Typography, CircularProgress, Chip, Tabs, Tab, Paper
} from '@mui/material';
import dayjs from 'dayjs';

import { emailsApi } from '../api/emailsApi';
import { queryKeys } from '../../../api/queryKeys';
import { EmailDeliveryStatus } from '../types/email';

interface Props {
  meetingId: string;
  logId: string;
  open: boolean;
  onClose: () => void;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function CustomTabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      {...other}
      style={{ flex: 1, display: 'flex', flexDirection: 'column' }}
    >
      {value === index && (
        <Box sx={{ p: 2, flex: 1, display: 'flex', flexDirection: 'column' }}>
          {children}
        </Box>
      )}
    </div>
  );
}

export const EmailLogDetailDialog: React.FC<Props> = ({ meetingId, logId, open, onClose }) => {
  const [tabValue, setTabValue] = useState(0);

  const { data: detail, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings.emailLogDetail(meetingId, logId),
    queryFn: () => emailsApi.getEmailLogDetail(meetingId, logId),
    enabled: open && !!logId
  });

  const getStatusChipColor = (status: EmailDeliveryStatus) => {
    switch (status) {
      case EmailDeliveryStatus.Sent: return 'success';
      case EmailDeliveryStatus.Failed: return 'error';
      case EmailDeliveryStatus.Pending: return 'warning';
      default: return 'default';
    }
  };

  const parseRecipients = (json?: string) => {
    if (!json) return '-';
    try {
      const arr = JSON.parse(json);
      return Array.isArray(arr) ? arr.map((r: any) => r.email).join(', ') : json;
    } catch {
      return json;
    }
  };

  if (!open) return null;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth sx={{ '& .MuiDialog-paper': { height: '85vh' } }}>
      <DialogTitle>E-posta Log Detayı</DialogTitle>
      
      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 5, flex: 1 }}><CircularProgress /></Box>
      ) : isError || !detail ? (
        <DialogContent dividers>
          <Typography color="error">Detaylar yüklenemedi.</Typography>
        </DialogContent>
      ) : (
        <>
          <Box sx={{ px: 3, py: 2, bgcolor: 'background.default', borderBottom: 1, borderColor: 'divider' }}>
            <Box sx={{ display: 'flex', gap: 2, mb: 1 }}>
              <Chip size="small" label={EmailDeliveryStatus[detail.status]} color={getStatusChipColor(detail.status)} />
              <Typography variant="body2" color="text.secondary">
                Talep: {dayjs(detail.requestedAt).format('DD.MM.YYYY HH:mm:ss')} 
                {detail.sentAt && ` | Gönderim: ${dayjs(detail.sentAt).format('DD.MM.YYYY HH:mm:ss')}`}
              </Typography>
            </Box>
            
            <Typography variant="subtitle2" sx={{ mt: 2 }}>Konu (Subject)</Typography>
            <Typography variant="body1">{detail.subject || '-'}</Typography>

            <Typography variant="subtitle2" sx={{ mt: 1 }}>Kime (To)</Typography>
            <Typography variant="body2">{parseRecipients(detail.toRecipientsJson)}</Typography>
            
            {detail.ccRecipientsJson && detail.ccRecipientsJson !== '[]' && (
              <>
                <Typography variant="subtitle2" sx={{ mt: 1 }}>Bilgi (CC)</Typography>
                <Typography variant="body2">{parseRecipients(detail.ccRecipientsJson)}</Typography>
              </>
            )}

            {detail.errorMessage && (
              <Box sx={{ mt: 2, p: 1, bgcolor: 'error.main', color: 'error.contrastText', borderRadius: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 'bold' }}>Hata Mesajı:</Typography>
                <Typography variant="body2">{detail.errorMessage}</Typography>
              </Box>
            )}
          </Box>

          {(detail.bodyHtml || detail.bodyText) && (
            <Box sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
              <Tabs value={tabValue} onChange={(e, val) => setTabValue(val)}>
                <Tab label="HTML İçerik" />
                <Tab label="Metin İçerik" />
              </Tabs>
            </Box>
          )}

          <DialogContent dividers sx={{ p: 0, display: 'flex', flexDirection: 'column' }}>
            {detail.bodyHtml || detail.bodyText ? (
              <>
                <CustomTabPanel value={tabValue} index={0}>
                  {detail.bodyHtml ? (
                    <Paper variant="outlined" sx={{ flex: 1, overflow: 'hidden' }}>
                      <iframe
                        title="HTML Body"
                        srcDoc={detail.bodyHtml}
                        sandbox="allow-same-origin"
                        style={{ width: '100%', height: '100%', border: 'none' }}
                      />
                    </Paper>
                  ) : (
                    <Typography color="text.secondary">HTML içerik bulunamadı.</Typography>
                  )}
                </CustomTabPanel>
                <CustomTabPanel value={tabValue} index={1}>
                  {detail.bodyText ? (
                    <Paper variant="outlined" sx={{ flex: 1, overflow: 'auto', p: 2, bgcolor: 'background.default' }}>
                      <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', fontFamily: 'monospace' }}>
                        {detail.bodyText}
                      </Typography>
                    </Paper>
                  ) : (
                    <Typography color="text.secondary">Metin içerik bulunamadı.</Typography>
                  )}
                </CustomTabPanel>
              </>
            ) : (
              <Box sx={{ p: 3, textAlign: 'center' }}>
                <Typography color="text.secondary">E-posta içeriği (Body) kaydedilmemiş.</Typography>
              </Box>
            )}
          </DialogContent>
        </>
      )}

      <DialogActions>
        <Button onClick={onClose} variant="contained">Kapat</Button>
      </DialogActions>
    </Dialog>
  );
};
