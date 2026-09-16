import React, { useState } from 'react';
import { 
  Box, Typography, Paper, Table, TableBody, TableCell, TableContainer, 
  TableHead, TableRow, Chip, IconButton, Button, Checkbox, Stack, TextField, MenuItem 
} from '@mui/material';
import { CheckCircle, FilterList, Email, Edit, Delete } from '@mui/icons-material';
import dayjs from 'dayjs';
import { useNotification } from '../contexts/NotificationContext';

export const MyActionsPage = () => {
  const { showNotification } = useNotification();
  const [filterPriority, setFilterPriority] = useState<string>('all');

  const [actions, setActions] = useState([
    {
      id: 'act-1',
      meetingTitle: 'Turkcell 5G Altyapı ve AI Asistan Entegrasyonu',
      description: 'Staj defteri için projenin canlı sistem ekran görüntülerini derle ve rapora ekle.',
      ownerName: 'Edanur Ünal',
      ownerEmail: 'user@meetingassistant.local',
      dueDate: dayjs().add(1, 'day').format('YYYY-MM-DD'),
      priority: 'High',
      priorityLabel: 'Yüksek',
      status: 'InProgress',
      statusLabel: 'Devam Ediyor',
      speakerLabel: 'Konuşmacı 1 (Edanur Ünal)'
    },
    {
      id: 'act-2',
      meetingTitle: 'Turkcell 5G Altyapı ve AI Asistan Entegrasyonu',
      description: 'Frontend UI temasını Turkcell kurumsal renk paletine (Lacivert #002C5F & Sarı #FFC72C) göre optimize et.',
      ownerName: 'Edanur Ünal',
      ownerEmail: 'user@meetingassistant.local',
      dueDate: dayjs().format('YYYY-MM-DD'),
      priority: 'Medium',
      priorityLabel: 'Orta',
      status: 'Completed',
      statusLabel: 'Tamamlandı',
      speakerLabel: 'Konuşmacı 2 (Ahmet Yurt)'
    },
    {
      id: 'act-3',
      meetingTitle: 'Mobil & Web Uygulaması Sprint Değerlendirmesi',
      description: 'PostgreSQL veritabanı performansını incele ve indeks tanımlamalarını kontrol et.',
      ownerName: 'System Admin',
      ownerEmail: 'admin@meetingassistant.local',
      dueDate: dayjs().add(3, 'day').format('YYYY-MM-DD'),
      priority: 'Low',
      priorityLabel: 'Düşük',
      status: 'Open',
      statusLabel: 'Açık',
      speakerLabel: 'Konuşmacı 1 (System Admin)'
    }
  ]);

  const toggleStatus = (id: string) => {
    setActions(prev => prev.map(item => {
      if (item.id === id) {
        const isComp = item.status === 'Completed';
        const newStatus = isComp ? 'Open' : 'Completed';
        showNotification(`Aksiyon durumu "${newStatus === 'Completed' ? 'Tamamlandı' : 'Açık'}" olarak güncellendi.`, 'info');
        return {
          ...item,
          status: newStatus,
          statusLabel: newStatus === 'Completed' ? 'Tamamlandı' : 'Açık'
        };
      }
      return item;
    }));
  };

  const handleSendEmail = (actionDescription: string, email: string) => {
    showNotification(`Görev detayı (${actionDescription}) -> ${email} adresine e-posta olarak iletildi.`, 'success');
  };

  const filteredActions = actions.filter(a => {
    if (filterPriority === 'all') return true;
    return a.priority.toLowerCase() === filterPriority.toLowerCase();
  });

  return (
    <Box sx={{ pb: 5 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" color="primary.main" sx={{ fontWeight: 700 }}>
          Aksiyonlarım & Görev Yönetimi
        </Typography>
        <Stack direction="row" spacing={2}>
          <TextField
            select
            size="small"
            label="Öncelik Filtresi"
            value={filterPriority}
            onChange={(e) => setFilterPriority(e.target.value)}
            sx={{ width: 180 }}
          >
            <MenuItem value="all">Tüm Öncelikler</MenuItem>
            <MenuItem value="High">Yüksek</MenuItem>
            <MenuItem value="Medium">Orta</MenuItem>
            <MenuItem value="Low">Düşük</MenuItem>
          </TextField>
        </Stack>
      </Box>

      <TableContainer component={Paper} sx={{ borderRadius: 3, boxShadow: '0 2px 10px rgba(0,0,0,0.05)' }}>
        <Table>
          <TableHead sx={{ bgcolor: '#002C5F' }}>
            <TableRow>
              <TableCell sx={{ color: '#ffffff', fontWeight: 700 }}>Durum</TableCell>
              <TableCell sx={{ color: '#ffffff', fontWeight: 700 }}>Toplantı</TableCell>
              <TableCell sx={{ color: '#ffffff', fontWeight: 700 }}>Görev / Aksiyon Açıklaması</TableCell>
              <TableCell sx={{ color: '#ffffff', fontWeight: 700 }}>Sorumlu & Konuşmacı</TableCell>
              <TableCell sx={{ color: '#ffffff', fontWeight: 700 }}>Termin Tarihi</TableCell>
              <TableCell sx={{ color: '#ffffff', fontWeight: 700 }}>Öncelik</TableCell>
              <TableCell align="right" sx={{ color: '#ffffff', fontWeight: 700 }}>E-Posta Gönder</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredActions.map((item) => (
              <TableRow key={item.id} hover sx={{ opacity: item.status === 'Completed' ? 0.75 : 1 }}>
                <TableCell>
                  <Checkbox
                    checked={item.status === 'Completed'}
                    onChange={() => toggleStatus(item.id)}
                    color="primary"
                  />
                </TableCell>

                <TableCell>
                  <Typography variant="body2" sx={{ fontWeight: 600, color: '#002C5F' }}>
                    {item.meetingTitle}
                  </Typography>
                </TableCell>

                <TableCell>
                  <Typography 
                    variant="body2" 
                    sx={{ 
                      textDecoration: item.status === 'Completed' ? 'line-through' : 'none',
                      color: item.status === 'Completed' ? 'text.secondary' : 'text.primary' 
                    }}
                  >
                    {item.description}
                  </Typography>
                </TableCell>

                <TableCell>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {item.ownerName}
                  </Typography>
                  <Chip 
                    label={item.speakerLabel} 
                    size="small" 
                    variant="outlined" 
                    sx={{ fontSize: '0.7rem', height: 20, mt: 0.5 }} 
                  />
                </TableCell>

                <TableCell>
                  <Typography variant="body2" color="text.secondary">
                    {dayjs(item.dueDate).format('DD.MM.YYYY')}
                  </Typography>
                </TableCell>

                <TableCell>
                  <Chip 
                    label={item.priorityLabel} 
                    color={
                      item.priority === 'High' ? 'error' :
                      item.priority === 'Medium' ? 'warning' : 'info'
                    }
                    size="small"
                    sx={{ fontWeight: 600 }}
                  />
                </TableCell>

                <TableCell align="right">
                  <IconButton 
                    color="primary"
                    onClick={() => handleSendEmail(item.description, item.ownerEmail)}
                    title="Görev Detayını E-posta İle Gönder"
                  >
                    <Email />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
};
