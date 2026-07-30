import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation } from '@tanstack/react-query';
import { TextField, Button, Box, Typography, Alert, CircularProgress } from '@mui/material';
import { useNavigate, useLocation } from 'react-router-dom';
import { authApi, LoginRequest } from '../api/authApi';
import { useAuth } from '../../../contexts/AuthContext';
import { ApiError } from '../../../api/apiError';

const loginSchema = z.object({
  email: z.string().min(1, 'E-posta alanı zorunludur').email('Geçerli bir e-posta adresi giriniz'),
  password: z.string().min(1, 'Şifre alanı zorunludur'),
});

type LoginFormInputs = z.infer<typeof loginSchema>;

export const Login = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [globalError, setGlobalError] = useState<string | null>(null);

  const { register, handleSubmit, formState: { errors } } = useForm<LoginFormInputs>({
    resolver: zodResolver(loginSchema),
  });

  const loginMutation = useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: (response) => {
      if (response.success && response.data) {
        login(response.data.token, response.data.user);
        const from = location.state?.from?.pathname || '/dashboard';
        navigate(from, { replace: true });
      }
    },
    onError: (error: ApiError) => {
      setGlobalError(error.message);
    }
  });

  const onSubmit = (data: LoginFormInputs) => {
    setGlobalError(null);
    loginMutation.mutate(data);
  };

  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate sx={{ width: '100%' }}>
      <Typography variant="h5" component="h1" sx={{ mb: 1, fontWeight: 'bold', textAlign: 'left' }}>
        Giriş Yap
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3, textAlign: 'left' }}>
        Sisteme erişmek için e-posta ve şifrenizi giriniz.
      </Typography>

      {globalError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {globalError}
        </Alert>
      )}

      {location.search.includes('expired=true') && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          Oturum süreniz doldu, lütfen tekrar giriş yapın.
        </Alert>
      )}

      <TextField
        fullWidth
        id="email"
        label="E-posta Adresi"
        autoComplete="email"
        {...register('email')}
        error={!!errors.email}
        helperText={errors.email?.message}
        margin="normal"
        autoFocus
      />

      <TextField
        fullWidth
        id="password"
        label="Şifre"
        type="password"
        autoComplete="current-password"
        {...register('password')}
        error={!!errors.password}
        helperText={errors.password?.message}
        margin="normal"
      />

      <Button
        type="submit"
        fullWidth
        variant="contained"
        size="large"
        disabled={loginMutation.isPending}
        sx={{ mt: 3, mb: 2, height: 48 }}
      >
        {loginMutation.isPending ? <CircularProgress size={24} color="inherit" /> : 'Giriş Yap'}
      </Button>
    </Box>
  );
};
