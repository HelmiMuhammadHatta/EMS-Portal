import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { employeeService } from '../services/apiService';
import { toast } from 'sonner';
import { Search, Plus, KeyRound, Users, ChevronLeft, ChevronRight, X, UserCog, Edit, Trash2, Eye } from 'lucide-react';
import { departmentService, positionService } from '../services/apiService';
import { Link, useNavigate } from 'react-router-dom';

export const EmployeeList = () => {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [deptFilter, setDeptFilter] = useState('');
  const [posFilter, setPosFilter] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(null);
  const [editData, setEditData] = useState<any>(null);
  const [selectedDeptId, setSelectedDeptId] = useState<string>('');
  const [selectedPosId, setSelectedPosId] = useState<string>('');
  const queryClient = useQueryClient();

  const { data: depts } = useQuery({ queryKey: ['departments'], queryFn: departmentService.getAll });
  const { data: positions } = useQuery({ queryKey: ['positions'], queryFn: positionService.getAll });

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['employees', page, search, deptFilter, posFilter],
    queryFn: () => employeeService.getAll({ page, pageSize: 10, search, departmentId: deptFilter || undefined, positionId: posFilter || undefined })
  });

  const createMutation = useMutation({
    mutationFn: employeeService.create,
    onSuccess: (res) => {
      toast.success("Employee created! Temp Password: " + res.data.temporaryPassword, { duration: 10000 });
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      setShowModal(false);
    },
    onError: (err: any) => {
      toast.error(err.response?.data?.errors?.[0] || "Failed to create employee");
    }
  });

  const changePasswordMutation = useMutation({
    mutationFn: (data: { id: string, newPassword: string }) => employeeService.changePassword(data.id, { newPassword: data.newPassword }),
    onSuccess: () => {
      toast.success("Password updated successfully!");
      setShowPasswordModal(false);
      setSelectedEmployeeId(null);
    },
    onError: (err: any) => {
      toast.error(err.response?.data?.errors?.[0] || "Failed to change password");
    }
  });

  const updateMutation = useMutation({
    mutationFn: (data: { id: string, body: any }) => employeeService.update(data.id, data.body),
    onSuccess: () => {
      toast.success("Employee updated successfully!");
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      setShowEditModal(false);
    },
    onError: (err: any) => toast.error("Failed to update employee")
  });

  const deleteMutation = useMutation({
    mutationFn: employeeService.delete,
    onSuccess: () => {
      toast.success("Employee deleted successfully!");
      queryClient.invalidateQueries({ queryKey: ['employees'] });
    },
    onError: (err: any) => toast.error("Failed to delete employee")
  });

  const handleCreate = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const data = Object.fromEntries(formData.entries());
    if (!data.managerId) data.managerId = null;
    createMutation.mutate(data);
  };

  const handleUpdate = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selectedEmployeeId) return;
    const formData = new FormData(e.currentTarget);
    const data = Object.fromEntries(formData.entries());
    if (!data.managerId) data.managerId = null;
    updateMutation.mutate({ id: selectedEmployeeId, body: data });
  };

  const handleDelete = (id: string) => {
    if (window.confirm("Are you sure you want to delete this employee?")) {
      deleteMutation.mutate(id);
    }
  };

  const handleChangePassword = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selectedEmployeeId) return;
    const formData = new FormData(e.currentTarget);
    const newPassword = formData.get('newPassword') as string;
    changePasswordMutation.mutate({ id: selectedEmployeeId, newPassword });
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
            <input 
              type="text" 
              placeholder="Search employees..." 
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-10 pr-4 py-2 bg-white border border-slate-200 rounded-lg w-full sm:w-64 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 shadow-sm transition-all" 
            />
          </div>
          <select 
            value={deptFilter} 
            onChange={(e) => setDeptFilter(e.target.value)} 
            className="py-2 px-3 bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 text-sm"
          >
            <option value="">All Departments</option>
            {depts?.map((d: any) => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
          <select 
            value={posFilter} 
            onChange={(e) => setPosFilter(e.target.value)} 
            className="py-2 px-3 bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 text-sm"
          >
            <option value="">All Positions</option>
            {positions?.map((p: any) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
        </div>
        <button onClick={() => { setShowModal(true); setSelectedDeptId(''); setSelectedPosId(''); }} className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg shadow-sm shadow-blue-200 hover:bg-blue-700 hover:shadow-md hover:shadow-blue-200 font-medium transition-all shrink-0">
          <Plus size={18} />
          Add Employee
        </button>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Employee</th>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Department</th>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Position</th>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Status</th>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12">
                    <div className="flex flex-col items-center justify-center text-slate-400 space-y-4">
                      <div className="w-8 h-8 border-4 border-slate-200 border-t-blue-600 rounded-full animate-spin"></div>
                      <p>Loading employees...</p>
                    </div>
                  </td>
                </tr>
              ) : isError ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-red-500 font-medium">
                    {(error as any)?.response?.data?.message || (error as any)?.message || 'Failed to load data.'}
                  </td>
                </tr>
              ) : !data?.data?.data || data.data.data.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-16">
                    <div className="flex flex-col items-center justify-center text-slate-400 space-y-3">
                      <div className="w-16 h-16 bg-slate-50 rounded-full flex items-center justify-center">
                        <Users size={32} className="text-slate-300" />
                      </div>
                      <p className="text-slate-500 font-medium">No employees found</p>
                      <p className="text-sm">Try adjusting your search or add a new employee.</p>
                    </div>
                  </td>
                </tr>
              ) : (
                data.data.data.map((emp: any) => (
                  <tr key={emp.id} className="hover:bg-slate-50/50 transition-colors">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center font-bold">
                          {emp.fullName.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <div className="font-semibold text-slate-900">{emp.fullName}</div>
                          <div className="text-xs text-slate-500">{emp.email}</div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-slate-600 font-medium">{emp.departmentName || 'IT Department'}</td>
                    <td className="px-6 py-4 text-slate-600">{emp.positionName || 'Staff'}</td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex items-center px-2.5 py-1 rounded-md text-xs font-semibold border ${
                        emp.status === 'Active' 
                          ? 'bg-green-50 text-green-700 border-green-200' 
                          : 'bg-slate-100 text-slate-700 border-slate-200'
                      }`}>
                        {emp.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Link to={`/employees/${emp.id}`} className="text-slate-500 hover:text-blue-600 p-1.5 rounded-md hover:bg-blue-50 transition-colors" title="View Details">
                          <Eye size={16} />
                        </Link>
                        <button onClick={() => { setSelectedEmployeeId(emp.id); setEditData(emp); setSelectedDeptId(emp.departmentId); setSelectedPosId(emp.positionId); setShowEditModal(true); }} className="text-slate-500 hover:text-orange-600 p-1.5 rounded-md hover:bg-orange-50 transition-colors" title="Edit Employee">
                          <Edit size={16} />
                        </button>
                        <button onClick={() => { setSelectedEmployeeId(emp.id); setShowPasswordModal(true); }} className="text-slate-500 hover:text-blue-600 p-1.5 rounded-md hover:bg-blue-50 transition-colors" title="Change Password">
                          <KeyRound size={16} />
                        </button>
                        <button onClick={() => handleDelete(emp.id)} className="text-slate-500 hover:text-red-600 p-1.5 rounded-md hover:bg-red-50 transition-colors" title="Delete Employee">
                          <Trash2 size={16} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        
        {/* Pagination */}
        <div className="px-6 py-4 border-t border-slate-200 flex justify-between items-center bg-slate-50">
          <span className="text-sm text-slate-500 font-medium">Page {data?.data?.currentPage || 1} of {data?.data?.totalPages || 1}</span>
          <div className="flex gap-2">
            <button 
              disabled={page === 1} 
              onClick={() => setPage(p => p - 1)} 
              className="flex items-center gap-1 px-3 py-1.5 bg-white border border-slate-200 rounded-md hover:bg-slate-50 disabled:opacity-50 disabled:hover:bg-white transition-colors text-sm font-medium text-slate-600 shadow-sm"
            >
              <ChevronLeft size={16} />
              Prev
            </button>
            <button 
              disabled={!data?.data || page >= data.data.totalPages} 
              onClick={() => setPage(p => p + 1)} 
              className="flex items-center gap-1 px-3 py-1.5 bg-white border border-slate-200 rounded-md hover:bg-slate-50 disabled:opacity-50 disabled:hover:bg-white transition-colors text-sm font-medium text-slate-600 shadow-sm"
            >
              Next
              <ChevronRight size={16} />
            </button>
          </div>
        </div>
      </div>

      {showModal && (
        <div className="fixed inset-0 bg-slate-900/40 flex items-center justify-center z-50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg border border-slate-100 overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                <div className="flex items-center gap-2 text-slate-800">
                  <UserCog size={20} className="text-blue-600" />
                  <h2 className="text-lg font-bold">Add New Employee</h2>
                </div>
                <button onClick={() => setShowModal(false)} className="text-slate-400 hover:text-slate-700 bg-white hover:bg-slate-100 p-1 rounded-full transition-colors">
                  <X size={20} />
                </button>
            </div>
            <div className="p-6">
              <form onSubmit={handleCreate} className="space-y-4">
                <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Email Address</label>
                    <input name="email" type="email" placeholder="john.doe@company.com" required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all" />
                </div>
                <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Full Name</label>
                    <input name="fullName" placeholder="John Doe" required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all" />
                </div>
                <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Gender</label>
                    <select name="gender" required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all">
                        <option value="">Select Gender</option>
                        <option value="Male">Laki-laki (Male)</option>
                        <option value="Female">Perempuan (Female)</option>
                    </select>
                </div>
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm font-semibold text-slate-700 mb-1.5">Department</label>
                        <select name="departmentId" required value={selectedDeptId} onChange={(e) => { setSelectedDeptId(e.target.value); setSelectedPosId(''); }} className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all">
                            <option value="">Select Department</option>
                            {depts?.map((d: any) => <option key={d.id} value={d.id}>{d.name}</option>)}
                        </select>
                    </div>
                    <div>
                        <label className="block text-sm font-semibold text-slate-700 mb-1.5">Position</label>
                        <select name="positionId" required value={selectedPosId} onChange={(e) => setSelectedPosId(e.target.value)} disabled={!selectedDeptId} className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all disabled:opacity-50">
                            {!selectedDeptId && <option value="">Pilih Department terlebih dahulu</option>}
                            {selectedDeptId && <option value="">Select Position</option>}
                            {selectedDeptId && positions && (() => {
                                const selectedDeptName = depts?.find((d: any) => d.id === selectedDeptId)?.name || 'Department';
                                const deptPositions = positions.filter((p: any) => String(p.departmentId) === String(selectedDeptId)).sort((a: any, b: any) => a.level - b.level);
                                const execPositions = positions.filter((p: any) => p.departmentId === null).sort((a: any, b: any) => a.level - b.level);
                                return (
                                    <>
                                        <optgroup label={`Posisi ${selectedDeptName}`}>
                                            {deptPositions.map((p: any) => <option key={p.id} value={p.id}>{p.name} (Level {p.level})</option>)}
                                        </optgroup>
                                        <optgroup label="Posisi Eksekutif">
                                            {execPositions.map((p: any) => <option key={p.id} value={p.id}>{p.name} (Level {p.level})</option>)}
                                        </optgroup>
                                    </>
                                );
                            })()}
                        </select>
                    </div>
                </div>
                <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Hire Date</label>
                    <input name="hireDate" type="date" required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all" defaultValue={new Date().toISOString().split('T')[0]} />
                </div>
                
                <div className="flex justify-end gap-3 mt-8 pt-6 border-t border-slate-100">
                  <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border border-slate-200 bg-white rounded-lg text-slate-600 hover:bg-slate-50 font-medium transition-colors shadow-sm">Cancel</button>
                  <button type="submit" disabled={createMutation.isPending} className="px-5 py-2 bg-blue-600 text-white rounded-lg shadow-sm hover:bg-blue-700 hover:shadow-md font-medium transition-all disabled:opacity-70 flex items-center">
                    {createMutation.isPending ? 'Saving...' : 'Create Employee'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {showEditModal && editData && (
        <div className="fixed inset-0 bg-slate-900/40 flex items-center justify-center z-50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg border border-slate-100 overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                <div className="flex items-center gap-2 text-slate-800">
                  <Edit size={20} className="text-orange-600" />
                  <h2 className="text-lg font-bold">Edit Employee</h2>
                </div>
                <button onClick={() => { setShowEditModal(false); setEditData(null); }} className="text-slate-400 hover:text-slate-700 bg-white hover:bg-slate-100 p-1 rounded-full transition-colors">
                  <X size={20} />
                </button>
            </div>
            <div className="p-6">
              <form onSubmit={handleUpdate} className="space-y-4">
                <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Full Name</label>
                    <input name="fullName" defaultValue={editData.fullName} required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all" />
                </div>
                <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Gender</label>
                    <select name="gender" defaultValue={editData.gender || ''} required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all">
                        <option value="">Select Gender</option>
                        <option value="Male">Laki-laki (Male)</option>
                        <option value="Female">Perempuan (Female)</option>
                    </select>
                </div>
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm font-semibold text-slate-700 mb-1.5">Department</label>
                        <select name="departmentId" value={selectedDeptId} onChange={(e) => { setSelectedDeptId(e.target.value); setSelectedPosId(''); }} required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all">
                            {depts?.map((d: any) => <option key={d.id} value={d.id}>{d.name}</option>)}
                        </select>
                    </div>
                    <div>
                        <label className="block text-sm font-semibold text-slate-700 mb-1.5">Position</label>
                        <select name="positionId" value={selectedPosId} onChange={(e) => setSelectedPosId(e.target.value)} disabled={!selectedDeptId} required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all disabled:opacity-50">
                            {!selectedDeptId && <option value="">Pilih Department terlebih dahulu</option>}
                            {selectedDeptId && <option value="">Select Position</option>}
                            {selectedDeptId && positions && (() => {
                                const selectedDeptName = depts?.find((d: any) => d.id === selectedDeptId)?.name || 'Department';
                                const deptPositions = positions.filter((p: any) => String(p.departmentId) === String(selectedDeptId)).sort((a: any, b: any) => a.level - b.level);
                                const execPositions = positions.filter((p: any) => p.departmentId === null).sort((a: any, b: any) => a.level - b.level);
                                return (
                                    <>
                                        <optgroup label={`Posisi ${selectedDeptName}`}>
                                            {deptPositions.map((p: any) => <option key={p.id} value={p.id}>{p.name} (Level {p.level})</option>)}
                                        </optgroup>
                                        <optgroup label="Posisi Eksekutif">
                                            {execPositions.map((p: any) => <option key={p.id} value={p.id}>{p.name} (Level {p.level})</option>)}
                                        </optgroup>
                                    </>
                                );
                            })()}
                        </select>
                    </div>
                </div>
                <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Status</label>
                    <select name="status" defaultValue={editData.status} required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all">
                        <option value="Active">Active</option>
                        <option value="Inactive">Inactive</option>
                        <option value="OnLeave">OnLeave</option>
                        <option value="Terminated">Terminated</option>
                    </select>
                </div>
                
                <div className="flex justify-end gap-3 mt-8 pt-6 border-t border-slate-100">
                  <button type="button" onClick={() => { setShowEditModal(false); setEditData(null); }} className="px-4 py-2 border border-slate-200 bg-white rounded-lg text-slate-600 hover:bg-slate-50 font-medium transition-colors shadow-sm">Cancel</button>
                  <button type="submit" disabled={updateMutation.isPending} className="px-5 py-2 bg-orange-600 text-white rounded-lg shadow-sm hover:bg-orange-700 hover:shadow-md font-medium transition-all disabled:opacity-70 flex items-center">
                    {updateMutation.isPending ? 'Saving...' : 'Update Employee'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {showPasswordModal && (
        <div className="fixed inset-0 bg-slate-900/40 flex items-center justify-center z-50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md border border-slate-100 overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                <div className="flex items-center gap-2 text-slate-800">
                  <KeyRound size={20} className="text-blue-600" />
                  <h2 className="text-lg font-bold">Change Password</h2>
                </div>
                <button onClick={() => { setShowPasswordModal(false); setSelectedEmployeeId(null); }} className="text-slate-400 hover:text-slate-700 bg-white hover:bg-slate-100 p-1 rounded-full transition-colors">
                  <X size={20} />
                </button>
            </div>
            <div className="p-6">
              <form onSubmit={handleChangePassword} className="space-y-4">
                <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">New Password</label>
                    <input name="newPassword" type="password" required minLength={6} placeholder="Enter new password" className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all" />
                    <p className="text-xs text-slate-500 mt-2">Must be at least 6 characters long.</p>
                </div>
                <div className="flex justify-end gap-3 mt-8 pt-6 border-t border-slate-100">
                  <button type="button" onClick={() => { setShowPasswordModal(false); setSelectedEmployeeId(null); }} className="px-4 py-2 border border-slate-200 bg-white rounded-lg text-slate-600 hover:bg-slate-50 font-medium transition-colors shadow-sm">Cancel</button>
                  <button type="submit" disabled={changePasswordMutation.isPending} className="px-5 py-2 bg-blue-600 text-white rounded-lg shadow-sm hover:bg-blue-700 hover:shadow-md font-medium transition-all disabled:opacity-70 flex items-center">
                    {changePasswordMutation.isPending ? 'Saving...' : 'Update Password'}
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
