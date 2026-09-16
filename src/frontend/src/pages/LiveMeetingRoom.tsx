import React, { useState, useEffect } from 'react';
import { 
  Box, Typography, Paper, Button, Grid, TextField, Chip, Avatar, 
  List, ListItem, ListItemAvatar, ListItemText, Divider, Stack, Alert, LinearProgress
} from '@mui/material';
import { 
  Mic, MicOff, Stop, PlayArrow, Send, SmartToy, Email, 
  CheckCircle, AccessTime, PersonAdd, Save, VideoCall
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import dayjs from 'dayjs';
import { useNotification } from '../contexts/NotificationContext';

export const LiveMeetingRoom = () => {
  const navigate = useNavigate();
  const { showNotification } = useNotification();

  const [isRecording, setIsRecording] = useState<boolean>(false);
  const [recordingTime, setRecordingTime] = useState<number>(0);
  const [meetingTitle, setMeetingTitle] = useState<string>('Turkcell Canlı Toplantı & AI Asistan Session');
  const [liveNotes, setLiveNotes] = useState<string>('');
  
  const [participants, setParticipants] = useState([
    { id: '1', name: 'Edanur Ünal', email: 'user@meetingassistant.local', role: 'Organizatör' },
    { id: '2', name: 'Ahmet Yurt', email: 'ahmet.yurt@turkcell.com.tr', role: 'Katılımcı' },
    { id: '3', name: 'Sistem Yöneticisi', email: 'admin@meetingassistant.local', role: 'Kıdemli Mimar' }
  ]);

  const [liveTranscript, setLiveTranscript] = useState([
    {
      id: 'tr-1',
      speaker: 'Konuşmacı 1 (Edanur Ünal)',
      time: '18:10:02',
      text: 'Arkadaşlar toplantıyı başlatıyorum. Bugün 5G optimizasyonu ve AI Asistan modülünü değerlendireceğiz.',
      avatarBg: '#002C5F'
    },
    {
      id: 'tr-2',
      speaker: 'Konuşmacı 2 (Ahmet Yurt)',
      time: '18:10:18',
      text: 'Sistem entegrasyonu tamamlandı. Ses kayıtları canlı olarak Türkçe metne dönüştürülüyor ve konuşmacılar ayrıştırılıyor.',
      avatarBg: '#FFC72C'
    }
  ]);

  // Timer effect when recording
  useEffect(() => {
    let interval: any = null;
    if (isRecording) {
      interval = setInterval(() => {
        setRecordingTime(prev => prev + 1);
      }, 1000);
    } else {
      clearInterval(interval);
    }
    return () => clearInterval(interval);
  }, [isRecording]);

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const handleStartRecording = () => {
    setIsRecording(true);
    showNotification('Canlı mikrofon kaydı ve Konuşmacı Ayrımı (STT) başlatıldı.', 'info');
  };

  const handleStopRecording = () => {
    setIsRecording(false);
    showNotification('Mikrofon kaydı durduruldu. Canlı ses verileri işlendi.', 'success');
  };

  const handleAddLiveNote = () => {
    if (!liveNotes.trim()) return;
    setLiveTranscript(prev => [
      ...prev,
      {
        id: `tr-${Date.now()}`,
        speaker: 'Not (Edanur Ünal)',
        time: dayjs().format('HH:mm:ss'),
        text: liveNotes,
        avatarBg: '#2e7d32'
      }
    ]);
    setLiveNotes('');
    showNotification('Toplantı notu canlı akışa eklendi.', 'success');
  };

  const handleFinishMeeting = () => {
    showNotification('Toplantı tamamlandı! AI Özeti çıkarıldı ve tüm katılımcılara e-posta olarak iletildi.', 'success');
    setTimeout(() => {
      navigate('/dashboard');
    }, 1500);
  };

  return (
    <Box sx={{ pb: 6 }}>
      {/* Top Banner Header */}
      <Box sx={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center', 
        mb: 4, 
        bgcolor: '#002C5F', 
        color: '#ffffff',
        p: 3, 
        borderRadius: 3,
        boxShadow: '0 4px 20px rgba(0, 44, 95, 0.2)'
      }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Avatar sx={{ bgcolor: '#FFC72C', color: '#002C5F', width: 48, height: 48 }}>
            <VideoCall fontSize="large" />
          </Avatar>
          <Box>
            <Typography variant="h5" sx={{ fontWeight: 700, color: '#ffffff' }}>
              Canlı Toplantı & AI Not Alma Odası
            </Typography>
            <Typography variant="body2" sx={{ color: '#FFC72C', fontWeight: 500 }}>
              Gerçek Zamanlı Ses Kaydı, Konuşmacı Ayrımı (Speaker Diarization) & Otomatik E-Posta Gönderimi
            </Typography>
          </Box>
        </Box>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          {isRecording && (
            <Chip 
              icon={<AccessTime />} 
              label={`KAYITTA: ${formatTime(recordingTime)}`} 
              color="error" 
              sx={{ fontWeight: 700, fontSize: '0.9rem' }}
            />
          )}

          <Button
            variant="contained"
            color="success"
            startIcon={<CheckCircle />}
            onClick={handleFinishMeeting}
            sx={{ fontWeight: 700, px: 3, height: 42 }}
          >
            Toplantıyı Bitir & AI Özeti Gönder
          </Button>
        </Box>
      </Box>

      {/* Main Grid: Live Recording & Transcript / Notes */}
      <Grid container spacing={3}>
        {/* Left Column: Live Audio Controls & Live Transcript Feed */}
        <Grid size={{ xs: 12, md: 7 }}>
          <Stack spacing={3}>
            {/* Audio Recording Control Panel */}
            <Paper sx={{ p: 3, borderRadius: 3, borderLeft: '5px solid #FFC72C' }}>
              <Typography variant="h6" sx={{ fontWeight: 700, color: '#002C5F', mb: 2 }}>
                🎙️ Mikrofon & Canlı Ses Analiz Paneli
              </Typography>

              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                {!isRecording ? (
                  <Button
                    variant="contained"
                    size="large"
                    startIcon={<Mic />}
                    onClick={handleStartRecording}
                    sx={{ bgcolor: '#002C5F', color: '#ffffff', fontWeight: 700, py: 1.5, px: 3 }}
                  >
                    Canlı Mikrofonu Başlat
                  </Button>
                ) : (
                  <Button
                    variant="contained"
                    color="error"
                    size="large"
                    startIcon={<Stop />}
                    onClick={handleStopRecording}
                    sx={{ fontWeight: 700, py: 1.5, px: 3 }}
                  >
                    Kaydı Durdur
                  </Button>
                )}

                <Typography variant="body2" color="text.secondary">
                  {isRecording ? 'Ses dalgaları ve konuşmacı ayrımı canlı işleniyor...' : 'Mikrofon kapalı. Kayda başlamak için butona tıklayın.'}
                </Typography>
              </Box>

              {isRecording && (
                <Box sx={{ width: '100%', mb: 1 }}>
                  <Typography variant="caption" color="primary" sx={{ fontWeight: 600 }}>
                    Ses Frekans Dalgaları (STT Active Stream)
                  </Typography>
                  <LinearProgress color="secondary" sx={{ height: 8, borderRadius: 2, mt: 0.5 }} />
                </Box>
              )}
            </Paper>

            {/* Live Diarized Transcript Feed */}
            <Paper sx={{ p: 3, borderRadius: 3, minHeight: 380 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" sx={{ fontWeight: 700, color: '#002C5F' }}>
                  🗣️ Canlı Transkripsiyon & Konuşmacı Akışı
                </Typography>
                <Chip label={`${liveTranscript.length} İleti`} size="small" color="primary" variant="outlined" />
              </Box>

              <List sx={{ maxHeight: 320, overflow: 'auto' }}>
                {liveTranscript.map((item) => (
                  <Paper key={item.id} variant="outlined" sx={{ p: 2, mb: 2, borderRadius: 2, bgcolor: '#fafafa' }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Avatar sx={{ width: 22, height: 22, fontSize: '0.7rem', bgcolor: item.avatarBg }}>
                          {item.speaker.charAt(0)}
                        </Avatar>
                        <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#002C5F' }}>
                          {item.speaker}
                        </Typography>
                      </Box>
                      <Typography variant="caption" color="text.secondary">
                        {item.time}
                      </Typography>
                    </Box>
                    <Typography variant="body2" color="text.primary">
                      "{item.text}"
                    </Typography>
                  </Paper>
                ))}
              </List>
            </Paper>
          </Stack>
        </Grid>

        {/* Right Column: Live Note Taking & Participant List */}
        <Grid size={{ xs: 12, md: 5 }}>
          <Stack spacing={3}>
            {/* Live Notes Notepad */}
            <Paper sx={{ p: 3, borderRadius: 3, borderTop: '4px solid #002C5F' }}>
              <Typography variant="h6" sx={{ fontWeight: 700, color: '#002C5F', mb: 2 }}>
                ✍️ Anlık Toplantı Not Defteri
              </Typography>

              <TextField
                multiline
                rows={5}
                fullWidth
                placeholder="Toplantı esnasında alınan özel notlar, kararlar veya vurgulamalar..."
                value={liveNotes}
                onChange={(e) => setLiveNotes(e.target.value)}
                sx={{ mb: 2 }}
              />

              <Button
                variant="contained"
                fullWidth
                startIcon={<Save />}
                onClick={handleAddLiveNote}
                disabled={!liveNotes.trim()}
                sx={{ bgcolor: '#002C5F', color: '#fff', fontWeight: 600 }}
              >
                Notu Canlı Akışa Ekle
              </Button>
            </Paper>

            {/* Participants & Auto Email Dispatch List */}
            <Paper sx={{ p: 3, borderRadius: 3 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" sx={{ fontWeight: 700, color: '#002C5F' }}>
                  👥 Katılımcılar & E-Posta Alıcıları
                </Typography>
                <Chip label={`${participants.length} Kişi`} size="small" color="success" />
              </Box>

              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 2 }}>
                Toplantı bittiğinde AI özeti ve aksiyonlar aşağıdaki e-posta adreslerine otomatik iletilecektir.
              </Typography>

              <List dense>
                {participants.map((p) => (
                  <ListItem key={p.id} sx={{ py: 1, px: 0 }}>
                    <ListItemAvatar sx={{ minWidth: 36 }}>
                      <Avatar sx={{ width: 28, height: 28, fontSize: '0.8rem', bgcolor: '#002C5F' }}>
                        {p.name.charAt(0)}
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText 
                      primary={<span style={{ fontWeight: 600, fontSize: '0.875rem' }}>{p.name}</span>} 
                      secondary={p.email} 
                    />
                    <Chip label={p.role} size="small" variant="outlined" sx={{ fontSize: '0.7rem', height: 20 }} />
                  </ListItem>
                ))}
              </List>
            </Paper>
          </Stack>
        </Grid>
      </Grid>
    </Box>
  );
};
