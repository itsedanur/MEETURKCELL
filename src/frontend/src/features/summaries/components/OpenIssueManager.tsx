import React, { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Box, Typography, Button, TextField, Dialog, DialogTitle, DialogContent, DialogActions } from '@mui/material';
import { Add as AddIcon, Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { useForm } from 'react-hook-form';

import { summariesApi } from '../api/summariesApi';
import { queryKeys } from '../../../api/queryKeys';
import { useNotification } from '../../../contexts/NotificationContext';
import { OpenIssueDto, CreateOpenIssueRequest, UpdateOpenIssueRequest } from '../types/summary';

interface Props {
  meetingId: string;
  summaryId: string;
  openIssues: OpenIssueDto[];
  canEdit: boolean;
}

export const OpenIssueManager: React.FC<Props> = ({ meetingId, summaryId, openIssues, canEdit }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();
  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  const { register, handleSubmit, reset } = useForm<CreateOpenIssueRequest>();

  const addMutation = useMutation({
    mutationFn: (data: CreateOpenIssueRequest) => summariesApi.addOpenIssue(meetingId, summaryId, data),
    onSuccess: () => {
      showNotification('Açık konu eklendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      handleClose();
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string, data: UpdateOpenIssueRequest }) => summariesApi.updateOpenIssue(meetingId, summaryId, id, data),
    onSuccess: () => {
      showNotification('Açık konu güncellendi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
      handleClose();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => summariesApi.deleteOpenIssue(meetingId, summaryId, id),
    onSuccess: () => {
      showNotification('Açık konu silindi.', 'success');
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.summary(meetingId) }); queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(meetingId) });
    }
  });

  const handleOpenNew = () => {
    reset({ description: '', ownerName: '', sortOrder: openIssues.length + 1 });
    setEditingId(null);
    setOpen(true);
  };

  const handleOpenEdit = (issue: OpenIssueDto) => {
    reset({ description: issue.description, ownerName: issue.ownerName || '', sortOrder: issue.sortOrder });
    setEditingId(issue.id);
    setOpen(true);
  };

  const handleClose = () => setOpen(false);

  const onSubmit = (data: CreateOpenIssueRequest) => {
    const payload = {
      ...data,
      ownerName: data.ownerName || undefined
    };
    if (editingId) {
      updateMutation.mutate({ id: editingId, data: payload });
    } else {
      addMutation.mutate(payload);
    }
  };

  return (
    <>
      {canEdit && (
        <Button startIcon={<AddIcon />} size="small" onClick={handleOpenNew} sx={{ mb: 2 }}>
          Yeni Açık Konu Ekle
        </Button>
      )}

      <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)}>
          <DialogTitle>{editingId ? 'Açık Konu Düzenle' : 'Yeni Açık Konu Ekle'}</DialogTitle>
          <DialogContent dividers>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
              <TextField label="Açıklama" fullWidth required multiline rows={2} {...register('description')} />
              <TextField label="Sorumlu Kişi" fullWidth {...register('ownerName')} />
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
