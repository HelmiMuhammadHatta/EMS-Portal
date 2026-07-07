import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { departmentService, positionService, roleService } from '../services/apiService';
import { useAuth } from '../hooks/useAuth';
import { toast } from 'sonner';
import { Briefcase, Building2, Plus, Trash2, Shield, Key } from 'lucide-react';

export const Settings = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'departments' | 'positions' | 'roles' | 'permissions'>('departments');
  const [showModal, setShowModal] = useState(false);
  const [selectedRole, setSelectedRole] = useState<any>(null);
  
  const { data: departments, isLoading: loadingDepts } = useQuery({
    queryKey: ['departments'],
    queryFn: departmentService.getAll,
    enabled: activeTab === 'departments'
  });

  const { data: positions, isLoading: loadingPos } = useQuery({
    queryKey: ['positions'],
    queryFn: positionService.getAll,
    enabled: activeTab === 'positions'
  });

  const { data: roles, isLoading: loadingRoles } = useQuery({
    queryKey: ['roles'],
    queryFn: roleService.getRoles,
    enabled: activeTab === 'roles' || activeTab === 'permissions'
  });

  const { data: permissions, isLoading: loadingPerms } = useQuery({
    queryKey: ['permissions'],
    queryFn: roleService.getPermissions,
    enabled: activeTab === 'permissions' || !!selectedRole
  });

  const { data: rolePermissions } = useQuery({
    queryKey: ['role-permissions', selectedRole?.id],
    queryFn: () => roleService.getRolePermissions(selectedRole.id),
    enabled: !!selectedRole
  });

  const createDeptMutation = useMutation({
    mutationFn: departmentService.create,
    onSuccess: () => {
      toast.success("Department created successfully!");
      queryClient.invalidateQueries({ queryKey: ['departments'] });
      setShowModal(false);
    },
    onError: () => toast.error("Failed to create department")
  });

  const deleteDeptMutation = useMutation({
    mutationFn: departmentService.delete,
    onSuccess: () => {
      toast.success("Department deleted successfully!");
      queryClient.invalidateQueries({ queryKey: ['departments'] });
    },
    onError: () => toast.error("Failed to delete department")
  });

  const createPosMutation = useMutation({
    mutationFn: positionService.create,
    onSuccess: () => {
      toast.success("Position created successfully!");
      queryClient.invalidateQueries({ queryKey: ['positions'] });
      setShowModal(false);
    },
    onError: () => toast.error("Failed to create position")
  });

  const deletePosMutation = useMutation({
    mutationFn: positionService.delete,
    onSuccess: () => {
      toast.success("Position deleted successfully!");
      queryClient.invalidateQueries({ queryKey: ['positions'] });
    },
    onError: () => toast.error("Failed to delete position")
  });

  const createRoleMutation = useMutation({
    mutationFn: roleService.createRole,
    onSuccess: () => {
      toast.success("Role created successfully!");
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      setShowModal(false);
    },
    onError: () => toast.error("Failed to create role")
  });

  const assignPermissionsMutation = useMutation({
    mutationFn: ({ roleId, data }: { roleId: string, data: any }) => roleService.assignPermissions(roleId, data),
    onSuccess: () => {
      toast.success("Permissions assigned successfully!");
      queryClient.invalidateQueries({ queryKey: ['role-permissions'] });
      setSelectedRole(null);
    },
    onError: () => toast.error("Failed to assign permissions")
  });

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const data = Object.fromEntries(formData.entries());
    
    if (activeTab === 'departments') {
      createDeptMutation.mutate({ name: data.name });
    } else if (activeTab === 'positions') {
      createPosMutation.mutate({ name: data.name, level: Number(data.level) });
    } else if (activeTab === 'roles') {
      createRoleMutation.mutate({ name: data.name });
    }
  };

  const handleDelete = (id: string) => {
    if (!window.confirm(`Are you sure you want to delete this ${activeTab === 'departments' ? 'department' : 'position'}?`)) return;
    
    if (activeTab === 'departments') {
      deleteDeptMutation.mutate(id);
    } else {
      deletePosMutation.mutate(id);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div></div>
        {(activeTab === 'departments' || activeTab === 'positions' || activeTab === 'roles') && !selectedRole && (
          <button onClick={() => setShowModal(true)} className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg shadow-sm shadow-blue-200 hover:bg-blue-700 font-medium transition-all">
            <Plus size={18} />
            Add {activeTab === 'departments' ? 'Department' : activeTab === 'positions' ? 'Position' : 'Role'}
          </button>
        )}
      </div>

      {!selectedRole ? (
        <>
          <div className="flex space-x-2 border-b border-slate-200 mb-6 overflow-x-auto pb-1">
            <button 
              className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-2 whitespace-nowrap ${
                activeTab === 'departments' 
                  ? 'border-blue-600 text-blue-600' 
                  : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'
              }`}
              onClick={() => setActiveTab('departments')}
            >
              <Building2 size={16} />
              Departments
            </button>
            <button 
              className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-2 whitespace-nowrap ${
                activeTab === 'positions' 
                  ? 'border-blue-600 text-blue-600' 
                  : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'
              }`}
              onClick={() => setActiveTab('positions')}
            >
              <Briefcase size={16} />
              Positions
            </button>
            {user?.role === 'Admin' && (
              <>
                <button 
                  className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-2 whitespace-nowrap ${
                    activeTab === 'roles' 
                      ? 'border-blue-600 text-blue-600' 
                      : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'
                  }`}
                  onClick={() => setActiveTab('roles')}
                >
                  <Shield size={16} />
                  Roles
                </button>
                <button 
                  className={`flex items-center gap-2 pb-3 px-4 text-sm font-semibold transition-colors border-b-2 whitespace-nowrap ${
                    activeTab === 'permissions' 
                      ? 'border-blue-600 text-blue-600' 
                      : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'
                  }`}
                  onClick={() => setActiveTab('permissions')}
                >
                  <Key size={16} />
                  Permissions
                </button>
              </>
            )}
          </div>

      <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Name</th>
                {activeTab === 'positions' && <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Level</th>}
                {activeTab === 'permissions' && <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs">Description</th>}
                {activeTab !== 'permissions' && <th className="px-6 py-4 font-semibold text-slate-500 uppercase tracking-wider text-xs text-right">Actions</th>}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {activeTab === 'departments' ? (
                loadingDepts ? (
                  <tr><td colSpan={2} className="px-6 py-8 text-center text-slate-400">Loading departments...</td></tr>
                ) : departments?.length === 0 ? (
                  <tr><td colSpan={2} className="px-6 py-8 text-center text-slate-400">No departments found</td></tr>
                ) : (
                  departments?.map((dept: any) => (
                    <tr key={dept.id} className="hover:bg-slate-50 transition-colors">
                      <td className="px-6 py-4 font-medium text-slate-900">{dept.name}</td>
                      <td className="px-6 py-4 text-right">
                        <button onClick={() => handleDelete(dept.id)} className="text-red-500 hover:text-red-700 p-1 rounded-md hover:bg-red-50 transition-colors">
                          <Trash2 size={16} />
                        </button>
                      </td>
                    </tr>
                  ))
                )
              ) : activeTab === 'positions' ? (
                loadingPos ? (
                  <tr><td colSpan={3} className="px-6 py-8 text-center text-slate-400">Loading positions...</td></tr>
                ) : positions?.length === 0 ? (
                  <tr><td colSpan={3} className="px-6 py-8 text-center text-slate-400">No positions found</td></tr>
                ) : (
                  positions?.map((pos: any) => (
                    <tr key={pos.id} className="hover:bg-slate-50 transition-colors">
                      <td className="px-6 py-4 font-medium text-slate-900">{pos.name}</td>
                      <td className="px-6 py-4 text-slate-600">Level {pos.level}</td>
                      <td className="px-6 py-4 text-right">
                        <button onClick={() => handleDelete(pos.id)} className="text-red-500 hover:text-red-700 p-1 rounded-md hover:bg-red-50 transition-colors">
                          <Trash2 size={16} />
                        </button>
                      </td>
                    </tr>
                  ))
                )
              ) : activeTab === 'roles' ? (
                loadingRoles ? (
                  <tr><td colSpan={2} className="px-6 py-8 text-center text-slate-400">Loading roles...</td></tr>
                ) : roles?.length === 0 ? (
                  <tr><td colSpan={2} className="px-6 py-8 text-center text-slate-400">No roles found</td></tr>
                ) : (
                  roles?.map((role: any) => (
                    <tr key={role.id} className="hover:bg-slate-50 transition-colors cursor-pointer" onClick={() => setSelectedRole(role)}>
                      <td className="px-6 py-4 font-medium text-slate-900">{role.name}</td>
                      <td className="px-6 py-4 text-right">
                        <button onClick={(e) => { e.stopPropagation(); setSelectedRole(role); }} className="text-blue-600 hover:text-blue-800 p-1 rounded-md hover:bg-blue-50 transition-colors text-sm font-semibold">
                          Edit Permissions
                        </button>
                      </td>
                    </tr>
                  ))
                )
              ) : (
                loadingPerms ? (
                  <tr><td colSpan={2} className="px-6 py-8 text-center text-slate-400">Loading permissions...</td></tr>
                ) : permissions?.length === 0 ? (
                  <tr><td colSpan={2} className="px-6 py-8 text-center text-slate-400">No permissions found</td></tr>
                ) : (
                  permissions?.map((perm: any) => {
                    let desc = "Access related to " + perm.name.split('.')[0] + " management";
                    if (perm.name.includes("read")) desc = `View ${perm.name.split('.')[0]} data`;
                    if (perm.name.includes("write")) desc = `Create and modify ${perm.name.split('.')[0]} data`;
                    if (perm.name.includes("delete")) desc = `Delete ${perm.name.split('.')[0]} data`;
                    if (perm.name === "leave.approve") desc = "Approve or reject leave requests";
                    return (
                      <tr key={perm.id} className="hover:bg-slate-50 transition-colors">
                        <td className="px-6 py-4 font-medium text-slate-900">
                          <span className="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-200">{perm.name}</span>
                        </td>
                        <td className="px-6 py-4 text-slate-600 text-sm">{desc}</td>
                      </tr>
                    );
                  })
                )
              )}
            </tbody>
          </table>
        </div>
      </div>
      </>
      ) : (
        <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden p-6">
          <div className="flex justify-between items-center mb-6">
            <h2 className="text-lg font-bold text-slate-800">Edit Permissions for {selectedRole.name}</h2>
            <button onClick={() => setSelectedRole(null)} className="text-slate-500 hover:text-slate-700 bg-slate-50 px-3 py-1.5 rounded-md text-sm font-semibold border border-slate-200">Back</button>
          </div>
          
          <form onSubmit={(e) => {
            e.preventDefault();
            const formData = new FormData(e.currentTarget);
            const selectedPerms = permissions?.filter((p: any) => formData.get(`perm_${p.id}`) === 'on').map((p: any) => p.id) || [];
            assignPermissionsMutation.mutate({ roleId: selectedRole.id, data: { permissionIds: selectedPerms } });
          }}>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
              {permissions?.map((perm: any) => (
                <label key={perm.id} className="flex items-center gap-3 p-3 border border-slate-200 rounded-lg cursor-pointer hover:bg-slate-50 transition-colors">
                  <input 
                    type="checkbox" 
                    name={`perm_${perm.id}`} 
                    defaultChecked={rolePermissions?.includes(perm.id)}
                    className="w-4 h-4 text-blue-600 rounded border-slate-300 focus:ring-blue-500" 
                  />
                  <span className="text-sm font-medium text-slate-700">{perm.name}</span>
                </label>
              ))}
            </div>
            
            <div className="flex justify-end pt-4 border-t border-slate-100">
              <button type="submit" disabled={assignPermissionsMutation.isPending} className="px-6 py-2.5 bg-blue-600 text-white rounded-lg shadow-sm hover:bg-blue-700 font-medium transition-all disabled:opacity-70">
                Save Permissions
              </button>
            </div>
          </form>
        </div>
      )}

      {showModal && !selectedRole && (
        <div className="fixed inset-0 bg-slate-900/40 flex items-center justify-center z-50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md border border-slate-100 overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                <h2 className="text-lg font-bold text-slate-800">Add {activeTab === 'departments' ? 'Department' : activeTab === 'positions' ? 'Position' : 'Role'}</h2>
                <button onClick={() => setShowModal(false)} className="text-slate-400 hover:text-slate-700 bg-white hover:bg-slate-100 p-1 rounded-full transition-colors">&times;</button>
            </div>
            <div className="p-6">
              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-1.5">Name</label>
                  <input name="name" type="text" required className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all" />
                </div>
                {activeTab === 'positions' && (
                  <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Level</label>
                    <input name="level" type="number" required min="1" className="w-full border border-slate-200 bg-slate-50 p-2.5 rounded-lg focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all" />
                  </div>
                )}
                <div className="flex justify-end gap-3 mt-8 pt-6 border-t border-slate-100">
                  <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border border-slate-200 bg-white rounded-lg text-slate-600 hover:bg-slate-50 font-medium transition-colors">Cancel</button>
                  <button type="submit" disabled={createDeptMutation.isPending || createPosMutation.isPending || createRoleMutation.isPending} className="px-5 py-2 bg-blue-600 text-white rounded-lg shadow-sm hover:bg-blue-700 font-medium transition-all disabled:opacity-70">
                    Save
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
