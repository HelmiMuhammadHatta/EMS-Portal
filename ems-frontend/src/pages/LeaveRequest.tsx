import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { leaveService, employeeService } from '../services/apiService';
import { useAuth } from '../hooks/useAuth';
import { toast } from 'sonner';
import { Calendar, Plus, CheckCircle, Clock, XCircle, X, Send, Inbox } from 'lucide-react';

export const LeaveRequest = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [showModal, setShowModal] = useState(false);
  const [rejectModalData, setRejectModalData] = useState<{ id: string, name: string } | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [tab, setTab] = useState<'my_requests' | 'team_requests'>('my_requests');

  const { data: employeeData } = useQuery({
    queryKey: ['employee', user?.employeeId],
    queryFn: () => employeeService.getById(user?.employeeId || ''),
    enabled: !!user?.employeeId
  });

  const { data, isLoading } = useQuery({
    queryKey: ['leaves', tab],
    queryFn: () => leaveService.getRequests()
  });

  const { data: balances } = useQuery({
    queryKey: ['leave-balances', user?.employeeId],
    queryFn: () => leaveService.getBalances(user?.employeeId || ''),
    enabled: !!user?.employeeId
  });

  const { data: leaveTypesData } = useQuery({
    queryKey: ['leave-types', user?.employeeId],
    queryFn: () => leaveService.getTypes(user?.employeeId)
  });

  const [selectedLeaveTypeId, setSelectedLeaveTypeId] = useState<string>('');

  const createMutation = useMutation({
    mutationFn: leaveService.create,
    onSuccess: () => {
      toast.success("Leave requested successfully!");
      queryClient.invalidateQueries({ queryKey: ['leaves'] });
      queryClient.invalidateQueries({ queryKey: ['leave-balances'] });
      setShowModal(false);
    },
    onError: (err: any) => {
      const data = err.response?.data;
      let msg = "Failed to request leave";
      if (data) {
        if (Array.isArray(data.errors) && data.errors.length > 0) msg = data.errors[0];
        else if (data.errors && typeof data.errors === 'object') {
          const firstKey = Object.keys(data.errors)[0];
          if (firstKey) msg = data.errors[firstKey][0];
        } else if (data.message) msg = data.message;
        else if (data.Message) msg = data.Message;
        else if (data.title) msg = data.title;
      }
      toast.error(msg);
    }
  });

  const approveMutation = useMutation({
    mutationFn: leaveService.approve,
    onSuccess: () => {
      toast.success("Leave approved!");
      queryClient.invalidateQueries({ queryKey: ['leaves'] });
    }
  });

  const rejectMutation = useMutation({
    mutationFn: leaveService.reject,
    onSuccess: () => {
      toast.success("Leave rejected!");
      queryClient.invalidateQueries({ queryKey: ['leaves'] });
      setRejectModalData(null);
      setRejectReason("");
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message || err.response?.data?.errors?.[0] || "Failed to reject leave";
      toast.error(msg);
    }
  });

  const handleCreate = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const data = Object.fromEntries(formData.entries());
    
    if (data.startDate) data.startDate = `${data.startDate}T00:00:00Z`;
    if (data.endDate) data.endDate = `${data.endDate}T00:00:00Z`;
    
    createMutation.mutate(data);
  };

  const getStatusBadge = (status: string) => {
    switch(status) {
      case 'Approved':
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700"><CheckCircle size={14} /> Approved</span>;
      case 'Pending':
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-700"><Clock size={14} /> Pending</span>;
      default:
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-red-100 text-red-700"><XCircle size={14} /> {status}</span>;
    }
  };

  return (
    <div className="flex flex-col min-h-full">
      {/* Page Header */}
      <div className="bg-white px-8 py-6 border-b border-slate-200">
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 max-w-7xl mx-auto w-full">
          <div>
            <h1 className="text-2xl font-bold text-slate-800">Leave Requests</h1>
            <div className="text-xs text-slate-500 flex items-center gap-1 mt-1 font-medium">
              <span>EMS Portal</span>
              <span className="text-slate-300">/</span>
              <span className="text-slate-600">Leaves</span>
            </div>
          </div>
          {user?.role !== 'Admin' && (
            <button onClick={() => setShowModal(true)} className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2.5 rounded-md hover:bg-blue-700 font-medium transition-all shadow-sm shrink-0">
              <Plus size={18} />
              Request Leave
            </button>
          )}
        </div>
      </div>

      <div className="p-8 max-w-7xl mx-auto w-full flex-1">
        {(user?.role === 'Manager' || user?.role === 'Admin') && (
          <div className="flex space-x-2 border-b border-slate-200 mb-6">
            <button 
              className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-[3px] ${
                tab === 'my_requests' 
                  ? 'border-blue-600 text-blue-600' 
                  : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'
              }`}
              onClick={() => setTab('my_requests')}
            >
              <Calendar size={18} />
              My Requests
            </button>
            <button 
              className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-[3px] ${
                tab === 'team_requests' 
                  ? 'border-blue-600 text-blue-600' 
                  : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'
              }`}
              onClick={() => setTab('team_requests')}
            >
              <Inbox size={18} />
              Team Approvals
            </button>
          </div>
        )}

        {employeeData?.data && !employeeData.data.gender && (
          <div className="bg-yellow-50 border border-yellow-200 p-4 mb-6 rounded-lg flex gap-3 shadow-sm">
            <div className="text-yellow-600 mt-0.5">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
            </div>
            <div>
              <h3 className="text-sm font-semibold text-yellow-800">Perhatian</h3>
              <p className="text-sm text-yellow-700 mt-1">Lengkapi data Gender Anda untuk melihat semua jenis cuti yang tersedia</p>
            </div>
          </div>
        )}

        {/* Balance Summary Card */}
        {balances?.data?.length > 0 && tab === 'my_requests' && (() => {
          const validBalances = balances.data.filter((bal: any) => bal.totalDays > 0);
          return (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4 mb-6">
              {validBalances.map((bal: any) => {
                const percentLeft = bal.totalDays > 0 ? (bal.remainingDays / bal.totalDays) * 100 : 0;
                let colorClass = "bg-green-500";
                let textClass = "text-green-600";
                if (percentLeft <= 20) {
                  colorClass = "bg-red-500"; textClass = "text-red-600";
                } else if (percentLeft <= 50) {
                  colorClass = "bg-yellow-500"; textClass = "text-yellow-600";
                }

                return (
                  <div key={bal.leaveTypeId} className="bg-white p-5 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200 flex flex-col justify-between hover:shadow-md transition-shadow">
                    <div>
                      <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2 line-clamp-1" title={bal.leaveTypeName}>{bal.leaveTypeName}</div>
                      <div className="flex items-baseline gap-1 mt-1">
                        <span className={`text-3xl font-bold tracking-tight ${textClass}`}>{bal.remainingDays}</span>
                        <span className="text-sm font-medium text-slate-400">/ {bal.totalDays}</span>
                      </div>
                    </div>
                    
                    <div className="mt-5">
                      <div className="w-full bg-slate-100 rounded-full h-1.5 mb-2 overflow-hidden">
                        <div className={`h-1.5 rounded-full ${colorClass}`} style={{ width: `${percentLeft}%` }}></div>
                      </div>
                      <div className="flex justify-between text-[11px] text-slate-400 font-medium">
                        <span>Used: {bal.usedDays} days</span>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          );
        })()}

        <div className="bg-white rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Employee</th>
                  <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Jenis Cuti</th>
                  <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Duration</th>
                  <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Reason</th>
                  <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Status</th>
                  {tab === 'team_requests' && <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs text-right">Actions</th>}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-slate-700">
                {isLoading ? (
                  Array.from({ length: 4 }).map((_, i) => (
                    <tr key={i} className="animate-pulse">
                      <td className="px-6 py-4"><div className="h-4 w-32 bg-slate-200 rounded"></div></td>
                      <td className="px-6 py-4"><div className="h-4 w-24 bg-slate-200 rounded"></div></td>
                      <td className="px-6 py-4"><div className="h-4 w-24 bg-slate-200 rounded mb-1"></div><div className="h-3 w-16 bg-slate-200 rounded"></div></td>
                      <td className="px-6 py-4"><div className="h-4 w-40 bg-slate-200 rounded"></div></td>
                      <td className="px-6 py-4"><div className="h-6 w-20 bg-slate-200 rounded-full"></div></td>
                      {tab === 'team_requests' && <td className="px-6 py-4"><div className="flex gap-2 justify-end"><div className="h-8 w-8 bg-slate-200 rounded-md"></div><div className="h-8 w-8 bg-slate-200 rounded-md"></div></div></td>}
                    </tr>
                  ))
                ) : data?.data?.data?.length === 0 ? (
                  <tr>
                    <td colSpan={tab === 'team_requests' ? 6 : 5} className="px-6 py-24">
                      <div className="flex flex-col items-center justify-center space-y-4">
                        <div className="w-20 h-20 bg-slate-50 rounded-full flex items-center justify-center">
                          <Calendar size={40} className="text-slate-300" />
                        </div>
                        <div className="text-center">
                          <h3 className="text-lg font-semibold text-slate-800 mb-1">Belum ada pengajuan cuti</h3>
                          <p className="text-sm text-slate-500">
                            {tab === 'my_requests' ? 'Anda belum membuat pengajuan cuti.' : 'Belum ada pengajuan cuti dari tim Anda.'}
                          </p>
                        </div>
                        {tab === 'my_requests' && (
                          <button onClick={() => setShowModal(true)} className="mt-2 bg-white border border-slate-200 text-slate-700 hover:bg-slate-50 px-4 py-2 rounded-md font-medium text-sm transition-colors shadow-sm">
                            Buat Pengajuan
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ) : (
                  data?.data?.data?.map((req: any) => (
                    <tr key={req.id} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-6 py-4">
                        <div className="font-semibold text-slate-900">{req.employeeName}</div>
                      </td>
                      <td className="px-6 py-4 font-medium">
                        {req.leaveTypeName || leaveTypesData?.data?.find((t: any) => t.id === req.leaveTypeId)?.name || 'Cuti'}
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex flex-col">
                          <span className="font-medium text-slate-800">{new Date(req.startDate).toLocaleDateString()}</span>
                          <span className="text-xs text-slate-500 mt-0.5">to {new Date(req.endDate).toLocaleDateString()}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        {req.reason.includes(' | Rejection Reason: ') ? (
                          <div className="flex flex-col gap-1.5">
                            <div className="max-w-[200px] text-slate-700 truncate text-sm" title={req.reason.split(' | Rejection Reason: ')[0]}>
                              {req.reason.split(' | Rejection Reason: ')[0]}
                            </div>
                            <div className="max-w-[250px] text-red-700 text-xs mt-1 p-2 bg-red-50 border border-red-100 rounded-md break-words" title={req.reason.split(' | Rejection Reason: ')[1]}>
                              <span className="font-semibold block mb-0.5">Alasan Ditolak:</span>
                              {req.reason.split(' | Rejection Reason: ')[1]}
                            </div>
                          </div>
                        ) : (
                          <div className="max-w-[200px] text-slate-700 truncate text-sm" title={req.reason}>
                            {req.reason}
                          </div>
                        )}
                      </td>
                      <td className="px-6 py-4">
                        {getStatusBadge(req.status)}
                      </td>
                      {tab === 'team_requests' && (
                        <td className="px-6 py-4 text-right">
                          {req.status === 'Pending' ? (
                            <div className="flex justify-end gap-2">
                              <button 
                                onClick={() => approveMutation.mutate(req.id)}
                                disabled={approveMutation.isPending}
                                className="text-slate-400 hover:text-green-600 p-2 rounded-md hover:bg-green-50 transition-colors"
                                title="Approve"
                              >
                                <CheckCircle size={20} />
                              </button>
                              <button 
                                onClick={() => {
                                  setRejectModalData({ id: req.id, name: req.employeeName });
                                  setRejectReason("");
                                }}
                                disabled={rejectMutation.isPending}
                                className="text-slate-400 hover:text-red-600 p-2 rounded-md hover:bg-red-50 transition-colors"
                                title="Reject"
                              >
                                <XCircle size={20} />
                              </button>
                            </div>
                          ) : (
                            <span className="text-slate-400 text-xs">-</span>
                          )}
                        </td>
                      )}
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {showModal && (
        <div className="fixed inset-0 bg-slate-900/40 flex items-center justify-center z-50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-lg shadow-xl w-full max-w-[480px] overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center px-6 py-4 border-b border-slate-200 bg-slate-50/50">
                <h2 className="text-lg font-bold text-slate-800">Request Leave</h2>
                <button onClick={() => setShowModal(false)} className="text-slate-400 hover:text-slate-700 bg-white border border-slate-200 hover:bg-slate-100 p-1.5 rounded-md transition-colors shadow-sm">
                  <X size={18} />
                </button>
            </div>
            <div className="p-6">
              <form onSubmit={handleCreate} className="space-y-4">
                <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1.5">Leave Type</label>
                    <select 
                      name="leaveTypeId" 
                      required 
                      value={selectedLeaveTypeId}
                      onChange={(e) => setSelectedLeaveTypeId(e.target.value)}
                      className="w-full border border-slate-200 bg-white px-3 py-2 rounded-md focus:border-blue-600 focus:ring-1 focus:ring-blue-600 outline-none transition-all shadow-sm text-sm"
                    >
                        <option value="" disabled>Select Leave Type</option>
                      {leaveTypesData?.data?.filter((type: any) => {
                        const userBal = balances?.data?.find((b: any) => b.leaveTypeId === type.id);
                        return userBal && userBal.totalDays > 0;
                      }).map((type: any) => (
                          <option key={type.id} value={type.id}>{type.name}</option>
                        ))}
                    </select>
                    {selectedLeaveTypeId && balances?.data && (
                      <div className="mt-3 flex items-center gap-2 bg-blue-50 p-3 rounded-md border border-blue-100">
                        <Clock size={16} className="text-blue-600" />
                        <p className="text-sm text-slate-700">
                          Sisa cuti: <span className="font-bold text-blue-700">
                            {balances.data.find((b: any) => b.leaveTypeId === selectedLeaveTypeId)?.remainingDays ?? 0} hari
                          </span>
                        </p>
                      </div>
                    )}
                </div>
                
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1.5">Start Date</label>
                    <input name="startDate" type="date" required className="w-full border border-slate-200 bg-white px-3 py-2 rounded-md focus:border-blue-600 focus:ring-1 focus:ring-blue-600 outline-none transition-all shadow-sm text-sm" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1.5">End Date</label>
                    <input name="endDate" type="date" required className="w-full border border-slate-200 bg-white px-3 py-2 rounded-md focus:border-blue-600 focus:ring-1 focus:ring-blue-600 outline-none transition-all shadow-sm text-sm" />
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1.5">Reason</label>
                  <textarea name="reason" rows={3} placeholder="Please provide a reason..." required className="w-full border border-slate-200 bg-white px-3 py-2 rounded-md focus:border-blue-600 focus:ring-1 focus:ring-blue-600 outline-none transition-all resize-none shadow-sm text-sm" />
                </div>
                
                <div className="flex justify-end gap-3 mt-8 pt-6 border-t border-slate-200">
                  <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border border-slate-200 bg-white rounded-md text-slate-700 hover:bg-slate-50 font-medium transition-colors shadow-sm text-sm">Cancel</button>
                  <button type="submit" disabled={createMutation.isPending} className="px-4 py-2 bg-blue-600 text-white rounded-md shadow-sm hover:bg-blue-700 font-medium transition-all disabled:opacity-70 flex items-center gap-2 text-sm">
                    {createMutation.isPending ? 'Submitting...' : 'Submit Request'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {rejectModalData && (
        <div className="fixed inset-0 bg-slate-900/40 flex items-center justify-center z-50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-lg shadow-xl w-full max-w-[480px] overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center px-6 py-4 border-b border-slate-200 bg-slate-50/50">
                <h2 className="text-lg font-bold text-slate-800">Tolak Pengajuan Cuti</h2>
                <button onClick={() => setRejectModalData(null)} className="text-slate-400 hover:text-slate-700 bg-white border border-slate-200 hover:bg-slate-100 p-1.5 rounded-md transition-colors shadow-sm">
                  <X size={18} />
                </button>
            </div>
            <div className="p-6">
              <form onSubmit={(e) => {
                e.preventDefault();
                rejectMutation.mutate({ id: rejectModalData.id, reason: rejectReason });
              }} className="space-y-4">
                <p className="text-sm text-slate-600 mb-4">
                  Anda akan menolak permintaan cuti dari <span className="font-semibold text-slate-800">{rejectModalData.name}</span>.
                </p>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1.5">Alasan Penolakan <span className="text-red-500">*</span></label>
                  <textarea 
                    value={rejectReason}
                    onChange={(e) => setRejectReason(e.target.value)}
                    rows={4} 
                    placeholder="Masukkan alasan penolakan di sini..." 
                    required 
                    className="w-full border border-slate-200 bg-white px-3 py-2 rounded-md focus:border-red-600 focus:ring-1 focus:ring-red-600 outline-none transition-all resize-none shadow-sm text-sm" 
                  />
                </div>
                <div className="flex justify-end gap-3 mt-8 pt-6 border-t border-slate-200">
                  <button type="button" onClick={() => setRejectModalData(null)} className="px-4 py-2 border border-slate-200 bg-white rounded-md text-slate-700 hover:bg-slate-50 font-medium transition-colors shadow-sm text-sm">Batal</button>
                  <button type="submit" disabled={rejectMutation.isPending || !rejectReason.trim()} className="px-4 py-2 bg-red-600 text-white rounded-md shadow-sm hover:bg-red-700 font-medium transition-all disabled:opacity-70 flex items-center gap-2 text-sm">
                    {rejectMutation.isPending ? 'Memproses...' : 'Tolak Cuti'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
