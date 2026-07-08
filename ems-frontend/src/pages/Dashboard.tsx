import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../hooks/useAuth';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line, PieChart, Pie, Cell, Legend } from 'recharts';
import { employeeService, leaveService, attendanceService } from '../services/apiService';
import { useMemo } from 'react';
import { Users, CalendarClock, Activity } from 'lucide-react';

export const Dashboard = () => {
  const { user } = useAuth();
  
  // Greeting based on time
  const hour = new Date().getHours();
  let greeting = 'Selamat pagi';
  if (hour >= 11 && hour < 15) greeting = 'Selamat siang';
  else if (hour >= 15 && hour < 18) greeting = 'Selamat sore';
  else if (hour >= 18) greeting = 'Selamat malam';
  
  const todayDateStr = new Date().toLocaleDateString('id-ID', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
  
  const { data: employeesData } = useQuery({
    queryKey: ['employees', 'all'],
    queryFn: () => employeeService.getAll({ page: 1, pageSize: 1000 }),
    enabled: user?.role === 'Admin' || user?.role === 'Manager'
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

    // Leave Trend (Last 6 Months)
    const leavesList = leavesData?.data?.data || [];
    const leaveTrend = [];
    for (let i = 5; i >= 0; i--) {
      const d = new Date();
      d.setMonth(d.getMonth() - i);
      const monthStr = d.toLocaleDateString('id-ID', { month: 'short' });
      const yearStr = d.getFullYear();
      
      const count = leavesList.filter((l: any) => {
        const ld = new Date(l.createdAt);
        return ld.getMonth() === d.getMonth() && ld.getFullYear() === d.getFullYear();
      }).length;
      
      leaveTrend.push({ name: `${monthStr} ${yearStr}`, total: count });
    }

    // Attendance Status Distribution (This Month)
    const attendancesList = attendancesData?.data?.data || [];
    const thisMonth = new Date().getMonth();
    const thisYear = new Date().getFullYear();
    const thisMonthAttendances = attendancesList.filter((a: any) => {
      const d = new Date(a.clockIn);
      return d.getMonth() === thisMonth && d.getFullYear() === thisYear;
    });

    let onTimeCount = 0;
    let lateCount = 0;
    
    thisMonthAttendances.forEach((a: any) => {
      if (a.status === 'OnTime') onTimeCount++;
      else if (a.status === 'Late') lateCount++;
    });

    // We can estimate absent if we have employees count and working days, but let's just show OnTime and Late for simplicity if Absent is not explicitly tracked as records
    const attendanceDistribution = [
      { name: 'On Time', value: onTimeCount, color: '#22c55e' },
      { name: 'Late', value: lateCount, color: '#eab308' }
    ].filter(d => d.value > 0);

    return { totalEmployees, pendingLeaves, attendanceRate, chartData, leaveTrend, attendanceDistribution };
  }, [employeesData, leavesData, attendancesData]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">{greeting}, {user?.name?.split(' ')[0] || 'User'} 👋</h1>
          <p className="text-sm font-medium text-slate-500 mt-1">{todayDateStr}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200 relative overflow-hidden group">
          <div className="absolute -right-6 -top-6 text-blue-50 opacity-50 group-hover:scale-110 transition-transform">
            <Users size={120} />
          </div>
          <div className="relative z-10">
            <div className="flex items-center gap-2 text-slate-500 font-semibold mb-2">
              <div className="p-1.5 bg-blue-50 rounded-md">
                <Users size={18} className="text-blue-500" />
              </div>
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
              <div className="p-1.5 bg-green-50 rounded-md">
                <Activity size={18} className="text-green-500" />
              </div>
              <span>Attendance Rate Today</span>
            </div>
            <div className="flex items-baseline gap-2">
              <h3 className="text-4xl font-bold text-slate-900 tracking-tight">
                {metrics.chartData.every(d => d.rate === 0) && metrics.attendanceRate === 0 ? '--' : metrics.attendanceRate}%
              </h3>
              <span className="text-sm font-medium text-slate-400">present</span>
            </div>
            {metrics.chartData.every(d => d.rate === 0) && metrics.attendanceRate === 0 && (
              <p className="text-xs text-slate-400 mt-2 font-medium">Belum ada data absensi hari ini</p>
            )}
          </div>
        </div>

        <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200 relative overflow-hidden group">
          <div className="absolute -right-6 -top-6 text-yellow-50 opacity-50 group-hover:scale-110 transition-transform">
            <CalendarClock size={120} />
          </div>
          <div className="relative z-10">
            <div className="flex items-center gap-2 text-slate-500 font-semibold mb-2">
              <div className="p-1.5 bg-yellow-50 rounded-md">
                <CalendarClock size={18} className="text-yellow-500" />
              </div>
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
          {metrics.chartData.some(d => d.rate > 0) ? (
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
              <div className="p-4 bg-slate-50 rounded-full mb-3">
                <Activity size={32} className="text-slate-300" />
              </div>
              <p className="font-medium text-slate-500">Belum ada data kehadiran untuk hari ini</p>
              <p className="text-sm mt-1">Data grafik akan muncul setelah karyawan melakukan clock-in.</p>
            </div>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
        {/* Leave Trend Chart */}
        <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200">
          <h2 className="text-lg font-bold mb-6 text-slate-800">Leave Trend (6 Months)</h2>
          <div className="h-72 w-full">
            {metrics.leaveTrend.some(d => d.total > 0) ? (
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={metrics.leaveTrend} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
                  <XAxis dataKey="name" tick={{fill: '#64748b', fontSize: 12}} tickLine={false} axisLine={{stroke: '#e2e8f0'}} />
                  <YAxis tick={{fill: '#64748b', fontSize: 12}} tickLine={false} axisLine={false} allowDecimals={false} />
                  <Tooltip 
                    cursor={{stroke: '#e2e8f0', strokeWidth: 2}} 
                    contentStyle={{borderRadius: '8px', border: '1px solid #e2e8f0', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)'}}
                  />
                  <Line type="monotone" dataKey="total" name="Requests" stroke="#8b5cf6" strokeWidth={3} dot={{r: 4, fill: '#8b5cf6', strokeWidth: 0}} activeDot={{r: 6}} />
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="w-full h-full flex flex-col items-center justify-center text-slate-400">
                <div className="p-4 bg-slate-50 rounded-full mb-3">
                  <CalendarClock size={32} className="text-slate-300" />
                </div>
                <p className="font-medium text-slate-500">Belum ada pengajuan cuti</p>
                <p className="text-sm mt-1">Data tren akan muncul setelah ada pengajuan cuti.</p>
              </div>
            )}
          </div>
        </div>

        {/* Attendance Distribution Chart */}
        <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200">
          <h2 className="text-lg font-bold mb-6 text-slate-800">Attendance Status (This Month)</h2>
          <div className="h-72 w-full">
            {metrics.attendanceDistribution.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={metrics.attendanceDistribution}
                    cx="50%"
                    cy="50%"
                    innerRadius={70}
                    outerRadius={100}
                    paddingAngle={5}
                    dataKey="value"
                  >
                    {metrics.attendanceDistribution.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip 
                    contentStyle={{borderRadius: '8px', border: '1px solid #e2e8f0', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)'}}
                  />
                  <Legend verticalAlign="bottom" height={36} iconType="circle" />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="w-full h-full flex flex-col items-center justify-center text-slate-400">
                <div className="p-4 bg-slate-50 rounded-full mb-3">
                  <Activity size={32} className="text-slate-300" />
                </div>
                <p className="font-medium text-slate-500">Belum ada data kehadiran bulan ini</p>
                <p className="text-sm mt-1">Diagram akan muncul setelah karyawan melakukan clock-in.</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
