import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'sonner';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Layout } from './components/Layout';
import { Login } from './pages/Login';

import { Dashboard } from './pages/Dashboard';
import { EmployeeList } from './pages/EmployeeList';
import { Attendance } from './pages/Attendance';
import { LeaveRequest } from './pages/LeaveRequest';
import { DailyReportList } from './pages/DailyReportList';

import { Settings } from './pages/Settings';
import { EmployeeDetail } from './pages/EmployeeDetail';

const queryClient = new QueryClient();

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Login />} />
          <Route element={<ProtectedRoute><Layout /></ProtectedRoute>}>
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/employees" element={<EmployeeList />} />
            <Route path="/employees/:id" element={<EmployeeDetail />} />
            <Route path="/leaves" element={<LeaveRequest />} />
            <Route path="/attendance" element={<Attendance />} />
            <Route path="/daily-reports" element={<DailyReportList />} />
            <Route path="/settings" element={<Settings />} />
          </Route>
        </Routes>
      </BrowserRouter>
      <Toaster position="top-right" richColors />
    </QueryClientProvider>
  );
}
