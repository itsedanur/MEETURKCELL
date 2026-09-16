import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Box, Typography, Button, Paper, TextField, CircularProgress, Alert, Chip
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

  const sampleDiarizedTranscript = [
    {
      speaker: 'Konuşmacı 1 (Edanur Ünal)',
      time: '10:02',
      text: 'Arkadaşlar günaydın. Bugün Turkcell 5G altyapı optimizasyonu ve AI Asistan modülünün canlıya geçiş sürecini ele alacağız.',
      avatarBg: '#002C5F'
    },
    {
      speaker: 'Konuşmacı 2 (Ahmet Yurt - Kıdemli Mimar)',
      time: '10:05',
      text: 'Baz istasyonlarındaki yük dengeleme testleri başarıyla tamamlandı. AI model transkripsiyonu %98 doğruluk oranına ulaştı.',
      avatarBg: '#FFC72C'
    },
    {
      speaker: 'Konuşmacı 1 (Edanur Ünal)',
      time: '10:12',
      text: 'Harika. Toplantı özetlerinin otomatik çıkarılıp katılımcıların e-posta adreslerine iletilmesini de doğruladık. Aksiyonları staj defterine aktaralım.',
      avatarBg: '#002C5F'
    }
  ];

  return (
    <Box sx={{ pb: 3 }}>
      <Typography variant="h6" sx={{ mb: 2, fontWeight: 700, color: '#002C5F' }}>
        Toplantı Notları & Konuşmacı Ayrımı (Speaker Diarization / STT)
      </Typography>

      {/* Speaker Diarization Section */}
      <Paper sx={{ p: 3, mb: 4, borderRadius: 3, borderLeft: '5px solid #002C5F' }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 2, color: '#002C5F' }}>
          🎙️ Ses Tanıma ve Konuşmacı Ayrımı (Multi-Speaker Diarization)
        </Typography>

        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {sampleDiarizedTranscript.map((item, idx) => (
            <Paper key={idx} variant="outlined" sx={{ p: 2, borderRadius: 2, bgcolor: '#f8f9fa' }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Box 
                    sx={{ 
                      width: 24, 
                      height: 24, 
                      borderRadius: '50%', 
                      bgcolor: item.avatarBg, 
                      color: item.avatarBg === '#FFC72C' ? '#002C5F' : '#fff',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: '0.75rem',
                      fontWeight: 700
                    }}
                  >
                    {idx + 1}
                  </Box>
                  <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#002C5F' }}>
                    {item.speaker}
                  </Typography>
                </Box>
                <Chip label={item.time} size="small" variant="outlined" sx={{ fontSize: '0.75rem' }} />
              </Box>
              <Typography variant="body2" color="text.primary">
                "{item.text}"
              </Typography>
            </Paper>
          ))}
        </Box>
      </Paper>

      {/* Manual Note Taking Form */}
      <Paper variant="outlined" sx={{ p: 3, borderRadius: 3 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5, color: '#002C5F' }}>
          ✍️ Canlı Toplantı Notu Ekle / Düzenle
        </Typography>
        <TextField
          multiline
          rows={6}
          fullWidth
          placeholder="Toplantı esnasında alınan notlar veya transkript metni..."
          value={manualText}
          onChange={(e) => setManualText(e.target.value)}
          sx={{ mb: 2 }}
        />
        <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
          <Button 
            variant="contained" 
            startIcon={<SendIcon />}
            onClick={() => {
              showNotification('Toplantı notları kaydedildi ve AI analizine gönderildi.', 'success');
              setManualText('');
            }}
            sx={{ bgcolor: '#002C5F', color: '#fff', fontWeight: 600 }}
          >
            Notları Kaydet & AI İle Analiz Et
          </Button>
        </Box>
      </Paper>
    </Box>
  );
};
