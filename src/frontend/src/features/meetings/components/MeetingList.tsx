import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  Box, Typography, Button, Paper, Table, TableBody, TableCell, 
  TableContainer, TableHead, TableRow, TablePagination, Chip,
  TextField, IconButton, InputAdornment, CircularProgress
} from '@mui/material';
import { Add as AddIcon, Search as SearchIcon, Visibility as ViewIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import dayjs from 'dayjs';

import { meetingsApi } from '../api/meetingsApi';
import { queryKeys } from '../../../api/queryKeys';
import { MeetingStatus, MeetingFilterDto } from '../types/meeting';

export const MeetingList = () => {
  const navigate = useNavigate();
  
  const [filter, setFilter] = useState<MeetingFilterDto>({
    pageNumber: 1,
    pageSize: 10,
    search: '',
  });

  const [searchInput, setSearchInput] = useState('');

  const { data, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings.list(filter),
    queryFn: () => meetingsApi.getMeetings(filter),
  });

  const handlePageChange = (event: unknown, newPage: number) => {
    setFilter({ ...filter, pageNumber: newPage + 1 });
  };

  const handleRowsPerPageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setFilter({ ...filter, pageSize: parseInt(event.target.value, 10), pageNumber: 1 });
  };

  const handleSearch = () => {
    setFilter({ ...filter, search: searchInput, pageNumber: 1 });
  };

  const getStatusColor = (status: MeetingStatus) => {
    switch(status) {
      case MeetingStatus.Draft: return 'default';
      case MeetingStatus.ReadyForAnalysis: return 'info';
      case MeetingStatus.Analyzing: return 'warning';
      case MeetingStatus.WaitingForApproval: return 'warning';
      case MeetingStatus.Approved: return 'success';
      case MeetingStatus.EmailSent: return 'success';
      case MeetingStatus.Archived: return 'default';
      case MeetingStatus.Failed: return 'error';
      default: return 'default';
    }
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" color="primary.main" sx={{ fontWeight: 600 }}>
          Toplantılar
        </Typography>
        <Button 
          variant="contained" 
          startIcon={<AddIcon />} 
          onClick={() => navigate('/meetings/new')}
        >
          Yeni Toplantı
        </Button>
      </Box>

      <Paper sx={{ p: 2, mb: 3 }}>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <TextField
            size="small"
            placeholder="Toplantı başlığı ara..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon />
                  </InputAdornment>
                ),
              }
            }}
            sx={{ width: 300 }}
          />
          <Button variant="outlined" onClick={handleSearch}>Ara</Button>
        </Box>
      </Paper>

      <TableContainer component={Paper}>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', p: 5 }}>
            <CircularProgress />
          </Box>
        ) : isError ? (
          <Box sx={{ p: 3 }}>
            <Typography color="error">Toplantılar yüklenirken bir hata oluştu.</Typography>
          </Box>
        ) : (
          <>
            <Table>
              <TableHead sx={{ bgcolor: 'rgba(0,0,0,0.02)' }}>
                <TableRow>
                  <TableCell><strong>Başlık</strong></TableCell>
                  <TableCell><strong>Tarih</strong></TableCell>
                  <TableCell><strong>Saat</strong></TableCell>
                  <TableCell><strong>Durum</strong></TableCell>
                  <TableCell align="right"><strong>İşlemler</strong></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data?.items.map((meeting) => (
                  <TableRow key={meeting.id} hover>
                    <TableCell>{meeting.title}</TableCell>
                    <TableCell>{dayjs(meeting.meetingDate).format('DD.MM.YYYY')}</TableCell>
                    <TableCell>
                      {meeting.startTime.substring(0, 5)} - {meeting.endTime.substring(0, 5)}
                    </TableCell>
                    <TableCell>
                      <Chip 
                        label={meeting.statusDisplayName} 
                        color={getStatusColor(meeting.status)} 
                        size="small" 
                        variant="outlined"
                      />
                    </TableCell>
                    <TableCell align="right">
                      <IconButton 
                        color="primary" 
                        onClick={() => navigate(`/meetings/${meeting.id}`)}
                        title="Görüntüle"
                      >
                        <ViewIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
                {data?.items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5} align="center" sx={{ py: 3 }}>
                      Kayıt bulunamadı.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
            <TablePagination
              component="div"
              count={data?.totalCount || 0}
              page={(filter.pageNumber || 1) - 1}
              onPageChange={handlePageChange}
              rowsPerPage={filter.pageSize || 10}
              onRowsPerPageChange={handleRowsPerPageChange}
              labelRowsPerPage="Sayfa başına kayıt:"
              labelDisplayedRows={({ from, to, count }) => `${count} kayıttan ${from}-${to} arası`}
            />
          </>
        )}
      </TableContainer>
    </Box>
  );
};
