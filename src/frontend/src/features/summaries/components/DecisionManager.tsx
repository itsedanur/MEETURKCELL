import React, { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Box, Typography, Button, TextField, IconButton, Dialog, DialogTitle, DialogContent, DialogActions, MenuItem } from '@mui/material';
import { Add as AddIcon, Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';

import { summariesApi } from '../api/summariesApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { MeetingDecisionDto, CreateMeetingDecisionRequest, UpdateMeetingDecisionRequest, MeetingTopicDto } from '../types/summary';

interface Props {
  meetingId: string;
  summaryId: string;
  decisions: MeetingDecisionDto[];
  topics: MeetingTopicDto[];
  canEdit: boolean;
}

export const DecisionManager: React.FC<Props> = ({ meetingId, summaryId, decisions, topics, canEdit }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();
  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  const { register, handleSubmit, reset, control } = useForm<CreateMeetingDecisionRequest>();

  const addMutation = useMutation({
    mutationFn: (data: CreateMeetingDecisionRequest) => summariesApi.addDecision(meetingId, summaryId, data),
    onSuccess: () => {
      showNotification('Karar eklendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      handleClose();
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string, data: UpdateMeetingDecisionRequest }) => summariesApi.updateDecision(meetingId, summaryId, id, data),
    onSuccess: () => {
      showNotification('Karar güncellendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      handleClose();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => summariesApi.deleteDecision(meetingId, summaryId, id),
    onSuccess: () => {
      showNotification('Karar silindi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
    }
  });

  const handleOpenNew = () => {
    reset({ description: '', relatedTopic: '', confidenceScore: 1.0, sortOrder: decisions.length + 1, evidence: '' });
    setEditingId(null);
    setOpen(true);
  };

  const handleOpenEdit = (decision: MeetingDecisionDto) => {
    reset({ 
      description: decision.description, 
      relatedTopic: decision.relatedTopic || '', 
      confidenceScore: decision.confidenceScore, 
      sortOrder: decision.sortOrder,
      evidence: decision.evidence || ''
    });
    setEditingId(decision.id);
    setOpen(true);
  };

  const handleClose = () => setOpen(false);

  const onSubmit = (data: CreateMeetingDecisionRequest) => {
    if (editingId) {
      updateMutation.mutate({ id: editingId, data });
    } else {
      addMutation.mutate(data);
    }
  };

  return (
    <>
      {canEdit && (
        <Button startIcon={<AddIcon />} size="small" onClick={handleOpenNew} sx={{ mb: 2 }}>
          Yeni Karar Ekle
        </Button>
      )}

      <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)}>
          <DialogTitle>{editingId ? 'Karar Düzenle' : 'Yeni Karar Ekle'}</DialogTitle>
          <DialogContent dividers>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
              <TextField label="Açıklama" fullWidth required multiline rows={2} {...register('description')} />
              <Controller
                name="relatedTopic"
                control={control}
                render={({ field }) => (
                  <TextField select label="İlgili Konu" {...field} fullWidth value={field.value || ''}>
                    <MenuItem value="">-- Konu Bağımsız --</MenuItem>
                    {topics.map(t => (
                      <MenuItem key={t.id} value={t.title}>{t.title}</MenuItem>
                    ))}
                  </TextField>
                )}
              />
              <TextField label="Sıra" type="number" fullWidth {...register('sortOrder', { valueAsNumber: true })} />
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={handleClose}>İptal</Button>
            <Button type="submit" variant="contained">Kaydet</Button>
          </DialogActions>
        </form>
      </Dialog>
    </>
  );
};
