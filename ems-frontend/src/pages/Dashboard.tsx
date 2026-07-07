import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../hooks/useAuth';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { employeeService, leaveService, attendanceService } from '../services/apiService';
import { useMemo } from 'react';
import { Users, CalendarClock, Activity } from 'lucide-react';

export const Dashboard = () => {
  const { user } = useAuth();
  
  const { data: employeesData } = useQuery({
    queryKey: ['employees', 'all'],
    queryFn: () => employeeService.getAll({ page: 1, pageSize: 1000 })
  });

  const { data: leavesData } = useQuery({
    queryKey: ['leaves', 'all'],
    queryFn: () => leaveService.getRequests()
  });

  const { data: attendancesData } = useQuery({
    queryKey: ['attendances', 'recent'],
    queryFn: () => attendanceService.getAttendances({ page: 1, pageSize: 1000 })
  });

  const metrics = useMemo(() => {
    const totalEmployees = employeesData?.data?.totalCount || 0;
    
    // Pending leaves
    const pendingLeaves = leavesData?.data?.data?.filter((l: any) => l.status === 'Pending').length || 0;

    // Today's attendances
    const todayStr = new Date().toLocaleDateString();
    const todayAttendances = attendancesData?.data?.data?.filter((a: any) => new Date(a.clockIn).toLocaleDateString() === todayStr) || [];
    
    const uniqueAttendees = new Set(todayAttendances.map((a: any) => a.employeeId)).size;
    
    const attendanceRate = totalEmployees > 0 ? Math.round((uniqueAttendees / totalEmployees) * 100) : 0;

    // By Department
    const employeesList = employeesData?.data?.data || [];
    const deptStats: Record<string, { total: number; attended: number }> = {};
    
    employeesList.forEach((emp: any) => {
      const deptName = emp.departmentName || 'IT Department';
      if (!deptStats[deptName]) deptStats[deptName] = { total: 0, attended: 0 };
      deptStats[deptName].total += 1;
      if (todayAttendances.some((a: any) => a.employeeId === emp.id)) {
        deptStats[deptName].attended += 1;
      }
    });

    const chartData = Object.entries(deptStats).map(([name, stats]) => ({
      name,
      rate: stats.total > 0 ? Math.round((stats.attended / stats.total) * 100) : 0
    }));

    return { totalEmployees, pendingLeaves, attendanceRate, chartData };
  }, [employeesData, leavesData, attendancesData]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          {/* Handled by Layout */}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200 relative overflow-hidden group">
          <div className="absolute -right-6 -top-6 text-blue-50 opacity-50 group-hover:scale-110 transition-transform">
            <Users size={120} />
          </div>
          <div className="relative z-10">
            <div className="flex items-center gap-2 text-slate-500 font-semibold mb-2">
              <Users size={18} className="text-blue-500" />
              <span>Total Employees</span>
            </div>
            <div className="flex items-baseline gap-2">
              <h3 className="text-4xl font-bold text-slate-900 tracking-tight">{metrics.totalEmployees}</h3>
              <span className="text-sm font-medium text-slate-400">active</span>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200 relative overflow-hidden group">
          <div className="absolute -right-6 -top-6 text-green-50 opacity-50 group-hover:scale-110 transition-transform">
            <Activity size={120} />
          </div>
          <div className="relative z-10">
            <div className="flex items-center gap-2 text-slate-500 font-semibold mb-2">
              <Activity size={18} className="text-green-500" />
              <span>Attendance Rate Today</span>
            </div>
            <div className="flex items-baseline gap-2">
              <h3 className="text-4xl font-bold text-slate-900 tracking-tight">{metrics.attendanceRate}%</h3>
              <span className="text-sm font-medium text-slate-400">present</span>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200 relative overflow-hidden group">
          <div className="absolute -right-6 -top-6 text-yellow-50 opacity-50 group-hover:scale-110 transition-transform">
            <CalendarClock size={120} />
          </div>
          <div className="relative z-10">
            <div className="flex items-center gap-2 text-slate-500 font-semibold mb-2">
              <CalendarClock size={18} className="text-yellow-500" />
              <span>Pending Leaves</span>
            </div>
            <div className="flex items-baseline gap-2">
              <h3 className="text-4xl font-bold text-slate-900 tracking-tight">{metrics.pendingLeaves}</h3>
              <span className="text-sm font-medium text-slate-400">requests</span>
            </div>
          </div>
        </div>
      </div>

      <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200 mt-8">
        <h2 className="text-lg font-bold mb-6 text-slate-800">Attendance Rate by Department</h2>
        <div className="h-72 w-full">
          {metrics.chartData.length > 0 ? (
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={metrics.chartData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
                <XAxis dataKey="name" tick={{fill: '#64748b', fontSize: 12}} tickLine={false} axisLine={{stroke: '#e2e8f0'}} />
                <YAxis tick={{fill: '#64748b', fontSize: 12}} tickLine={false} axisLine={false} domain={[0, 100]} tickFormatter={(val) => `${val}%`} />
                <Tooltip 
                  cursor={{fill: '#f8fafc'}} 
                  contentStyle={{borderRadius: '8px', border: '1px solid #e2e8f0', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)'}}
                  formatter={(value) => [`${value}%`, 'Attendance Rate']}
                />
                <Bar dataKey="rate" fill="#3b82f6" radius={[6, 6, 0, 0]} maxBarSize={60} />
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <div className="w-full h-full flex flex-col items-center justify-center text-slate-400">
              <Activity size={32} className="mb-2 opacity-50" />
              <p>Not enough data to display chart</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
