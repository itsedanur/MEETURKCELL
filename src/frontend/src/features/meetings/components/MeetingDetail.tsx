import React, { useState } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { 
  Box, Typography, Button, Paper, Tabs, Tab, CircularProgress, Chip, Grid
} from '@mui/material';
import dayjs from 'dayjs';

import { meetingsApi } from '../api/meetingsApi';
import { queryKeys } from '../../../api/queryKeys';
import { MeetingStatus } from '../types/meeting';

import { analysisApi } from '../../analysis/api/analysisApi';

import { ParticipantList } from '../../participants/components/ParticipantList';
import { TranscriptManager } from '../../transcripts/components/TranscriptManager';
import { AnalysisManager } from '../../analysis/components/AnalysisManager';
import { SummaryManager } from '../../summaries/components/SummaryManager';
import { ActionManager } from '../../actions/components/ActionManager';
import { ApprovalPanel } from '../../summaries/components/ApprovalPanel';
import { EmailManager } from '../../emails/components/EmailManager';
import { RecordingManager } from '../../recordings/components/RecordingManager';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function CustomTabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`meeting-tabpanel-${index}`}
      aria-labelledby={`meeting-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ p: 3 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

export const MeetingDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  
  const tabParam = searchParams.get('tab');
  const [tabValue, setTabValue] = useState(
    tabParam === 'participants' ? 1 :
    tabParam === 'recordings' ? 2 :
    tabParam === 'transcript' ? 3 :
    tabParam === 'analysis' ? 4 :
    tabParam === 'summary' ? 5 :
    tabParam === 'actions' ? 6 :
    tabParam === 'email' ? 7 : 0
  );

  const { data: meeting, isLoading, isError } = useQuery({
    queryKey: queryKeys.meetings.detail(id!),
    queryFn: () => meetingsApi.getMeeting(id!),
    enabled: !!id
  });

  const { data: summary } = useQuery({
    queryKey: queryKeys.meetings.summary(id!),
    queryFn: () => analysisApi.getSummary(id!),
    enabled: !!id && !!meeting && meeting.status >= MeetingStatus.WaitingForApproval
  });

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
    const tabNames = ['general', 'participants', 'recordings', 'transcript', 'analysis', 'summary', 'actions', 'email'];
    setSearchParams({ tab: tabNames[newValue] });
  };

  if (isLoading) {
    return <Box sx={{ display: 'flex', justifyContent: 'center', p: 5 }}><CircularProgress /></Box>;
  }

  if (isError || !meeting) {
    return <Typography color="error">Toplantı bilgileri yüklenemedi.</Typography>;
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 3 }}>
        <Box>
          <Typography variant="h4" color="primary.main" sx={{ fontWeight: 600, mb: 1 }}>
            {meeting.title}
          </Typography>
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
            <Chip label={meeting.statusDisplayName} color="primary" variant="outlined" />
            <Typography variant="body2" color="text.secondary">
              {dayjs(meeting.meetingDate).format('DD.MM.YYYY')} | {meeting.startTime.substring(0, 5)} - {meeting.endTime.substring(0, 5)}
            </Typography>
          </Box>
        </Box>
        <Button variant="outlined" onClick={() => navigate('/meetings')}>
          Listeye Dön
        </Button>
      </Box>

      <Paper sx={{ width: '100%', mb: 2 }}>
        {summary && (
          <Box sx={{ p: 2, pb: 0 }}>
            <ApprovalPanel meetingId={id!} meetingStatus={meeting.status} summary={summary} />
          </Box>
        )}
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={tabValue} onChange={handleTabChange} variant="scrollable" scrollButtons="auto">
            <Tab label="Genel Bilgiler" />
            <Tab label="Katılımcılar" />
            <Tab label="Ses Kayıtları" />
            <Tab label="Transcript" />
            <Tab label="Analiz" />
            <Tab label="Özet ve Kararlar" />
            <Tab label="Aksiyonlar" />
            <Tab label="E-posta" />
          </Tabs>
        </Box>
        
        <CustomTabPanel value={tabValue} index={0}>
          <Grid container spacing={3}>
            <Grid size={{ xs: 12, md: 6 }}>
              <Typography variant="subtitle2" color="text.secondary">Başlık</Typography>
              <Typography variant="body1" sx={{ mb: 2 }}>{meeting.title}</Typography>
              
              <Typography variant="subtitle2" color="text.secondary">Tarih</Typography>
              <Typography variant="body1" sx={{ mb: 2 }}>{dayjs(meeting.meetingDate).format('DD.MM.YYYY')}</Typography>
              
              <Typography variant="subtitle2" color="text.secondary">Saat</Typography>
              <Typography variant="body1" sx={{ mb: 2 }}>{meeting.startTime.substring(0, 5)} - {meeting.endTime.substring(0, 5)}</Typography>
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <Typography variant="subtitle2" color="text.secondary">Konum/Link</Typography>
              <Typography variant="body1" sx={{ mb: 2 }}>{meeting.location || '-'}</Typography>
              
              <Typography variant="subtitle2" color="text.secondary">Açıklama</Typography>
              <Typography variant="body1" sx={{ mb: 2 }}>{meeting.description || '-'}</Typography>
            </Grid>
          </Grid>
        </CustomTabPanel>
        
        <CustomTabPanel value={tabValue} index={1}>
          <ParticipantList meetingId={id!} meetingStatus={meeting.status} />
        </CustomTabPanel>

        <CustomTabPanel value={tabValue} index={2}>
          <RecordingManager meetingId={id!} isReadOnly={meeting.status >= MeetingStatus.WaitingForApproval} />
        </CustomTabPanel>
        
        <CustomTabPanel value={tabValue} index={3}>
          <TranscriptManager meetingId={id!} meetingStatus={meeting.status} />
        </CustomTabPanel>

        <CustomTabPanel value={tabValue} index={4}>
          <AnalysisManager meetingId={id!} meetingStatus={meeting.status} />
        </CustomTabPanel>

        <CustomTabPanel value={tabValue} index={5}>
          <SummaryManager meetingId={id!} meetingStatus={meeting.status} />
        </CustomTabPanel>

        <CustomTabPanel value={tabValue} index={6}>
          <ActionManager meetingId={id!} meetingStatus={meeting.status} />
        </CustomTabPanel>

        <CustomTabPanel value={tabValue} index={7}>
          <EmailManager meetingId={id!} />
        </CustomTabPanel>
      </Paper>
    </Box>
  );
};
