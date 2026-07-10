import { api } from '../lib/axios';

export const authService = {
  login: (data: any) => api.post('/Auth/login', data).then(res => res.data),
  getMe: () => api.get('/Auth/me').then(res => res.data),
};

export const employeeService = {
  getAll: (params?: any) => api.get('/Employees', { params }).then(res => res.data),
  getById: (id: string) => api.get(`/Employees/${id}`).then(res => res.data),
  create: (data: any) => api.post('/Employees', data).then(res => res.data),
  update: (id: string, data: any) => api.put(`/Employees/${id}`, data).then(res => res.data),
  delete: (id: string) => api.delete(`/Employees/${id}`).then(res => res.data),
  changePassword: (id: string, data: any) => api.put(`/Employees/${id}/password`, data).then(res => res.data),
  getSubordinates: (id: string) => api.get(`/Employees/${id}/subordinates`).then(res => res.data),
  getDocuments: (id: string) => api.get(`/Employees/${id}/documents`).then(res => res.data),
  uploadDocument: (id: string, formData: FormData, onUploadProgress?: (progressEvent: any) => void) => api.post(`/Employees/${id}/documents`, formData, { headers: { 'Content-Type': 'multipart/form-data' }, onUploadProgress }).then(res => res.data),
  downloadDocument: (id: string, documentId: string) => api.get(`/Employees/${id}/documents/${documentId}/download`, { responseType: 'blob' }).then(res => res.data),
  deleteDocument: (id: string, documentId: string) => api.delete(`/Employees/${id}/documents/${documentId}`).then(res => res.data),
  getAuditLogs: (id: string, page: number = 1, pageSize: number = 10) => api.get(`/Employees/${id}/audit-log?page=${page}&pageSize=${pageSize}`).then(res => res.data)
};

export const leaveService = {
  getTypes: (employeeId?: string) => api.get('/leave-types', { params: { employeeId } }).then(res => res.data),
  getRequests: (params?: any) => api.get('/leave-requests', { params }).then(res => res.data),
  create: (data: any) => api.post('/leave-requests', data).then(res => res.data),
  approve: (id: string) => api.put(`/leave-requests/${id}/approve`).then(res => res.data),
  reject: ({ id, reason }: { id: string, reason: string }) => api.put(`/leave-requests/${id}/reject`, { reason }).then(res => res.data),
  getBalances: (employeeId: string) => api.get(`/leave-balances/${employeeId}`).then(res => res.data)
};

export const attendanceService = {
  clockIn: (data: any) => api.post('/attendances/clock-in', data).then(res => res.data),
  clockOut: (data: any) => api.post('/attendances/clock-out', data).then(res => res.data),
  getAttendances: (params?: any) => api.get('/attendances', { params }).then(res => res.data),
  getSummary: (employeeId: string, month: number, year: number) => api.get(`/attendances/summary/${employeeId}?month=${month}&year=${year}`).then(res => res.data),
  export: (params?: any) => api.get('/attendances/export', { params, responseType: 'blob' }).then(res => res.data)
};

export const departmentService = {
  getAll: () => api.get('/departments').then(res => res.data),
  create: (data: any) => api.post('/departments', data).then(res => res.data),
  delete: (id: string) => api.delete(`/departments/${id}`).then(res => res.data)
};

export const positionService = {
  getAll: () => api.get('/positions').then(res => res.data),
  create: (data: any) => api.post('/positions', data).then(res => res.data),
  delete: (id: string) => api.delete(`/positions/${id}`).then(res => res.data)
};

export const roleService = {
  getRoles: () => api.get('/roles').then(res => res.data),
  createRole: (data: any) => api.post('/roles', data).then(res => res.data),
  getPermissions: () => api.get('/roles/permissions').then(res => res.data),
  getRolePermissions: (roleId: string) => api.get(`/roles/${roleId}/permissions`).then(res => res.data),
  assignPermissions: (roleId: string, data: any) => api.post(`/roles/${roleId}/permissions`, data).then(res => res.data)
};
