import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Box, Typography, Paper, Grid, TextField, Button, Checkbox, FormControlLabel,
  Chip, Dialog, DialogTitle, DialogContent, DialogActions, Alert, Tooltip, CircularProgress,
  IconButton
} from '@mui/material';
import { Add as AddIcon, Close as CloseIcon, Send as SendIcon, Preview as PreviewIcon, BugReport as BugReportIcon } from '@mui/icons-material';
import { z } from 'zod';

import { participantsApi } from '../../participants/api/participantsApi';
import { emailsApi } from '../api/emailsApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { EmailStatusDto, EmailRecipientRequest } from '../types/email';
import { EmailPreviewDialog } from './EmailPreviewDialog';

interface Props {
  meetingId: string;
  emailStatus: EmailStatusDto;
}

const emailSchema = z.string().email('Geçerli bir e-posta adresi giriniz');

export const RecipientManager: React.FC<Props> = ({ meetingId, emailStatus }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();

  const [toRecipients, setToRecipients] = useState<EmailRecipientRequest[]>([]);
  const [ccRecipients, setCcRecipients] = useState<EmailRecipientRequest[]>([]);
  
  const [newTo, setNewTo] = useState('');
  const [newCc, setNewCc] = useState('');

  const [subjectOverride, setSubjectOverride] = useState('');
  const [introText, setIntroText] = useState('');
  const [closingText, setClosingText] = useState('');

  const [includes, setIncludes] = useState({
    includeParticipants: true,
    includeTopics: true,
    includeDecisions: true,
    includeActionItems: true,
    includeOpenIssues: true,
    includeActionStatus: true,
    includeEvidence: false
  });

  const [previewOpen, setPreviewOpen] = useState(false);
  const [sendConfirmOpen, setSendConfirmOpen] = useState(false);
  const [testEmailOpen, setTestEmailOpen] = useState(false);
  const [testEmailAddress, setTestEmailAddress] = useState('');
  const [sendConfirmationChecked, setSendConfirmationChecked] = useState(false);

  const { data: participants } = useQuery({
    queryKey: queryKeys.meetings.participants(meetingId),
    queryFn: () => participantsApi.getParticipants(meetingId),
  });

  // Default populating TO list from participants on initial load
  useEffect(() => {
    if (participants && toRecipients.length === 0 && !emailStatus.hasSentEmail) {
      const validParticipants = participants
        .filter(p => emailSchema.safeParse(p.email).success)
        .map(p => ({ email: p.email, name: p.fullName }));
      
      if (validParticipants.length > 0) {
        setToRecipients(validParticipants);
      }
    }
  }, [participants]);

  const handleAddRecipient = (type: 'TO' | 'CC') => {
    const email = type === 'TO' ? newTo.trim() : newCc.trim();
    if (!email) return;

    const validation = emailSchema.safeParse(email);
    if (!validation.success) {
      showNotification(validation.error.issues[0].message, 'error');
      return;
    }

    const existsInTo = toRecipients.some(r => r.email.toLowerCase() === email.toLowerCase());
    const existsInCc = ccRecipients.some(r => r.email.toLowerCase() === email.toLowerCase());

    if (existsInTo || existsInCc) {
      showNotification('Bu e-posta adresi zaten ekli.', 'error');
      return;
    }

    if (type === 'TO') {
      setToRecipients([...toRecipients, { email, name: email.split('@')[0] }]);
      setNewTo('');
    } else {
      setCcRecipients([...ccRecipients, { email, name: email.split('@')[0] }]);
      setNewCc('');
    }
  };

  const handleRemoveRecipient = (type: 'TO' | 'CC', email: string) => {
    if (type === 'TO') {
      setToRecipients(toRecipients.filter(r => r.email !== email));
    } else {
      setCcRecipients(ccRecipients.filter(r => r.email !== email));
    }
  };

  const handleIncludeChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setIncludes({ ...includes, [event.target.name]: event.target.checked });
  };

  const generatePreviewMutation = useMutation({
    mutationFn: () => emailsApi.generatePreview(meetingId, {
      subjectOverride: subjectOverride || undefined,
      introText: introText || undefined,
      closingText: closingText || undefined,
      ...includes
    })
  });

  const handlePreview = () => {
    generatePreviewMutation.mutate(undefined, {
      onSuccess: () => setPreviewOpen(true),
      onError: (error: any) => showNotification(error.message, 'error')
    });
  };

  const sendEmailMutation = useMutation({
    mutationFn: (idempotencyKey: string) => emailsApi.sendMeetingEmail(meetingId, {
      additionalTo: toRecipients, // In backend logic, maybe this merges. Our DTO handles it.
      cc: ccRecipients,
      subjectOverride: subjectOverride || undefined,
      introText: introText || undefined,
      closingText: closingText || undefined,
      idempotencyKey,
      ...includes
    }),
    onSuccess: () => {
      showNotification('E-posta başarıyla gönderildi.', 'success');
      setSendConfirmOpen(false);
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.emailStatus(meetingId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.emailLogs(meetingId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
    },
    onError: (error: any) => showNotification(error.message, 'error')
  });

  const handleSend = () => {
    if (!sendConfirmationChecked) return;
    const idempotencyKey = crypto.randomUUID();
    sendEmailMutation.mutate(idempotencyKey);
  };

  const testEmailMutation = useMutation({
    mutationFn: () => emailsApi.sendTestEmail(meetingId, { toEmail: testEmailAddress }),
    onSuccess: () => {
      showNotification('Test e-postası gönderildi.', 'success');
      setTestEmailOpen(false);
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.emailLogs(meetingId) });
      // status does not change for test emails
    },
    onError: (error: any) => showNotification(error.message, 'error')
  });

  const getButtonTooltip = (condition: boolean, reason: string) => condition ? '' : reason;
  const isSendDisabled = !emailStatus.canSend || emailStatus.hasSentEmail;
  const sendDisabledReason = emailStatus.hasSentEmail ? 'E-posta zaten gönderildi.' : (!emailStatus.isSummaryApproved ? 'Toplantı özeti onaylanmamış.' : 'Onay geçerli değil (içerik değişmiş).');

  return (
    <Paper variant="outlined" sx={{ p: 3 }}>
      <Typography variant="h6" sx={{ mb: 3 }}>Gönderim Detayları ve Alıcılar</Typography>
      
      <Grid container spacing={4}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Box sx={{ mb: 3 }}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>Kime (To) ({toRecipients.length})</Typography>
            <Box sx={{ display: 'flex', gap: 1, mb: 1 }}>
              <TextField 
                size="small" 
                placeholder="E-posta adresi ekle..." 
                fullWidth 
                value={newTo}
                onChange={(e) => setNewTo(e.target.value)}
                onKeyPress={(e) => e.key === 'Enter' && handleAddRecipient('TO')}
                disabled={emailStatus.hasSentEmail}
              />
              <Button variant="outlined" onClick={() => handleAddRecipient('TO')} disabled={emailStatus.hasSentEmail}>Ekle</Button>
            </Box>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
              {toRecipients.map((r, idx) => (
                <Chip 
                  key={idx} 
                  label={r.email} 
                  onDelete={emailStatus.hasSentEmail ? undefined : () => handleRemoveRecipient('TO', r.email)}
                />
              ))}
            </Box>
          </Box>

          <Box sx={{ mb: 3 }}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>Bilgi (CC) ({ccRecipients.length})</Typography>
            <Box sx={{ display: 'flex', gap: 1, mb: 1 }}>
              <TextField 
                size="small" 
                placeholder="CC e-posta adresi ekle..." 
                fullWidth 
                value={newCc}
                onChange={(e) => setNewCc(e.target.value)}
                onKeyPress={(e) => e.key === 'Enter' && handleAddRecipient('CC')}
                disabled={emailStatus.hasSentEmail}
              />
              <Button variant="outlined" onClick={() => handleAddRecipient('CC')} disabled={emailStatus.hasSentEmail}>Ekle</Button>
            </Box>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
              {ccRecipients.map((r, idx) => (
                <Chip 
                  key={idx} 
                  label={r.email} 
                  onDelete={emailStatus.hasSentEmail ? undefined : () => handleRemoveRecipient('CC', r.email)}
                />
              ))}
            </Box>
          </Box>
          
          <Box sx={{ mb: 3 }}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>Özel İçerikler</Typography>
            <TextField label="Konu (Subject) Özelleştir (Opsiyonel)" fullWidth size="small" sx={{ mb: 2 }} value={subjectOverride} onChange={e => setSubjectOverride(e.target.value)} disabled={emailStatus.hasSentEmail} />
            <TextField label="Giriş Metni (Opsiyonel)" fullWidth size="small" multiline rows={2} sx={{ mb: 2 }} value={introText} onChange={e => setIntroText(e.target.value)} disabled={emailStatus.hasSentEmail} />
            <TextField label="Kapanış Metni (Opsiyonel)" fullWidth size="small" multiline rows={2} value={closingText} onChange={e => setClosingText(e.target.value)} disabled={emailStatus.hasSentEmail} />
          </Box>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <Typography variant="subtitle2" sx={{ mb: 1 }}>E-posta İçeriğine Eklenecekler</Typography>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0 }}>
            <FormControlLabel control={<Checkbox checked={includes.includeParticipants} onChange={handleIncludeChange} name="includeParticipants" disabled={emailStatus.hasSentEmail} />} label="Katılımcılar" />
            <FormControlLabel control={<Checkbox checked={includes.includeTopics} onChange={handleIncludeChange} name="includeTopics" disabled={emailStatus.hasSentEmail} />} label="Konular (Topics)" />
            <FormControlLabel control={<Checkbox checked={includes.includeDecisions} onChange={handleIncludeChange} name="includeDecisions" disabled={emailStatus.hasSentEmail} />} label="Kararlar (Decisions)" />
            <FormControlLabel control={<Checkbox checked={includes.includeActionItems} onChange={handleIncludeChange} name="includeActionItems" disabled={emailStatus.hasSentEmail} />} label="Aksiyonlar (Actions)" />
            <FormControlLabel control={<Checkbox checked={includes.includeOpenIssues} onChange={handleIncludeChange} name="includeOpenIssues" disabled={emailStatus.hasSentEmail} />} label="Açık Konular (Open Issues)" />
            <FormControlLabel control={<Checkbox checked={includes.includeActionStatus} onChange={handleIncludeChange} name="includeActionStatus" disabled={emailStatus.hasSentEmail} />} label="Aksiyon Durumları" />
            <FormControlLabel control={<Checkbox checked={includes.includeEvidence} onChange={handleIncludeChange} name="includeEvidence" disabled={emailStatus.hasSentEmail} />} label="Kanıt Metinleri (Evidence)" />
          </Box>
        </Grid>
      </Grid>

      <Box sx={{ mt: 3, pt: 3, borderTop: 1, borderColor: 'divider', display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
        <Tooltip title={getButtonTooltip(emailStatus.canPreview, 'Önizleme şu an yapılamıyor.')}>
          <span>
            <Button 
              variant="outlined" 
              startIcon={generatePreviewMutation.isPending ? <CircularProgress size={20} /> : <PreviewIcon />} 
              disabled={!emailStatus.canPreview || generatePreviewMutation.isPending}
              onClick={handlePreview}
            >
              Ön İzleme
            </Button>
          </span>
        </Tooltip>

        <Tooltip title={getButtonTooltip(emailStatus.canSendTestEmail, 'Test mail gönderilemiyor.')}>
          <span>
            <Button 
              variant="outlined" 
              color="secondary"
              startIcon={<BugReportIcon />} 
              disabled={!emailStatus.canSendTestEmail}
              onClick={() => { setTestEmailAddress(''); setTestEmailOpen(true); }}
            >
              Test Mail Gönder
            </Button>
          </span>
        </Tooltip>

        <Tooltip title={getButtonTooltip(!isSendDisabled, sendDisabledReason)}>
          <span>
            <Button 
              variant="contained" 
              color="primary" 
              startIcon={<SendIcon />} 
              disabled={isSendDisabled}
              onClick={() => { setSendConfirmationChecked(false); setSendConfirmOpen(true); }}
            >
              Gönder
            </Button>
          </span>
        </Tooltip>
      </Box>

      {/* Preview Dialog */}
      {previewOpen && generatePreviewMutation.data && (
        <EmailPreviewDialog 
          open={previewOpen} 
          onClose={() => setPreviewOpen(false)} 
          preview={generatePreviewMutation.data} 
        />
      )}

      {/* Send Confirm Dialog */}
      <Dialog open={sendConfirmOpen} onClose={() => setSendConfirmOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>E-posta Gönderim Onayı</DialogTitle>
        <DialogContent dividers>
          <Typography variant="body1" sx={{ mb: 2 }}>
            Toplantı özetini ve kararları {toRecipients.length + ccRecipients.length} kişiye e-posta ile göndermek üzeresiniz. 
            Bu işlem geri alınamaz.
          </Typography>
          <FormControlLabel
            control={<Checkbox checked={sendConfirmationChecked} onChange={(e) => setSendConfirmationChecked(e.target.checked)} />}
            label="Bu toplantı özetini göndermeyi onaylıyorum."
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSendConfirmOpen(false)} disabled={sendEmailMutation.isPending}>İptal</Button>
          <Button 
            variant="contained" 
            onClick={handleSend} 
            disabled={!sendConfirmationChecked || sendEmailMutation.isPending}
            startIcon={sendEmailMutation.isPending ? <CircularProgress size={20} /> : <SendIcon />}
          >
            Tümüne Gönder
          </Button>
        </DialogActions>
      </Dialog>

      {/* Test Email Dialog */}
      <Dialog open={testEmailOpen} onClose={() => setTestEmailOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Test E-postası Gönder</DialogTitle>
        <DialogContent dividers>
          <Typography variant="body2" sx={{ mb: 2 }}>
            Sadece formatı ve içeriği test etmek amacıyla kendinize e-posta gönderebilirsiniz. 
            Bu işlem toplantının durumunu "Gönderildi" olarak değiştirmez.
          </Typography>
          <TextField
            fullWidth
            required
            label="E-posta Adresi"
            value={testEmailAddress}
            onChange={(e) => setTestEmailAddress(e.target.value)}
            error={testEmailAddress.trim() !== '' && !emailSchema.safeParse(testEmailAddress).success}
            helperText={testEmailAddress.trim() !== '' && !emailSchema.safeParse(testEmailAddress).success ? 'Geçerli bir e-posta girin' : ''}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setTestEmailOpen(false)} disabled={testEmailMutation.isPending}>İptal</Button>
          <Button 
            variant="contained" 
            color="secondary"
            onClick={() => testEmailMutation.mutate()} 
            disabled={!emailSchema.safeParse(testEmailAddress).success || testEmailMutation.isPending}
            startIcon={testEmailMutation.isPending ? <CircularProgress size={20} /> : <SendIcon />}
          >
            Gönder
          </Button>
        </DialogActions>
      </Dialog>

    </Paper>
  );
};
