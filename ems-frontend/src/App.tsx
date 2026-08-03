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
import { ShiftSchedules } from './pages/ShiftSchedules';
import { TakeTest } from './pages/TakeTest';
import { TestResult } from './pages/TestResult';
import { CandidateList } from './pages/CandidateList';
import { CandidateDetail } from './pages/CandidateDetail';
import { Careers } from './pages/Careers';
import { CareerApply } from './pages/CareerApply';
import { JobOpenings } from './pages/JobOpenings';


const queryClient = new QueryClient();

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Login />} />
          <Route path="/careers" element={<Careers />} />
          <Route path="/careers/apply/:jobOpeningId" element={<CareerApply />} />
          <Route path="/take-test/:testId/:sessionId" element={<TakeTest />} />
          <Route element={<ProtectedRoute><Layout /></ProtectedRoute>}>
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/employees" element={<EmployeeList />} />
            <Route path="/employees/:id" element={<EmployeeDetail />} />
            <Route path="/leaves" element={<LeaveRequest />} />
            <Route path="/attendance" element={<Attendance />} />
            <Route path="/shift-schedules" element={<ShiftSchedules />} />
            <Route path="/daily-reports" element={<DailyReportList />} />
            <Route path="/settings" element={<Settings />} />
            <Route path="/test-results/:sessionId" element={<TestResult />} />
            <Route path="/assessments" element={
              <ProtectedRoute allowedRoles={['Admin', 'Manager', 'HR']}>
                <div className="p-8 text-center text-slate-500">Assessments management coming soon</div>
              </ProtectedRoute>
            } />
            
            {/* Recruitment Module */}
            <Route path="/candidates" element={
              <ProtectedRoute allowedRoles={['Admin', 'Manager', 'HR']}>
                <CandidateList />
              </ProtectedRoute>
            } />
            <Route path="/candidates/:id" element={
              <ProtectedRoute allowedRoles={['Admin', 'Manager', 'HR']}>
                <CandidateDetail />
              </ProtectedRoute>
            } />
            <Route path="/job-openings" element={
              <ProtectedRoute allowedRoles={['Admin', 'Manager', 'HR']}>
                <JobOpenings />
              </ProtectedRoute>
            } />
          </Route>

        </Routes>
      </BrowserRouter>
      <Toaster position="top-right" richColors />
    </QueryClientProvider>
  );
}
