import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { candidateService, departmentService, positionService } from '../services/apiService';
import { toast } from 'sonner';
import { Search, Plus, Filter, Users, X, ArrowRight } from 'lucide-react';
import { format } from 'date-fns';

export const CandidateList = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [showAddModal, setShowAddModal] = useState(false);
  const [formData, setFormData] = useState({ fullName: '', email: '', phone: '', appliedDepartmentId: '', appliedPositionId: '', notes: '' });

  const { data: candidates, isLoading } = useQuery({
    queryKey: ['candidates'],
    queryFn: candidateService.getAll
  });

  const { data: departments } = useQuery({ queryKey: ['departments'], queryFn: departmentService.getAll });
  const { data: positions } = useQuery({ queryKey: ['positions'], queryFn: positionService.getAll });

  const createMutation = useMutation({
    mutationFn: candidateService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['candidates'] });
      toast.success('Candidate created successfully');
      setShowAddModal(false);
      setFormData({ fullName: '', email: '', phone: '', appliedDepartmentId: '', appliedPositionId: '', notes: '' });
    },
    onError: (err: any) => {
      toast.error(err.response?.data?.message || 'Failed to create candidate');
    }
  });

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Applied': return 'bg-slate-100 text-slate-700 border-slate-200';
      case 'TestAssigned': return 'bg-blue-100 text-blue-700 border-blue-200';
      case 'TestCompleted': return 'bg-purple-100 text-purple-700 border-purple-200';
      case 'Interview': return 'bg-amber-100 text-amber-700 border-amber-200';
      case 'Passed': return 'bg-green-100 text-green-700 border-green-200';
      case 'Hired': return 'bg-teal-100 text-teal-700 border-teal-200';
      case 'Rejected': return 'bg-red-100 text-red-700 border-red-200';
      default: return 'bg-slate-100 text-slate-700';
    }
  };

  const filteredCandidates = candidates?.filter((c: any) => 
    c.fullName.toLowerCase().includes(searchTerm.toLowerCase()) &&
    (statusFilter === '' || c.status === statusFilter)
  ) || [];

  return (
    <div className="flex flex-col h-full animate-in fade-in duration-300">
      <header className="bg-white px-8 py-6 border-b border-slate-200 shrink-0">
        <div className="flex justify-between items-center max-w-6xl mx-auto w-full">
          <div>
            <h1 className="text-2xl font-bold text-slate-800 flex items-center gap-2">
              <Users size={24} className="text-blue-600" />
              Recruitment
            </h1>
            <p className="text-slate-500 mt-1">Manage candidates and job applications</p>
          </div>
          <button
            onClick={() => setShowAddModal(true)}
            className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2.5 rounded-lg hover:bg-blue-700 transition-colors font-medium shadow-sm shadow-blue-200"
          >
            <Plus size={18} /> Add Candidate
          </button>
        </div>
      </header>

      <div className="flex-1 overflow-auto bg-slate-50 p-8">
        <div className="max-w-6xl mx-auto space-y-6">
          
          <div className="flex flex-col sm:flex-row gap-4 justify-between bg-white p-4 rounded-xl shadow-sm border border-slate-200">
            <div className="relative w-full sm:w-96">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={20} />
              <input
                type="text"
                placeholder="Search candidates..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-10 pr-4 py-2 bg-slate-50 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white transition-colors"
              />
            </div>
            
            <div className="relative min-w-[200px]">
              <Filter className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                className="w-full pl-10 pr-4 py-2 bg-slate-50 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 appearance-none transition-colors font-medium text-slate-700"
              >
                <option value="">All Statuses</option>
                <option value="Applied">Applied</option>
                <option value="TestAssigned">Test Assigned</option>
                <option value="TestCompleted">Test Completed</option>
                <option value="Interview">Interview</option>
                <option value="Passed">Passed</option>
                <option value="Rejected">Rejected</option>
                <option value="Hired">Hired</option>
              </select>
            </div>
          </div>

          <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm text-slate-600">
                <thead className="bg-slate-50 text-slate-500 border-b border-slate-200">
                  <tr>
                    <th className="px-6 py-4 font-semibold">Candidate</th>
                    <th className="px-6 py-4 font-semibold">Applied Position</th>
                    <th className="px-6 py-4 font-semibold">Applied Date</th>
                    <th className="px-6 py-4 font-semibold">Status</th>
                    <th className="px-6 py-4 font-semibold text-right">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {isLoading ? (
                    <tr>
                      <td colSpan={5} className="px-6 py-12 text-center text-slate-500">Loading candidates...</td>
                    </tr>
                  ) : filteredCandidates.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="px-6 py-12 text-center text-slate-500">No candidates found.</td>
                    </tr>
                  ) : (
                    filteredCandidates.map((c: any) => (
                      <tr key={c.id} className="hover:bg-slate-50/50 transition-colors group">
                        <td className="px-6 py-4">
                          <div className="font-semibold text-slate-800">{c.fullName}</div>
                          <div className="text-xs text-slate-500 mt-0.5">{c.email}</div>
                        </td>
                        <td className="px-6 py-4">
                          <div className="font-medium text-slate-700">{c.appliedPositionName}</div>
                          <div className="text-xs text-slate-500 mt-0.5">{c.appliedDepartmentName}</div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          {format(new Date(c.createdAt), 'MMM d, yyyy')}
                        </td>
                        <td className="px-6 py-4">
                          <span className={`px-2.5 py-1 text-[11px] uppercase tracking-wider font-bold rounded-full border ${getStatusColor(c.status)}`}>
                            {c.status}
                          </span>
                        </td>
                        <td className="px-6 py-4 text-right">
                          <button
                            onClick={() => navigate(`/candidates/${c.id}`)}
                            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-blue-600 bg-blue-50 border border-transparent rounded-lg hover:border-blue-200 transition-colors"
                          >
                            Details <ArrowRight size={14} />
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      {showAddModal && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-2xl w-full max-w-lg shadow-xl overflow-hidden animate-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center p-6 border-b border-slate-100">
              <h2 className="text-xl font-bold text-slate-800">Add New Candidate</h2>
              <button onClick={() => setShowAddModal(false)} className="text-slate-400 hover:bg-slate-100 p-2 rounded-lg transition-colors">
                <X size={20} />
              </button>
            </div>
            <form onSubmit={(e) => {
              e.preventDefault();
              createMutation.mutate(formData);
            }} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Full Name</label>
                <input required type="text" className="w-full p-2.5 bg-slate-50 border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:bg-white outline-none" value={formData.fullName} onChange={e => setFormData({ ...formData, fullName: e.target.value })} />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Email</label>
                  <input required type="email" className="w-full p-2.5 bg-slate-50 border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:bg-white outline-none" value={formData.email} onChange={e => setFormData({ ...formData, email: e.target.value })} />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Phone</label>
                  <input type="text" className="w-full p-2.5 bg-slate-50 border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:bg-white outline-none" value={formData.phone} onChange={e => setFormData({ ...formData, phone: e.target.value })} />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Department</label>
                  <select required className="w-full p-2.5 bg-slate-50 border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none" value={formData.appliedDepartmentId} onChange={e => setFormData({ ...formData, appliedDepartmentId: e.target.value })}>
                    <option value="">Select Department</option>
                    {departments?.map((d: any) => <option key={d.id} value={d.id}>{d.name}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Position</label>
                  <select required className="w-full p-2.5 bg-slate-50 border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none" value={formData.appliedPositionId} onChange={e => setFormData({ ...formData, appliedPositionId: e.target.value })}>
                    <option value="">Select Position</option>
                    {positions?.map((p: any) => <option key={p.id} value={p.id}>{p.name}</option>)}
                  </select>
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Notes</label>
                <textarea rows={3} className="w-full p-2.5 bg-slate-50 border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:bg-white outline-none" value={formData.notes} onChange={e => setFormData({ ...formData, notes: e.target.value })}></textarea>
              </div>
              
              <div className="pt-4 border-t border-slate-100 flex justify-end gap-3">
                <button type="button" onClick={() => setShowAddModal(false)} className="px-5 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-100 rounded-lg transition-colors">
                  Cancel
                </button>
                <button type="submit" disabled={createMutation.isPending} className="px-5 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm shadow-blue-200 disabled:opacity-70 transition-colors">
                  {createMutation.isPending ? 'Saving...' : 'Add Candidate'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
