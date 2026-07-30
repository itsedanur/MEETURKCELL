import React, { useState } from 'react';
import { 
  Dialog, DialogTitle, DialogContent, DialogActions, Button, 
  Tabs, Tab, Box, Typography, Paper
} from '@mui/material';
import { EmailPreviewDto } from '../types/email';

interface Props {
  open: boolean;
  onClose: () => void;
  preview: EmailPreviewDto;
}

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
      id={`preview-tabpanel-${index}`}
      aria-labelledby={`preview-tab-${index}`}
      {...other}
      style={{ flex: 1, display: 'flex', flexDirection: 'column' }}
    >
      {value === index && (
        <Box sx={{ p: 2, flex: 1, display: 'flex', flexDirection: 'column' }}>
          {children}
        </Box>
      )}
    </div>
  );
}

export const EmailPreviewDialog: React.FC<Props> = ({ open, onClose, preview }) => {
  const [tabValue, setTabValue] = useState(0);

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth sx={{ '& .MuiDialog-paper': { height: '80vh' } }}>
      <DialogTitle>E-posta Ön İzleme</DialogTitle>
      
      <Box sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
        <Tabs value={tabValue} onChange={handleTabChange}>
          <Tab label="HTML Görünüm" />
          <Tab label="Düz Metin (Plain Text)" />
        </Tabs>
      </Box>

      <DialogContent dividers sx={{ display: 'flex', flexDirection: 'column', p: 0 }}>
        <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider', bgcolor: 'background.default' }}>
          <Typography variant="subtitle2" color="text.secondary">Konu (Subject):</Typography>
          <Typography variant="body1" sx={{ fontWeight: 600 }}>{preview.subject}</Typography>
        </Box>

        <CustomTabPanel value={tabValue} index={0}>
          {preview.htmlBody ? (
            <Paper variant="outlined" sx={{ flex: 1, overflow: 'hidden' }}>
              <iframe
                title="HTML Preview"
                srcDoc={preview.htmlBody}
                sandbox="allow-same-origin"
                style={{ width: '100%', height: '100%', border: 'none' }}
              />
            </Paper>
          ) : (
            <Typography color="text.secondary">HTML içerik bulunamadı.</Typography>
          )}
        </CustomTabPanel>

        <CustomTabPanel value={tabValue} index={1}>
          {preview.textBody ? (
            <Paper variant="outlined" sx={{ flex: 1, overflow: 'auto', p: 2, bgcolor: 'background.default' }}>
              <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', fontFamily: 'monospace' }}>
                {preview.textBody}
              </Typography>
            </Paper>
          ) : (
            <Typography color="text.secondary">Düz metin içerik bulunamadı.</Typography>
          )}
        </CustomTabPanel>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose} variant="contained">Kapat</Button>
      </DialogActions>
    </Dialog>
  );
};
