import React from 'react';
import { 
  Box, Typography, Paper, Grid, Card, CardContent, Button, Chip, 
  List, ListItem, ListItemIcon, ListItemText, Checkbox, Divider, Avatar, Stack
} from '@mui/material';
import { 
  CalendarToday, MeetingRoom, SmartToy, Email, 
  CheckCircle, AccessTime, Add, ArrowForward, Person, NoteAdd, Send, VideoCall
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import 'dayjs/locale/tr';

import { meetingsApi } from '../features/meetings/api/meetingsApi';
import { queryKeys } from '../api/queryKeys';
import { MeetingStatus } from '../features/meetings/types/meeting';
import { useNotification } from '../contexts/NotificationContext';

dayjs.locale('tr');

export const Dashboard = () => {
  const navigate = useNavigate();
  const { showNotification } = useNotification();

  // Fetch meetings list
  const { data: meetingsData } = useQuery({
    queryKey: queryKeys.meetings.list({ pageNumber: 1, pageSize: 10 }),
    queryFn: () => meetingsApi.getMeetings({ pageNumber: 1, pageSize: 10 })
  });

  const meetings = (meetingsData?.items && meetingsData.items.length > 0) ? meetingsData.items : [
    {
      id: 'demo-1',
      title: 'Turkcell 5G Altyapı ve AI Asistan Entegrasyonu',
      description: '5G şebeke optimizasyonu ve Yapay Zeka destekli toplantı özetleme asistanının canlıya geçiş planlaması.',
      meetingDate: dayjs().format('YYYY-MM-DD'),
      startTime: '10:00:00',
      endTime: '11:00:00',
      status: MeetingStatus.Approved,
      statusDisplayName: 'Onaylandı',
      organizerName: 'Edanur Ünal'
    },
    {
      id: 'demo-2',
      title: 'Mobil & Web Uygulaması Sprint Değerlendirmesi',
      description: 'Turkcell Meeting Assistant projesinin frontend arayüz geliştirmeleri ve staj defteri dokümantasyonu.',
      meetingDate: dayjs().format('YYYY-MM-DD'),
      startTime: '14:00:00',
      endTime: '15:00:00',
      status: MeetingStatus.Draft,
      statusDisplayName: 'Taslak',
      organizerName: 'System Admin'
    }
  ];

  const todayMeetings = meetings.filter(m => dayjs(m.meetingDate).isSame(dayjs(), 'day')) || meetings;

  const handleSendEmailSummary = (meetingTitle: string) => {
    showNotification(`"${meetingTitle}" toplantı özeti ve aksiyonları katılımcılara e-posta olarak iletildi.`, 'success');
  };

  return (
    <Box sx={{ pb: 5 }}>
      {/* Header Banner */}
      <Box sx={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center', 
        mb: 4, 
        bgcolor: '#002C5F', 
        color: '#ffffff',
        p: 3, 
        borderRadius: 3,
        boxShadow: '0 4px 20px rgba(0, 44, 95, 0.15)'
      }}>
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 700, mb: 0.5, color: '#ffffff' }}>
            Hoş Geldiniz, Turkcell Toplantı Asistanı 👋
          </Typography>
          <Typography variant="body1" sx={{ opacity: 0.9, color: '#FFC72C', fontWeight: 500 }}>
            {dayjs().format('DD MMMM YYYY, dddd')} — Günlük Toplantı Özetleri, Takvim & Görev Yönetimi
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button 
            variant="contained" 
            startIcon={<VideoCall />}
            onClick={() => navigate('/meetings/live')}
            sx={{ 
              bgcolor: '#FFC72C', 
              color: '#002C5F', 
              fontWeight: 700,
              '&:hover': { bgcolor: '#ffd45e' } 
            }}
          >
            🎙️ Canlı Toplantı Başlat
          </Button>
          <Button 
            variant="outlined" 
            startIcon={<NoteAdd />}
            onClick={() => navigate('/meetings/new')}
            sx={{ 
              borderColor: '#FFC72C', 
              color: '#FFC72C', 
              fontWeight: 700,
              '&:hover': { borderColor: '#ffd45e', bgcolor: 'rgba(255,199,44,0.1)' } 
            }}
          >
            Yeni Planla
          </Button>
        </Box>
      </Box>

      {/* Top Stat Cards */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Paper sx={{ p: 2.5, borderRadius: 3, borderLeft: '5px solid #002C5F' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Box>
                <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
                  Toplam Toplantı
                </Typography>
                <Typography variant="h3" sx={{ fontWeight: 700, color: '#002C5F', mt: 0.5 }}>
                  {meetings.length}
                </Typography>
              </Box>
              <Avatar sx={{ bgcolor: 'rgba(0, 44, 95, 0.1)', color: '#002C5F' }}>
                <MeetingRoom />
              </Avatar>
            </Box>
          </Paper>
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Paper sx={{ p: 2.5, borderRadius: 3, borderLeft: '5px solid #FFC72C' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Box>
                <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
                  Bugünkü Program
                </Typography>
                <Typography variant="h3" sx={{ fontWeight: 700, color: '#b28b1e', mt: 0.5 }}>
                  {todayMeetings.length}
                </Typography>
              </Box>
              <Avatar sx={{ bgcolor: 'rgba(255, 199, 44, 0.2)', color: '#b28b1e' }}>
                <CalendarToday />
              </Avatar>
            </Box>
          </Paper>
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Paper sx={{ p: 2.5, borderRadius: 3, borderLeft: '5px solid #2e7d32' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Box>
                <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
                  Tamamlanan AI Özetler
                </Typography>
                <Typography variant="h3" sx={{ fontWeight: 700, color: '#2e7d32', mt: 0.5 }}>
                  1
                </Typography>
              </Box>
              <Avatar sx={{ bgcolor: 'rgba(46, 125, 50, 0.1)', color: '#2e7d32' }}>
                <SmartToy />
              </Avatar>
            </Box>
          </Paper>
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Paper sx={{ p: 2.5, borderRadius: 3, borderLeft: '5px solid #ed6c02' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Box>
                <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
                  Açık Görevler / Aksiyonlar
                </Typography>
                <Typography variant="h3" sx={{ fontWeight: 700, color: '#ed6c02', mt: 0.5 }}>
                  3
                </Typography>
              </Box>
              <Avatar sx={{ bgcolor: 'rgba(237, 108, 2, 0.1)', color: '#ed6c02' }}>
                <CheckCircle />
              </Avatar>
            </Box>
          </Paper>
        </Grid>
      </Grid>

      {/* Main Content Grid: Daily Calendar & AI Summaries / Actions */}
      <Grid container spacing={3}>
        {/* Left Column: Daily Calendar Widget */}
        <Grid size={{ xs: 12, md: 7 }}>
          <Paper sx={{ p: 3, borderRadius: 3, height: '100%' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <CalendarToday color="primary" />
                <Typography variant="h6" sx={{ fontWeight: 700, color: '#002C5F' }}>
                  Günlük Takvim & Toplantı Akışı
                </Typography>
              </Box>
              <Chip 
                label={dayjs().format('DD MMMM YYYY')} 
                color="primary" 
                variant="outlined" 
                size="small" 
                sx={{ fontWeight: 600 }}
              />
            </Box>

            <List sx={{ width: '100%' }}>
              {todayMeetings.map((item) => (
                <React.Fragment key={item.id}>
                  <Paper 
                    variant="outlined" 
                    sx={{ 
                      p: 2, 
                      mb: 2, 
                      borderRadius: 2, 
                      bgcolor: '#fafafa',
                      borderLeft: '4px solid #002C5F',
                      '&:hover': { bgcolor: '#f0f4f9', borderColor: '#FFC72C' }
                    }}
                  >
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <AccessTime fontSize="small" color="action" />
                        <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#002C5F' }}>
                          {item.startTime.substring(0, 5)} - {item.endTime.substring(0, 5)}
                        </Typography>
                        <Chip 
                          label={item.statusDisplayName || 'Planlandı'} 
                          size="small" 
                          color={item.status === MeetingStatus.Approved ? 'success' : 'warning'} 
                          sx={{ height: 22, fontSize: '0.75rem', fontWeight: 600 }}
                        />
                      </Box>
                      <Button 
                        size="small" 
                        endIcon={<ArrowForward />} 
                        onClick={() => navigate(`/meetings/${item.id}`)}
                      >
                        İncele & Not Al
                      </Button>
                    </Box>

                    <Typography variant="h6" sx={{ fontSize: '1.05rem', fontWeight: 600, mb: 0.5 }}>
                      {item.title}
                    </Typography>

                    <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
                      {item.description || 'Toplantı açıklaması belirtilmemiş.'}
                    </Typography>

                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Avatar sx={{ width: 24, height: 24, fontSize: '0.75rem', bgcolor: '#002C5F' }}>
                        {(item as any).organizerName?.charAt(0) || 'E'}
                      </Avatar>
                      <Typography variant="caption" color="text.secondary">
                        Düzenleyen: <strong>{(item as any).organizerName || 'Edanur Ünal'}</strong>
                      </Typography>
                    </Box>
                  </Paper>
                </React.Fragment>
              ))}
            </List>
          </Paper>
        </Grid>

        {/* Right Column: Recent AI Summaries & Email Action */}
        <Grid size={{ xs: 12, md: 5 }}>
          <Stack spacing={3}>
            {/* AI Summary Card */}
            <Paper sx={{ p: 3, borderRadius: 3, bgcolor: '#ffffff', borderTop: '4px solid #FFC72C' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <SmartToy sx={{ color: '#002C5F' }} />
                <Typography variant="h6" sx={{ fontWeight: 700, color: '#002C5F' }}>
                  Son AI Toplantı Özeti
                </Typography>
              </Box>

              <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#1A2027', mb: 1 }}>
                Turkcell 5G Altyapı ve AI Asistan Entegrasyonu
              </Typography>

              <Typography variant="body2" color="text.secondary" sx={{ mb: 2, bgcolor: '#f8f9fa', p: 1.5, borderRadius: 1.5, fontStyle: 'italic' }}>
                "Turkcell 5G baz istasyonlarında AI destekli yük dengeleme sistemi başarıyla test edildi. Toplantı asistanı modülü canlı ortama entegre edildi ve otomatik aksiyon maddeleri üretildi."
              </Typography>

              <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                <Chip label="5G Optimizasyonu" size="small" variant="outlined" />
                <Chip label="AI Not Alma" size="small" color="primary" variant="outlined" />
                <Chip label="Canlı Ortam" size="small" color="success" variant="outlined" />
              </Box>

              <Button
                variant="contained"
                fullWidth
                startIcon={<Send />}
                onClick={() => handleSendEmailSummary('Turkcell 5G Altyapı ve AI Asistan Entegrasyonu')}
                sx={{ bgcolor: '#002C5F', color: '#ffffff', fontWeight: 600, '&:hover': { bgcolor: '#001e42' } }}
              >
                Katılımcılara E-posta ile Gönder
              </Button>
            </Paper>

            {/* Task Checklist Widget */}
            <Paper sx={{ p: 3, borderRadius: 3 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <CheckCircle sx={{ color: '#ed6c02' }} />
                  <Typography variant="h6" sx={{ fontWeight: 700, color: '#002C5F' }}>
                    Görevlerim & Aksiyonlar
                  </Typography>
                </Box>
                <Button size="small" onClick={() => navigate('/my-actions')}>
                  Tümü
                </Button>
              </Box>

              <List dense>
                <ListItem disablePadding sx={{ py: 1 }}>
                  <ListItemIcon sx={{ minWidth: 36 }}>
                    <Checkbox defaultChecked size="small" color="primary" />
                  </ListItemIcon>
                  <ListItemText 
                    primary={<span style={{ textDecoration: 'line-through', color: '#888' }}>Frontend UI temasını Turkcell kurumsal renklerine göre optimize et</span>}
                    secondary="Tamamlandı • Edanur Ünal"
                  />
                </ListItem>
                <Divider component="li" />
                <ListItem disablePadding sx={{ py: 1 }}>
                  <ListItemIcon sx={{ minWidth: 36 }}>
                    <Checkbox size="small" color="primary" />
                  </ListItemIcon>
                  <ListItemText 
                    primary="Staj defteri için projenin canlı sistem ekran görüntülerini derle"
                    secondary="Devam Ediyor • Son Tarih: Yarın"
                  />
                  <Chip label="Yüksek" size="small" color="error" sx={{ height: 20, fontSize: '0.7rem' }} />
                </ListItem>
                <Divider component="li" />
                <ListItem disablePadding sx={{ py: 1 }}>
                  <ListItemIcon sx={{ minWidth: 36 }}>
                    <Checkbox size="small" color="primary" />
                  </ListItemIcon>
                  <ListItemText 
                    primary="PostgreSQL veritabanı performansını incele"
                    secondary="Açık • Sistem Yöneticisi"
                  />
                  <Chip label="Düşük" size="small" color="info" sx={{ height: 20, fontSize: '0.7rem' }} />
                </ListItem>
              </List>
            </Paper>
          </Stack>
        </Grid>
      </Grid>
    </Box>
  );
};

