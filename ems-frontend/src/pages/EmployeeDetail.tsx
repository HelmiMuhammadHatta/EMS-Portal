import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { employeeService } from '../services/apiService';
import { toast } from 'sonner';
import { useState } from 'react';
import { ArrowLeft, User, Briefcase, Building2, Calendar, FileText, Clock, Upload, Trash2, Download, Users } from 'lucide-react';

export const EmployeeDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'info' | 'subordinates' | 'documents' | 'audit'>('info');

  const { data: employeeData, isLoading } = useQuery({
    queryKey: ['employee', id],
    queryFn: () => employeeService.getById(id!),
    enabled: !!id
  });

  const { data: subordinates } = useQuery({
    queryKey: ['employee-subordinates', id],
    queryFn: () => employeeService.getSubordinates(id!),
    enabled: !!id && activeTab === 'subordinates'
  });

  const { data: documents } = useQuery({
    queryKey: ['employee-documents', id],
    queryFn: () => employeeService.getDocuments(id!),
    enabled: !!id && activeTab === 'documents'
  });

  const { data: auditLogs } = useQuery({
    queryKey: ['employee-audit', id],
    queryFn: () => employeeService.getAuditLogs(id!),
    enabled: !!id && activeTab === 'audit'
  });

  const uploadDocMutation = useMutation({
    mutationFn: ({ id, file, docType }: { id: string, file: File, docType: string }) => {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('documentType', docType);
      return employeeService.uploadDocument(id, formData);
    },
    onSuccess: () => {
      toast.success("Document uploaded successfully");
      queryClient.invalidateQueries({ queryKey: ['employee-documents', id] });
    },
    onError: () => toast.error("Failed to upload document")
  });

  const handleUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    
    if (file.size > 5 * 1024 * 1024) {
      toast.error("File size exceeds 5MB limit");
      return;
    }
    
    const validTypes = ['application/pdf', 'image/jpeg', 'image/png'];
    if (!validTypes.includes(file.type)) {
      toast.error("Only PDF, JPG, and PNG are allowed");
      return;
    }
    
    // For simplicity, hardcoding 'IdentityCard' as type for this demo, 
    // ideally should have a dropdown
    uploadDocMutation.mutate({ id: id!, file, docType: 'IdentityCard' });
  };

  if (isLoading) return <div className="p-8 text-center text-slate-500">Loading employee details...</div>;

  const emp = employeeData?.data;
  if (!emp) return <div className="p-8 text-center text-slate-500">Employee not found</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4 mb-6">
        <button onClick={() => navigate(-1)} className="p-2 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-600 transition-colors">
          <ArrowLeft size={20} />
        </button>
        <div>
          <h1 className="text-2xl font-bold text-slate-800">{emp.fullName}</h1>
          <p className="text-sm text-slate-500">{emp.positionName} at {emp.departmentName}</p>
        </div>
      </div>

      <div className="flex space-x-2 border-b border-slate-200 mb-6 overflow-x-auto pb-1">
        <button className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-2 whitespace-nowrap ${activeTab === 'info' ? 'border-blue-600 text-blue-600' : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'}`} onClick={() => setActiveTab('info')}>
          <User size={16} /> Information
        </button>
        <button className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-2 whitespace-nowrap ${activeTab === 'subordinates' ? 'border-blue-600 text-blue-600' : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'}`} onClick={() => setActiveTab('subordinates')}>
          <Users size={16} /> Subordinates
        </button>
        <button className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-2 whitespace-nowrap ${activeTab === 'documents' ? 'border-blue-600 text-blue-600' : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'}`} onClick={() => setActiveTab('documents')}>
          <FileText size={16} /> Documents
        </button>
        <button className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-2 whitespace-nowrap ${activeTab === 'audit' ? 'border-blue-600 text-blue-600' : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'}`} onClick={() => setActiveTab('audit')}>
          <Clock size={16} /> Audit Log
        </button>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden min-h-[400px]">
        {activeTab === 'info' && (
          <div className="p-6 sm:p-8">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              <div className="space-y-6">
                <div>
                  <h3 className="text-sm font-bold text-slate-400 uppercase tracking-wider mb-4">Personal Details</h3>
                  <div className="space-y-4">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center text-blue-600"><User size={20} /></div>
                      <div><p className="text-xs text-slate-500">Full Name</p><p className="font-semibold text-slate-800">{emp.fullName}</p></div>
                    </div>
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-lg bg-slate-50 flex items-center justify-center text-slate-600"><Building2 size={20} /></div>
                      <div><p className="text-xs text-slate-500">Department</p><p className="font-semibold text-slate-800">{emp.departmentName || '-'}</p></div>
                    </div>
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-lg bg-slate-50 flex items-center justify-center text-slate-600"><Briefcase size={20} /></div>
                      <div><p className="text-xs text-slate-500">Position</p><p className="font-semibold text-slate-800">{emp.positionName || '-'}</p></div>
                    </div>
                  </div>
                </div>
              </div>
              <div className="space-y-6">
                <div>
                  <h3 className="text-sm font-bold text-slate-400 uppercase tracking-wider mb-4">Employment Details</h3>
                  <div className="space-y-4">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center text-green-600"><Calendar size={20} /></div>
                      <div><p className="text-xs text-slate-500">Hire Date</p><p className="font-semibold text-slate-800">{new Date(emp.hireDate).toLocaleDateString()}</p></div>
                    </div>
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-lg bg-purple-50 flex items-center justify-center text-purple-600"><User size={20} /></div>
                      <div><p className="text-xs text-slate-500">Direct Manager</p><p className="font-semibold text-slate-800">{emp.managerName || '-'}</p></div>
                    </div>
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-lg bg-yellow-50 flex items-center justify-center text-yellow-600"><Clock size={20} /></div>
                      <div><p className="text-xs text-slate-500">Status</p><p className="font-semibold text-slate-800">{emp.status}</p></div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {activeTab === 'subordinates' && (
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-6 py-4 font-semibold text-slate-500">Name</th>
                <th className="px-6 py-4 font-semibold text-slate-500">Position</th>
                <th className="px-6 py-4 font-semibold text-slate-500">Department</th>
                <th className="px-6 py-4 font-semibold text-slate-500">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {subordinates?.data?.map((sub: any) => (
                <tr key={sub.id} className="hover:bg-slate-50 cursor-pointer" onClick={() => navigate(`/employees/${sub.id}`)}>
                  <td className="px-6 py-4 font-medium text-slate-900">{sub.fullName}</td>
                  <td className="px-6 py-4 text-slate-600">{sub.positionName}</td>
                  <td className="px-6 py-4 text-slate-600">{sub.departmentName}</td>
                  <td className="px-6 py-4"><span className="px-2.5 py-1 rounded-md text-xs font-semibold bg-green-50 text-green-700 border border-green-200">{sub.status}</span></td>
                </tr>
              ))}
              {(!subordinates?.data || subordinates.data.length === 0) && (
                <tr><td colSpan={4} className="px-6 py-8 text-center text-slate-400">No subordinates found</td></tr>
              )}
            </tbody>
          </table>
        )}

        {activeTab === 'documents' && (
          <div>
            <div className="p-4 border-b border-slate-200 bg-slate-50/50 flex justify-end">
              <label className="cursor-pointer flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors text-sm font-medium">
                <Upload size={16} />
                Upload Document
                <input type="file" className="hidden" accept=".pdf,.jpg,.png" onChange={handleUpload} disabled={uploadDocMutation.isPending} />
              </label>
            </div>
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  <th className="px-6 py-4 font-semibold text-slate-500">Type</th>
                  <th className="px-6 py-4 font-semibold text-slate-500">Uploaded At</th>
                  <th className="px-6 py-4 font-semibold text-slate-500 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {documents?.data?.map((doc: any) => (
                  <tr key={doc.id} className="hover:bg-slate-50">
                    <td className="px-6 py-4 font-medium text-slate-900">{doc.documentType}</td>
                    <td className="px-6 py-4 text-slate-600">{new Date(doc.uploadedAt).toLocaleString()}</td>
                    <td className="px-6 py-4 text-right">
                      <a href={`http://localhost:5000${doc.filePath}`} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-white border border-slate-200 rounded-md text-slate-600 hover:text-blue-600 hover:bg-blue-50 transition-colors">
                        <Download size={14} /> Download
                      </a>
                    </td>
                  </tr>
                ))}
                {(!documents?.data || documents.data.length === 0) && (
                  <tr><td colSpan={3} className="px-6 py-8 text-center text-slate-400">No documents found</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {activeTab === 'audit' && (
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-6 py-4 font-semibold text-slate-500">Date</th>
                <th className="px-6 py-4 font-semibold text-slate-500">Action</th>
                <th className="px-6 py-4 font-semibold text-slate-500">Changed By</th>
                <th className="px-6 py-4 font-semibold text-slate-500">Details</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {auditLogs?.data?.map((log: any) => (
                <tr key={log.id} className="hover:bg-slate-50">
                  <td className="px-6 py-4 text-slate-600">{new Date(log.changedAt).toLocaleString()}</td>
                  <td className="px-6 py-4 font-medium">
                    <span className={`px-2 py-1 rounded-md text-xs font-semibold ${log.action === 0 ? 'bg-green-50 text-green-700' : log.action === 1 ? 'bg-blue-50 text-blue-700' : 'bg-red-50 text-red-700'}`}>
                      {log.action === 0 ? 'Create' : log.action === 1 ? 'Update' : 'Delete'}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-slate-600 font-mono text-xs">{log.changedBy}</td>
                  <td className="px-6 py-4 text-xs">
                    <div className="max-w-xs overflow-auto max-h-24 p-2 bg-slate-50 rounded border border-slate-100 font-mono text-slate-600">
                      {log.newValue || log.oldValue || '-'}
                    </div>
                  </td>
                </tr>
              ))}
              {(!auditLogs?.data || auditLogs.data.length === 0) && (
                <tr><td colSpan={4} className="px-6 py-8 text-center text-slate-400">No audit logs found</td></tr>
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};


