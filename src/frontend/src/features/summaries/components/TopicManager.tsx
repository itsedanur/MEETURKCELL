import React, { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Box, Typography, Button, TextField, IconButton, Dialog, DialogTitle, DialogContent, DialogActions } from '@mui/material';
import { Add as AddIcon, Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { useForm } from 'react-hook-form';

import { summariesApi } from '../api/summariesApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { MeetingTopicDto, CreateMeetingTopicRequest, UpdateMeetingTopicRequest } from '../types/summary';

interface Props {
  meetingId: string;
  summaryId: string;
  topics: MeetingTopicDto[];
  canEdit: boolean;
}

export const TopicManager: React.FC<Props> = ({ meetingId, summaryId, topics, canEdit }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();
  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  const { register, handleSubmit, reset } = useForm<CreateMeetingTopicRequest>();

  const addMutation = useMutation({
    mutationFn: (data: CreateMeetingTopicRequest) => summariesApi.addTopic(meetingId, summaryId, data),
    onSuccess: () => {
      showNotification('Konu eklendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      handleClose();
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string, data: UpdateMeetingTopicRequest }) => summariesApi.updateTopic(meetingId, summaryId, id, data),
    onSuccess: () => {
      showNotification('Konu güncellendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      handleClose();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => summariesApi.deleteTopic(meetingId, summaryId, id),
    onSuccess: () => {
      showNotification('Konu silindi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
    }
  });

  const handleOpenNew = () => {
    reset({ title: '', description: '', sortOrder: topics.length + 1 });
    setEditingId(null);
    setOpen(true);
  };

  const handleOpenEdit = (topic: MeetingTopicDto) => {
    reset({ title: topic.title, description: topic.description || '', sortOrder: topic.sortOrder });
    setEditingId(topic.id);
    setOpen(true);
  };

  const handleClose = () => setOpen(false);

  const onSubmit = (data: CreateMeetingTopicRequest) => {
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
          Yeni Konu Ekle
        </Button>
      )}

      <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)}>
          <DialogTitle>{editingId ? 'Konu Düzenle' : 'Yeni Konu Ekle'}</DialogTitle>
          <DialogContent dividers>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
              <TextField label="Başlık" fullWidth required {...register('title')} />
              <TextField label="Açıklama" fullWidth multiline rows={3} {...register('description')} />
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
