import React from 'react';
import { Box, Typography, Paper, Grid, Card, CardContent } from '@mui/material';

export const Dashboard = () => {
  return (
    <Box>
      <Typography variant="h4" color="primary.main" sx={{ fontWeight: 600, mb: 3 }}>
        Dashboard
      </Typography>
      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Toplam Toplantı
              </Typography>
              <Typography variant="h3" component="div">
                0
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Açık Aksiyonlarım
              </Typography>
              <Typography variant="h3" component="div" color="warning.main">
                0
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Bekleyen Onaylar
              </Typography>
              <Typography variant="h3" component="div" color="info.main">
                0
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};
