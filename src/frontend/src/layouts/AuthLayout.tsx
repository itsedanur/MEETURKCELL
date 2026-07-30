import React from 'react';
import { Box, CssBaseline, Container, Paper, Typography } from '@mui/material';
import { Outlet } from 'react-router-dom';

export const AuthLayout = () => {
  return (
    <Box 
      sx={{ 
        minHeight: '100vh', 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'center',
        bgcolor: 'primary.dark'
      }}
    >
      <CssBaseline />
      <Container maxWidth="sm">
        <Box sx={{ textAlign: 'center', mb: 4 }}>
          <Typography variant="h3" color="white" sx={{ fontWeight: 700 }}>
            Turkcell Toplantı Asistanı
          </Typography>
        </Box>
        <Paper elevation={6} sx={{ p: 4, borderRadius: 3 }}>
          <Outlet />
        </Paper>
      </Container>
    </Box>
  );
};
