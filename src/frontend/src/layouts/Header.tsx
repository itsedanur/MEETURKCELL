import React from 'react';
import { AppBar, Toolbar, Typography, Button, Box } from '@mui/material';
import { useAuth } from '../contexts/AuthContext';

export const Header = () => {
  const { user, logout } = useAuth();

  return (
    <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
      <Toolbar>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1, fontWeight: 700 }}>
          Toplantı Asistanı
        </Typography>
        {user && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Typography variant="body2">
              {user.firstName} {user.lastName}
            </Typography>
            <Button color="inherit" onClick={logout} variant="outlined" sx={{ borderColor: 'rgba(255,255,255,0.5)' }}>
              Çıkış Yap
            </Button>
          </Box>
        )}
      </Toolbar>
    </AppBar>
  );
};
