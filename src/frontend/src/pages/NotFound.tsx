import React from 'react';
import { Box, Typography, Button } from '@mui/material';
import { useNavigate } from 'react-router-dom';

export const NotFound = () => {
  const navigate = useNavigate();

  return (
    <Box 
      sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '80vh' }}
    >
      <Typography variant="h1" color="primary" sx={{ fontWeight: 'bold' }}>
        404
      </Typography>
      <Typography variant="h5" color="text.secondary" sx={{ mt: 2, mb: 4 }}>
        Aradığınız sayfa bulunamadı.
      </Typography>
      <Button variant="contained" onClick={() => navigate('/')}>
        Ana Sayfaya Dön
      </Button>
    </Box>
  );
};
