import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Box, Typography, Button, Paper, Table, TableBody, TableCell, TableContainer, 
  TableHead, TableRow, Chip, IconButton, Tooltip, Dialog, DialogTitle, DialogContent, 
  DialogActions, TextField, CircularProgress, MenuItem, Alert
} from '@mui/material';
import { 
  Add as AddIcon, Edit as EditIcon, Delete as DeleteIcon,
  Warning as WarningIcon
} from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import dayjs from 'dayjs';

import { analysisApi } from '../../analysis/api/analysisApi';
import { actionsApi } from '../api/actionsApi';
import { participantsApi } from '../../participants/api/participantsApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { ApiError } from '../../../api/apiError';
import { MeetingStatus } from '../../meetings/types/meeting';
import { ActionItemStatus, ActionPriority } from '../../summaries/types/summary';
import { CreateActionItemRequest, UpdateActionItemRequest, UpdateActionItemStatusRequest } from '../types/action';

const actionSchema = z.object({
  description: z.string().min(1, 'Açıklama zorunludur').max(500),
  assignedParticipantId: z.string().optional().nullable(),
  ownerName: z.string().max(100).optional().nullable(),
  dueDate: z.string().optional().nullable(),
  priority: z.nativeEnum(ActionPriority),
  status: z.nativeEnum(ActionItemStatus),
  cancellationReason: z.string().max(500).optional().nullable()
}).refine(data => {
  if (data.status === ActionItemStatus.Cancelled && !data.cancellationReason?.trim()) {
    return false;
  }
  return true;
}, {
  message: "İptal nedeni zorunludur",
  path: ["cancellationReason"]
});

type ActionInputs = z.infer<typeof actionSchema>;

interface Props {
  meetingId: string;
  meetingStatus: MeetingStatus;
}

export const ActionManager: React.FC<Props> = ({ meetingId, meetingStatus }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();

  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  const canEdit = meetingStatus === MeetingStatus.WaitingForApproval || meetingStatus === MeetingStatus.Approved;

  const { data: summary, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings.summary(meetingId),
    queryFn: () => analysisApi.getSummary(meetingId),
    enabled: meetingStatus >= MeetingStatus.WaitingForApproval
  });

  const { data: participants } = useQuery({
    queryKey: queryKeys.meetings.participants(meetingId),
    queryFn: () => participantsApi.getParticipants(meetingId),
  });

  const { control, register, handleSubmit, reset, watch, formState: { errors } } = useForm<ActionInputs>({
    resolver: zodResolver(actionSchema),
    defaultValues: {
      priority: ActionPriority.Medium,
      status: ActionItemStatus.Open
    }
  });

  const watchedStatus = watch('status');

  const addMutation = useMutation({
    mutationFn: (data: CreateActionItemRequest) => {
      if (!summary) throw new Error("Summary not loaded");
      return actionsApi.addAction(meetingId, summary.summaryId, data);
    },
    onSuccess: () => {
      showNotification('Aksiyon eklendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      handleClose();
    },
    onError: (error: ApiError) => showNotification(error.message, 'error')
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string, data: UpdateActionItemRequest }) => {
      if (!summary) throw new Error("Summary not loaded");
      return actionsApi.updateAction(meetingId, summary.summaryId, id, data);
    },
    onSuccess: () => {
      showNotification('Aksiyon güncellendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      handleClose();
    },
    onError: (error: ApiError) => {
      if (error.statusCode === 409) {
        showNotification('Başka biri bu aksiyonu güncellemiş olabilir.', 'error');
      } else {
        showNotification(error.message, 'error');
      }
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => {
      if (!summary) throw new Error("Summary not loaded");
      return actionsApi.deleteAction(meetingId, summary.summaryId, id);
    },
    onSuccess: () => {
      showNotification('Aksiyon silindi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
    },
    onError: (error: ApiError) => showNotification(error.message, 'error')
  });

  const patchStatusMutation = useMutation({
    mutationFn: ({ id, data }: { id: string, data: UpdateActionItemStatusRequest }) => {
      if (!summary) throw new Error("Summary not loaded");
      return actionsApi.updateActionStatus(meetingId, summary.summaryId, id, data);
    },
    onSuccess: () => {
      showNotification('Aksiyon durumu güncellendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
    },
    onError: (error: ApiError) => {
      if (error.statusCode === 409) {
        showNotification('Başka biri bu aksiyonu güncellemiş olabilir.', 'error');
      } else {
        showNotification(error.message, 'error');
      }
    }
  });

  const handleOpenNew = () => {
    reset({ description: '', ownerName: '', assignedParticipantId: '', dueDate: '', priority: ActionPriority.Medium, status: ActionItemStatus.Open, cancellationReason: '' });
    setEditingId(null);
    setOpen(true);
  };

  const handleOpenEdit = (action: any) => {
    reset({
      description: action.description,
      ownerName: action.ownerName || '',
      assignedParticipantId: action.assignedParticipantId || '',
      dueDate: action.dueDate ? dayjs(action.dueDate).format('YYYY-MM-DD') : '',
      priority: action.priority,
      status: action.status,
      cancellationReason: action.cancellationReason || ''
    });
    setEditingId(action.id);
    setOpen(true);
  };

  const handleClose = () => setOpen(false);

  const onSubmit = (data: ActionInputs) => {
    let finalOwnerName = data.ownerName ?? undefined;
    let finalOwnerEmail = undefined;

    if (data.assignedParticipantId) {
      const p = participants?.find(x => x.id === data.assignedParticipantId);
      if (p) {
        finalOwnerName = p.fullName;
        finalOwnerEmail = p.email;
      }
    }

    const payload = {
      description: data.description,
      assignedParticipantId: data.assignedParticipantId ?? undefined,
      ownerName: finalOwnerName,
      ownerEmail: finalOwnerEmail,
      dueDate: data.dueDate ? dayjs(data.dueDate).toISOString() : undefined,
      priority: data.priority,
      status: data.status,
      cancellationReason: data.status === ActionItemStatus.Cancelled ? (data.cancellationReason ?? undefined) : undefined,
      confidenceScore: 1.0 // Manual updates assume high confidence
    };

    if (editingId) {
      const originalAction = summary?.actionItems.find(x => x.id === editingId);
      
      // If ONLY status changed and cancellation reason (if applicable), we can use PATCH
      if (originalAction && 
          originalAction.description === payload.description &&
          originalAction.assignedParticipantId === payload.assignedParticipantId &&
          originalAction.ownerName === payload.ownerName &&
          (originalAction.dueDate ? dayjs(originalAction.dueDate).toISOString() : undefined) === payload.dueDate &&
          originalAction.priority === payload.priority &&
          originalAction.status !== payload.status) {
        
        patchStatusMutation.mutate({
          id: editingId,
          data: {
            status: payload.status,
            cancellationReason: payload.cancellationReason
          }
        });
        handleClose();
        return;
      }

      updateMutation.mutate({ 
        id: editingId, 
        data: payload
      });
    } else {
      addMutation.mutate(payload);
    }
  };

  if (meetingStatus < MeetingStatus.WaitingForApproval) {
    return (
      <Alert severity="info">
        Aksiyon maddeleri, toplantı analiz edildikten sonra burada görüntülenecektir.
      </Alert>
    );
  }

  if (isLoading) return <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>;
  if (isError || !summary) return <Typography color="error">Aksiyonlar yüklenemedi.</Typography>;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Aksiyon Maddeleri ({summary.actionItems.length})</Typography>
        {canEdit && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={handleOpenNew} size="small">
            Aksiyon Ekle
          </Button>
        )}
      </Box>

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead sx={{ bgcolor: 'rgba(0,0,0,0.02)' }}>
            <TableRow>
              <TableCell>Açıklama</TableCell>
              <TableCell>Sorumlu</TableCell>
              <TableCell>Termin Tarihi</TableCell>
              <TableCell>Öncelik</TableCell>
              <TableCell>Durum</TableCell>
              {canEdit && <TableCell align="right">İşlemler</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {summary.actionItems.map((action) => (
              <TableRow key={action.id} sx={{ bgcolor: action.requiresReview ? 'warning.light' : 'inherit' }}>
                <TableCell>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    {action.description}
                    {action.requiresReview && (
                      <Tooltip title="Düşük Güven Skoru - Lütfen İnceleyin">
                        <WarningIcon color="warning" fontSize="small" />
                      </Tooltip>
                    )}
                  </Box>
                </TableCell>
                <TableCell>{action.ownerName || '-'}</TableCell>
                <TableCell>
                  {action.dueDate ? (
                    <Typography color={action.isOverdue ? 'error.main' : 'inherit'}>
                      {dayjs(action.dueDate).format('DD.MM.YYYY')}
                      {action.isOverdue && ' (Gecikmiş)'}
                    </Typography>
                  ) : '-'}
                </TableCell>
                <TableCell>{action.priorityDisplayName}</TableCell>
                <TableCell>
                  <Chip 
                    label={action.statusDisplayName} 
                    color={
                      action.status === ActionItemStatus.Completed ? 'success' :
                      action.status === ActionItemStatus.InProgress ? 'info' :
                      'default'
                    }
                    size="small"
                  />
                </TableCell>
                {canEdit && (
                  <TableCell align="right">
                    <IconButton size="small" onClick={() => handleOpenEdit(action)} color="primary">
                      <EditIcon fontSize="small" />
                    </IconButton>
                    <IconButton size="small" onClick={() => {
                      if(window.confirm('Emin misiniz?')) deleteMutation.mutate(action.id);
                    }} color="error">
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </TableCell>
                )}
              </TableRow>
            ))}
            {summary.actionItems.length === 0 && (
              <TableRow>
                <TableCell colSpan={canEdit ? 6 : 5} align="center" sx={{ py: 3 }}>
                  Kayıt bulunamadı.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)}>
          <DialogTitle>{editingId ? 'Aksiyon Düzenle' : 'Yeni Aksiyon Ekle'}</DialogTitle>
          <DialogContent dividers>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
              <TextField
                label="Açıklama"
                fullWidth
                multiline
                rows={3}
                {...register('description')}
                error={!!errors.description}
                helperText={errors.description?.message}
              />
              <Controller
                name="assignedParticipantId"
                control={control}
                render={({ field }) => (
                  <TextField select label="Katılımcı Seç (Opsiyonel)" {...field} fullWidth value={field.value || ''}>
                    <MenuItem value="">-- Serbest Giriş --</MenuItem>
                    {participants?.map(p => (
                      <MenuItem key={p.id} value={p.id}>{p.fullName}</MenuItem>
                    ))}
                  </TextField>
                )}
              />
              <TextField
                label="Sorumlu Kişi (Manuel)"
                fullWidth
                {...register('ownerName')}
                error={!!errors.ownerName}
                helperText={errors.ownerName?.message}
                disabled={!!watch('assignedParticipantId')}
              />
              <TextField
                label="Termin Tarihi"
                type="date"
                fullWidth
                slotProps={{ inputLabel: { shrink: true } }}
                {...register('dueDate')}
              />
              <Controller
                name="priority"
                control={control}
                render={({ field }) => (
                  <TextField select label="Öncelik" {...field} fullWidth>
                    <MenuItem value={ActionPriority.Low}>Düşük</MenuItem>
                    <MenuItem value={ActionPriority.Medium}>Orta</MenuItem>
                    <MenuItem value={ActionPriority.High}>Yüksek</MenuItem>
                    <MenuItem value={ActionPriority.Critical}>Kritik</MenuItem>
                  </TextField>
                )}
              />
              <Controller
                name="status"
                control={control}
                render={({ field }) => (
                  <TextField select label="Durum" {...field} fullWidth>
                    <MenuItem value={ActionItemStatus.Open}>Açık</MenuItem>
                    <MenuItem value={ActionItemStatus.InProgress}>Devam Ediyor</MenuItem>
                    <MenuItem value={ActionItemStatus.Completed}>Tamamlandı</MenuItem>
                    <MenuItem value={ActionItemStatus.Cancelled}>İptal Edildi</MenuItem>
                  </TextField>
                )}
              />
              {watchedStatus === ActionItemStatus.Cancelled && (
                <TextField
                  label="İptal Nedeni"
                  fullWidth
                  multiline
                  rows={2}
                  {...register('cancellationReason')}
                  error={!!errors.cancellationReason}
                  helperText={errors.cancellationReason?.message}
                />
              )}
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={handleClose} disabled={addMutation.isPending || updateMutation.isPending || patchStatusMutation.isPending}>
              İptal
            </Button>
            <Button type="submit" variant="contained" disabled={addMutation.isPending || updateMutation.isPending || patchStatusMutation.isPending}>
              Kaydet
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
};
