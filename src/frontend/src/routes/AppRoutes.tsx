import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { AppShell } from '../layouts/AppShell';
import { AuthLayout } from '../layouts/AuthLayout';
import { NotFound } from '../pages/NotFound';
import { Login } from '../features/auth/components/Login';
import { Dashboard } from '../pages/Dashboard';
import { MeetingList } from '../features/meetings/components/MeetingList';
import { MeetingCreate } from '../features/meetings/components/MeetingCreate';
import { MeetingDetail } from '../features/meetings/components/MeetingDetail';

import { MyActionsPage } from '../pages/MyActionsPage';
import { LiveMeetingRoom } from '../pages/LiveMeetingRoom';

export const AppRoutes = () => {
  return (
    <Routes>
      {/* Public Routes */}
      <Route element={<AuthLayout />}>
        <Route path="/login" element={<Login />} />
        {/* Register route can go here if needed */}
      </Route>

      {/* Protected Routes */}
      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<Dashboard />} />
          
          <Route path="/meetings" element={<MeetingList />} />
          <Route path="/meetings/new" element={<MeetingCreate />} />
          <Route path="/meetings/live" element={<LiveMeetingRoom />} />
          <Route path="/meetings/:id" element={<MeetingDetail />} />
          <Route path="/my-actions" element={<MyActionsPage />} />
          
        </Route>
      </Route>

      {/* Fallback */}
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
};
