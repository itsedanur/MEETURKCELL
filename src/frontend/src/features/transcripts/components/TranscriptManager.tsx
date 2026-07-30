import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Box, Typography, Button, Paper, TextField, CircularProgress, Alert
} from '@mui/material';
import { CloudUpload as CloudUploadIcon, Send as SendIcon } from '@mui/icons-material';

import { transcriptsApi } from '../api/transcriptsApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { ApiError } from '../../../api/apiError';
import { MeetingStatus } from '../../meetings/types/meeting';

interface Props {
  meetingId: string;
  meetingStatus: MeetingStatus;
}

export const TranscriptManager: React.FC<Props> = ({ meetingId, meetingStatus }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();
  
  const [manualText, setManualText] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const canEdit = meetingStatus === MeetingStatus.Draft;

  const { data: transcript, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings.transcript(meetingId),
    queryFn: () => transcriptsApi.getTranscript(meetingId)
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => transcriptsApi.uploadFile(meetingId, { file }),
    onSuccess: () => {
      showNotification('Transcript dosyası yüklendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.transcript(meetingId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      setSelectedFile(null);
    },
    onError: (error: ApiError) => {
      showNotification(error.message, 'error');
    }
  });

  const manualMutation = useMutation({
    mutationFn: (text: string) => transcriptsApi.submitManual(meetingId, { text }),
    onSuccess: () => {
      showNotification('Manuel transcript kaydedildi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.transcript(meetingId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      setManualText('');
    },
    onError: (error: ApiError) => {
      showNotification(error.message, 'error');
    }
  });

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      setSelectedFile(e.target.files[0]);
    }
  };

  const handleUpload = () => {
    if (selectedFile) {
      uploadMutation.mutate(selectedFile);
    }
  };

  const handleManualSubmit = () => {
    if (manualText.trim()) {
      manualMutation.mutate(manualText);
    }
  };

  if (isLoading) {
    return <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>;
  }

  if (isError) {
    return <Typography color="error">Transcript yüklenemedi.</Typography>;
  }

  if (transcript) {
    return (
      <Box>
        <Typography variant="h6" sx={{ mb: 2 }}>Mevcut Transcript</Typography>
        <Paper sx={{ p: 3, bgcolor: 'background.default', maxHeight: 500, overflow: 'auto' }}>
          <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', fontFamily: 'monospace' }}>
            {transcript.originalText}
          </Typography>
        </Paper>
      </Box>
    );
  }

  if (!canEdit) {
    return (
      <Alert severity="info">
        Bu toplantı için henüz transcript eklenmemiş ve toplantı durumu nedeniyle yeni transcript eklenemez.
      </Alert>
    );
  }

  return (
    <Box>
      <Typography variant="h6" sx={{ mb: 2 }}>Transcript Ekle</Typography>
      
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
        <Paper variant="outlined" sx={{ p: 3 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
            Dosya Yükle (.txt veya .vtt)
          </Typography>
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
            <Button
              variant="outlined"
              component="label"
              startIcon={<CloudUploadIcon />}
            >
              Dosya Seç
              <input
                type="file"
                hidden
                accept=".txt,.vtt"
                onChange={handleFileChange}
              />
            </Button>
            <Typography variant="body2" color="text.secondary">
              {selectedFile ? selectedFile.name : 'Henüz dosya seçilmedi'}
            </Typography>
            <Button 
              variant="contained" 
              disabled={!selectedFile || uploadMutation.isPending}
              onClick={handleUpload}
            >
              {uploadMutation.isPending ? <CircularProgress size={24} /> : 'Yükle'}
            </Button>
          </Box>
        </Paper>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Box sx={{ flexGrow: 1, height: '1px', bgcolor: 'divider' }} />
          <Typography variant="body2" color="text.secondary">VEYA</Typography>
          <Box sx={{ flexGrow: 1, height: '1px', bgcolor: 'divider' }} />
        </Box>

        <Paper variant="outlined" sx={{ p: 3 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
            Manuel Metin Girişi
          </Typography>
          <TextField
            multiline
            rows={10}
            fullWidth
            placeholder="Konuşma dökümünü buraya yapıştırın..."
            value={manualText}
            onChange={(e) => setManualText(e.target.value)}
            sx={{ mb: 2 }}
          />
          <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
            <Button 
              variant="contained" 
              startIcon={<SendIcon />}
              onClick={handleManualSubmit}
              disabled={!manualText.trim() || manualMutation.isPending}
            >
              {manualMutation.isPending ? <CircularProgress size={24} /> : 'Kaydet'}
            </Button>
          </Box>
        </Paper>
      </Box>
    </Box>
  );
};
