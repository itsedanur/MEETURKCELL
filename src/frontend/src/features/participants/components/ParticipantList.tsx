import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Box, Typography, Button, Table, TableBody, TableCell, TableContainer, 
  TableHead, TableRow, Paper, IconButton, Chip, Dialog, DialogTitle, 
  DialogContent, DialogActions, TextField, CircularProgress, Checkbox
} from '@mui/material';
import { Add as AddIcon, Delete as DeleteIcon, Edit as EditIcon } from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

import { participantsApi } from '../api/participantsApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { ApiError } from '../../../api/apiError';
import { MeetingParticipantDto, AddParticipantRequest, UpdateParticipantRequest } from '../types/participant';
import { MeetingStatus } from '../../meetings/types/meeting';

const participantSchema = z.object({
  fullName: z.string().min(1, 'Ad Soyad zorunludur').max(100),
  email: z.string().email('Geçerli bir e-posta giriniz').max(100),
  company: z.string().max(100).optional().nullable(),
  title: z.string().max(100).optional().nullable(),
  isRequired: z.boolean(),
  attended: z.boolean(),
});

type ParticipantInputs = z.infer<typeof participantSchema>;

interface Props {
  meetingId: string;
  meetingStatus: MeetingStatus;
}

export const ParticipantList: React.FC<Props> = ({ meetingId, meetingStatus }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();
  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  const canEdit = meetingStatus === MeetingStatus.Draft || meetingStatus === MeetingStatus.ReadyForAnalysis;

  const { data: participants, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings.participants(meetingId),
    queryFn: () => participantsApi.getParticipants(meetingId)
  });

  const { control, register, handleSubmit, reset, setValue, formState: { errors } } = useForm<ParticipantInputs>({
    resolver: zodResolver(participantSchema),
    defaultValues: {
      isRequired: true,
      attended: false,
    }
  });

  const addMutation = useMutation({
    mutationFn: (data: AddParticipantRequest) => participantsApi.addParticipant(meetingId, data),
    onSuccess: () => {
      showNotification('Katılımcı eklendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.participants(meetingId) });
      handleClose();
    },
    onError: (error: ApiError) => {
      showNotification(error.message, 'error');
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string, data: UpdateParticipantRequest }) => participantsApi.updateParticipant(meetingId, id, data),
    onSuccess: () => {
      showNotification('Katılımcı güncellendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.participants(meetingId) });
      handleClose();
    },
    onError: (error: ApiError) => {
      showNotification(error.message, 'error');
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => participantsApi.removeParticipant(meetingId, id),
    onSuccess: () => {
      showNotification('Katılımcı silindi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.participants(meetingId) });
    },
    onError: (error: ApiError) => {
      showNotification(error.message, 'error');
    }
  });

  const handleOpenNew = () => {
    reset({ isRequired: true, attended: false, fullName: '', email: '', company: '', title: '' });
    setEditingId(null);
    setOpen(true);
  };

  const handleOpenEdit = (participant: MeetingParticipantDto) => {
    reset({
      fullName: participant.fullName,
      email: participant.email,
      company: participant.company,
      title: participant.title,
      isRequired: participant.isRequired,
      attended: participant.attended,
    });
    setEditingId(participant.id);
    setOpen(true);
  };

  const handleClose = () => {
    setOpen(false);
  };

  const onSubmit = (data: ParticipantInputs) => {
    if (editingId) {
      updateMutation.mutate({ id: editingId, data: data as UpdateParticipantRequest });
    } else {
      addMutation.mutate(data as AddParticipantRequest);
    }
  };

  if (isLoading) {
    return <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>;
  }

  if (isError) {
    return <Typography color="error">Katılımcılar yüklenemedi.</Typography>;
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Katılımcı Listesi ({participants?.length || 0})</Typography>
        {canEdit && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={handleOpenNew} size="small">
            Katılımcı Ekle
          </Button>
        )}
      </Box>

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead sx={{ bgcolor: 'rgba(0,0,0,0.02)' }}>
            <TableRow>
              <TableCell>Ad Soyad</TableCell>
              <TableCell>E-posta</TableCell>
              <TableCell>Şirket/Unvan</TableCell>
              <TableCell>Katılım Durumu</TableCell>
              <TableCell>Zorunluluk</TableCell>
              {canEdit && <TableCell align="right">İşlemler</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {participants?.map((p) => (
              <TableRow key={p.id}>
                <TableCell>{p.fullName}</TableCell>
                <TableCell>{p.email}</TableCell>
                <TableCell>
                  {p.company} {p.company && p.title && '-'} {p.title}
                </TableCell>
                <TableCell>
                  {p.attended ? (
                    <Chip label="Katıldı" color="success" size="small" />
                  ) : (
                    <Chip label="Katılmadı" color="default" size="small" />
                  )}
                </TableCell>
                <TableCell>
                  {p.isRequired ? (
                    <Typography variant="body2" color="error.main">Zorunlu</Typography>
                  ) : (
                    <Typography variant="body2" color="text.secondary">İsteğe Bağlı</Typography>
                  )}
                </TableCell>
                {canEdit && (
                  <TableCell align="right">
                    <IconButton size="small" onClick={() => handleOpenEdit(p)} color="primary">
                      <EditIcon fontSize="small" />
                    </IconButton>
                    <IconButton size="small" onClick={() => {
                      if(window.confirm('Emin misiniz?')) deleteMutation.mutate(p.id);
                    }} color="error">
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </TableCell>
                )}
              </TableRow>
            ))}
            {participants?.length === 0 && (
              <TableRow>
                <TableCell colSpan={canEdit ? 6 : 5} align="center" sx={{ py: 3 }}>
                  Henüz katılımcı eklenmemiş.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)}>
          <DialogTitle>{editingId ? 'Katılımcı Düzenle' : 'Yeni Katılımcı Ekle'}</DialogTitle>
          <DialogContent dividers>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
              <TextField
                label="Ad Soyad"
                fullWidth
                {...register('fullName')}
                error={!!errors.fullName}
                helperText={errors.fullName?.message}
              />
              <TextField
                label="E-posta"
                type="email"
                fullWidth
                {...register('email')}
                error={!!errors.email}
                helperText={errors.email?.message}
              />
              <TextField
                label="Şirket"
                fullWidth
                {...register('company')}
                error={!!errors.company}
                helperText={errors.company?.message}
              />
              <TextField
                label="Unvan"
                fullWidth
                {...register('title')}
                error={!!errors.title}
                helperText={errors.title?.message}
              />
              
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Controller
                  name="isRequired"
                  control={control}
                  render={({ field: { onChange, value } }) => (
                    <Checkbox checked={value} onChange={onChange} />
                  )}
                />
                <Typography variant="body2">Zorunlu Katılımcı</Typography>
              </Box>

              {editingId && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Controller
                    name="attended"
                    control={control}
                    render={({ field: { onChange, value } }) => (
                      <Checkbox checked={value} onChange={onChange} />
                    )}
                  />
                  <Typography variant="body2">Toplantıya Katıldı</Typography>
                </Box>
              )}
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={handleClose} disabled={addMutation.isPending || updateMutation.isPending}>
              İptal
            </Button>
            <Button type="submit" variant="contained" disabled={addMutation.isPending || updateMutation.isPending}>
              Kaydet
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
};
