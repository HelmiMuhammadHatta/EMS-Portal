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
    
    // Fix for strict System.Text.Json DateTime parsing
    if (data.startDate) data.startDate = `${data.startDate}T00:00:00Z`;
    if (data.endDate) data.endDate = `${data.endDate}T00:00:00Z`;
    
    createMutation.mutate(data);
  };

  const getStatusBadge = (status: string) => {
    switch(status) {
      case 'Approved':
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-semibold bg-green-50 text-green-700 border border-green-200"><CheckCircle size={12} /> Approved</span>;
      case 'Pending':
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-semibold bg-yellow-50 text-yellow-700 border border-yellow-200"><Clock size={12} /> Pending</span>;
      default:
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-semibold bg-red-50 text-red-700 border border-red-200"><XCircle size={12} /> {status}</span>;
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          {/* Title is handled by Layout, but keeping a subtitle here if needed */}
        </div>
        {user?.role !== 'Admin' && (
          <button onClick={() => setShowModal(true)} className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg shadow-sm shadow-blue-200 hover:bg-blue-700 hover:shadow-md hover:shadow-blue-200 font-medium transition-all">
            <Plus size={18} />
            Request Leave
          </button>
        )}
      </div>

      {(user?.role === 'Manager' || user?.role === 'Admin') && (
        <div className="flex space-x-2 border-b border-slate-200 mb-6">
          <button 
            className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-2 ${
              tab === 'my_requests' 
                ? 'border-blue-600 text-blue-600' 
                : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'
            }`}
            onClick={() => setTab('my_requests')}
          >
            <Calendar size={16} />
            My Requests
          </button>
          <button 
            className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-2 ${
              tab === 'team_requests' 
                ? 'border-blue-600 text-blue-600' 
                : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'
            }`}
            onClick={() => setTab('team_requests')}
          >
            <Inbox size={16} />
            Team Approvals
          </button>
        </div>
      )}

      {employeeData?.data && !employeeData.data.gender && (
        <div className="bg-yellow-50 border-l-4 border-yellow-400 p-4 mb-6 rounded-r-xl flex gap-3 shadow-sm">
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
                <div key={bal.leaveTypeId} className="bg-white p-4 rounded-xl shadow-sm border border-slate-200 flex flex-col justify-between hover:shadow-md transition-shadow">
                  <div>
                    <div className="text-[11px] font-bold text-slate-500 uppercase tracking-wider mb-2 line-clamp-1" title={bal.leaveTypeName}>{bal.leaveTypeName}</div>
                    <div className="flex items-baseline gap-1 mt-1">
                      <span className={`text-2xl font-bold tracking-tight ${textClass}`}>{bal.remainingDays}</span>
                      <span className="text-xs font-medium text-slate-400">/ {bal.totalDays}</span>
                    </div>
                  </div>
                  
                  <div className="mt-4">
                    <div className="w-full bg-slate-100 rounded-full h-1.5 mb-1.5 overflow-hidden">
                      <div className={`h-1.5 rounded-full ${colorClass}`} style={{ width: `${percentLeft}%` }}></div>
                    </div>
                    <div className="flex justify-between text-[10px] text-slate-400 font-semibold">
                      <span>Used: {bal.usedDays}</span>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        );
      })()}

      <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Employee</th>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Jenis Cuti</th>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Duration</th>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Reason</th>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Status</th>
                {tab === 'team_requests' && <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs text-right">Actions</th>}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={tab === 'team_requests' ? 6 : 5} className="px-6 py-12">
                    <div className="flex flex-col items-center justify-center text-slate-400 space-y-4">
                      <div className="w-8 h-8 border-4 border-slate-200 border-t-blue-600 rounded-full animate-spin"></div>
                      <p>Loading requests...</p>
                    </div>
                  </td>
                </tr>
              ) : data?.data?.data?.length === 0 ? (
                <tr>
                  <td colSpan={tab === 'team_requests' ? 6 : 5} className="px-6 py-16">
                    <div className="flex flex-col items-center justify-center text-slate-400 space-y-3">
                      <div className="w-16 h-16 bg-slate-50 rounded-full flex items-center justify-center">
                        <Calendar size={32} className="text-slate-300" />
                      </div>
                      <p className="text-slate-500 font-medium">No leave requests found</p>
                      <p className="text-sm">You haven't made any requests yet.</p>
                    </div>
                  </td>
                </tr>
              ) : (
                data?.data?.data?.map((req: any) => (
                  <tr key={req.id} className="hover:bg-slate-50/50 transition-colors">
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{req.employeeName}</div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-medium text-slate-700">
                        {req.leaveTypeName || leaveTypesData?.data?.find((t: any) => t.id === req.leaveTypeId)?.name || 'Cuti'}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-col">
                        <span className="font-medium text-slate-800">{new Date(req.startDate).toLocaleDateString()}</span>
                        <span className="text-xs text-slate-500">to {new Date(req.endDate).toLocaleDateString()}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      {req.reason.includes(' | Rejection Reason: ') ? (
                        <div className="flex flex-col gap-1">
                          <div className="max-w-[200px] text-slate-600 truncate text-sm" title={req.reason.split(' | Rejection Reason: ')[0]}>
                            {req.reason.split(' | Rejection Reason: ')[0]}
                          </div>
                          <div className="max-w-[200px] text-red-600 text-xs mt-1 p-1.5 bg-red-50 border border-red-100 rounded-md break-words" title={req.reason.split(' | Rejection Reason: ')[1]}>
                            <span className="font-semibold block mb-0.5">Alasan Ditolak:</span>
                            {req.reason.split(' | Rejection Reason: ')[1]}
                          </div>
                        </div>
                      ) : (
                        <div className="max-w-[200px] text-slate-600 truncate text-sm" title={req.reason}>
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
                              className="inline-flex items-center gap-1.5 bg-green-600 text-white px-3 py-1.5 rounded-md hover:bg-green-700 disabled:opacity-50 font-medium transition-colors shadow-sm text-sm"
                            >
                              <CheckCircle size={14} />
                              Approve
                            </button>
                            <button 
                              onClick={() => {
                                setRejectModalData({ id: req.id, name: req.employeeName });
                                setRejectReason("");
                              }}
                              disabled={rejectMutation.isPending}
                              className="inline-flex items-center gap-1.5 bg-red-600 text-white px-3 py-1.5 rounded-md hover:bg-red-700 disabled:opacity-50 font-medium transition-colors shadow-sm text-sm"
                            >
                              <XCircle size={14} />
                              Reject
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

      {showModal && (
        <div className="fixed inset-0 bg-slate-900/40 flex items-center justify-center z-50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg border border-slate-100 overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                <div className="flex items-center gap-2 text-slate-800">
                  <Send size={20} className="text-blue-600" />
                  <h2 className="text-lg font-bold">Request Leave</h2>
                </div>
                <button onClick={() => setShowModal(false)} className="text-slate-400 hover:text-slate-700 bg-white hover:bg-slate-100 p-1 rounded-full transition-colors">
                  <X size={20} />
                </button>
            </div>
            <div className="p-6">
              <form onSubmit={handleCreate} className="space-y-4">
                <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Leave Type</label>
                    <select 
                      name="leaveTypeId" 
                      required 
                      value={selectedLeaveTypeId}
                      onChange={(e) => setSelectedLeaveTypeId(e.target.value)}
                      className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all"
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
                      <div className="mt-2 flex items-center gap-2 bg-blue-50 p-2.5 rounded-lg border border-blue-100">
                        <Clock size={16} className="text-blue-500" />
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
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Start Date</label>
                    <input name="startDate" type="date" required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all" />
                  </div>
                  <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">End Date</label>
                    <input name="endDate" type="date" required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all" />
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-1.5">Reason</label>
                  <textarea name="reason" rows={3} placeholder="Please provide a reason..." required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all resize-none" />
                </div>
                
                <div className="flex justify-end gap-3 mt-8 pt-6 border-t border-slate-100">
                  <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border border-slate-200 bg-white rounded-lg text-slate-600 hover:bg-slate-50 font-medium transition-colors shadow-sm">Cancel</button>
                  <button type="submit" disabled={createMutation.isPending} className="px-5 py-2 bg-blue-600 text-white rounded-lg shadow-sm hover:bg-blue-700 hover:shadow-md font-medium transition-all disabled:opacity-70 flex items-center gap-2">
                    {createMutation.isPending ? 'Submitting...' : <><Send size={16}/> Submit Request</>}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {rejectModalData && (
        <div className="fixed inset-0 bg-slate-900/40 flex items-center justify-center z-50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md border border-slate-100 overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center px-6 py-4 border-b border-slate-100 bg-red-50/50">
                <div className="flex items-center gap-2 text-red-800">
                  <XCircle size={20} className="text-red-600" />
                  <h2 className="text-lg font-bold">Tolak Pengajuan Cuti</h2>
                </div>
                <button onClick={() => setRejectModalData(null)} className="text-slate-400 hover:text-slate-700 bg-white hover:bg-slate-100 p-1 rounded-full transition-colors">
                  <X size={20} />
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
                  <label className="block text-sm font-semibold text-slate-700 mb-1.5">Alasan Penolakan <span className="text-red-500">*</span></label>
                  <textarea 
                    value={rejectReason}
                    onChange={(e) => setRejectReason(e.target.value)}
                    rows={4} 
                    placeholder="Masukkan alasan penolakan di sini..." 
                    required 
                    className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-red-500/20 focus:border-red-500 outline-none transition-all resize-none" 
                  />
                </div>
                <div className="flex justify-end gap-3 mt-8 pt-6 border-t border-slate-100">
                  <button type="button" onClick={() => setRejectModalData(null)} className="px-4 py-2 border border-slate-200 bg-white rounded-lg text-slate-600 hover:bg-slate-50 font-medium transition-colors shadow-sm">Batal</button>
                  <button type="submit" disabled={rejectMutation.isPending || !rejectReason.trim()} className="px-5 py-2 bg-red-600 text-white rounded-lg shadow-sm hover:bg-red-700 hover:shadow-md font-medium transition-all disabled:opacity-70 flex items-center gap-2">
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
