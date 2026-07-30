import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Box, Typography, Button, Paper, TextField, Grid, CircularProgress 
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import dayjs from 'dayjs';

import { meetingsApi } from '../api/meetingsApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { ApiError } from '../../../api/apiError';

const createMeetingSchema = z.object({
  title: z.string().min(1, 'Başlık alanı zorunludur').max(200, 'Başlık en fazla 200 karakter olabilir'),
  meetingDate: z.string().min(1, 'Tarih alanı zorunludur'),
  startTime: z.string().min(1, 'Başlangıç saati zorunludur').regex(/^([0-1][0-9]|2[0-3]):[0-5][0-9]$/, 'Geçerli bir saat giriniz (HH:mm)'),
  endTime: z.string().min(1, 'Bitiş saati zorunludur').regex(/^([0-1][0-9]|2[0-3]):[0-5][0-9]$/, 'Geçerli bir saat giriniz (HH:mm)'),
  location: z.string().max(200, 'Konum en fazla 200 karakter olabilir').optional(),
  description: z.string().max(1000, 'Açıklama en fazla 1000 karakter olabilir').optional(),
}).refine((data) => {
  const start = data.startTime.split(':');
  const end = data.endTime.split(':');
  const startMins = parseInt(start[0]) * 60 + parseInt(start[1]);
  const endMins = parseInt(end[0]) * 60 + parseInt(end[1]);
  return endMins > startMins;
}, {
  message: "Bitiş saati, başlangıç saatinden sonra olmalıdır",
  path: ["endTime"]
});

type CreateMeetingInputs = z.infer<typeof createMeetingSchema>;

export const MeetingCreate = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();
  
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<CreateMeetingInputs>({
    resolver: zodResolver(createMeetingSchema),
    defaultValues: {
      meetingDate: dayjs().format('YYYY-MM-DD'),
      startTime: dayjs().format('HH:00'),
      endTime: dayjs().add(1, 'hour').format('HH:00'),
    }
  });

  const mutation = useMutation({
    mutationFn: meetingsApi.createMeeting,
    onSuccess: (data) => {
      showNotification('Toplantı başarıyla oluşturuldu.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.all() });
      navigate(`/meetings/${data.id}`);
    },
    onError: (error: ApiError) => {
      showNotification(error.message, 'error');
    }
  });

  const onSubmit = (data: CreateMeetingInputs) => {
    // Backend expects timespan like HH:mm:ss
    const payload = {
      ...data,
      startTime: `${data.startTime}:00`,
      endTime: `${data.endTime}:00`
    };
    mutation.mutate(payload);
  };

  return (
    <Box sx={{ maxWidth: 'md', mx: 'auto' }}>
      <Typography variant="h4" color="primary.main" sx={{ fontWeight: 600, mb: 3 }}>
        Yeni Toplantı Oluştur
      </Typography>

      <Paper sx={{ p: 4 }}>
        <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <Grid container spacing={3}>
            <Grid size={{ xs: 12 }}>
              <TextField
                fullWidth
                label="Toplantı Başlığı"
                {...register('title')}
                error={!!errors.title}
                helperText={errors.title?.message}
                autoFocus
              />
            </Grid>
            
            <Grid size={{ xs: 12, sm: 4 }}>
              <TextField
                fullWidth
                label="Tarih"
                type="date"
                slotProps={{ inputLabel: { shrink: true } }}
                {...register('meetingDate')}
                error={!!errors.meetingDate}
                helperText={errors.meetingDate?.message}
              />
            </Grid>
            
            <Grid size={{ xs: 12, sm: 4 }}>
              <TextField
                fullWidth
                label="Başlangıç Saati"
                type="time"
                slotProps={{ inputLabel: { shrink: true } }}
                {...register('startTime')}
                error={!!errors.startTime}
                helperText={errors.startTime?.message}
              />
            </Grid>
            
            <Grid size={{ xs: 12, sm: 4 }}>
              <TextField
                fullWidth
                label="Bitiş Saati"
                type="time"
                slotProps={{ inputLabel: { shrink: true } }}
                {...register('endTime')}
                error={!!errors.endTime}
                helperText={errors.endTime?.message}
              />
            </Grid>

            <Grid size={{ xs: 12 }}>
              <TextField
                fullWidth
                label="Konum / Link"
                {...register('location')}
                error={!!errors.location}
                helperText={errors.location?.message}
                placeholder="Örn: Toplantı Odası 1 veya Teams Linki"
              />
            </Grid>
            
            <Grid size={{ xs: 12 }}>
              <TextField
                fullWidth
                label="Açıklama"
                multiline
                rows={4}
                {...register('description')}
                error={!!errors.description}
                helperText={errors.description?.message}
              />
            </Grid>
            
            <Grid size={{ xs: 12 }} sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2, mt: 2 }}>
              <Button variant="outlined" onClick={() => navigate('/meetings')} disabled={isSubmitting || mutation.isPending}>
                İptal
              </Button>
              <Button type="submit" variant="contained" disabled={isSubmitting || mutation.isPending}>
                {mutation.isPending ? <CircularProgress size={24} color="inherit" /> : 'Kaydet'}
              </Button>
            </Grid>
          </Grid>
        </Box>
      </Paper>
    </Box>
  );
};
