import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../hooks/useAuth';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line, PieChart, Pie, Cell, Legend } from 'recharts';
import { employeeService, leaveService, attendanceService } from '../services/apiService';
import { useMemo } from 'react';
import { Users, CalendarClock, Activity } from 'lucide-react';

export const Dashboard = () => {
  const { user } = useAuth();
  
  // Greeting based on time
  const getGreeting = () => {
    const hour = new Date().getHours();
    if (hour >= 11 && hour < 15) return 'siang';
    if (hour >= 15 && hour < 18) return 'sore';
    if (hour >= 18) return 'malam';
    return 'pagi';
  };
  
  const todayDateStr = new Date().toLocaleDateString('id-ID', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
  
  const { data: employeesData } = useQuery({
    queryKey: ['employees', 'all'],
    queryFn: () => employeeService.getAll({ page: 1, pageSize: 1000 }),
    enabled: user?.role === 'Admin' || user?.role === 'Manager'
  });

  const displayName = user?.fullName ?? user?.email?.split('@')[0] ?? 'User';

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

    const attendanceDistribution = [
      { name: 'On Time', value: onTimeCount, color: '#16a34a' }, // Tailwind green-600 #16a34a
      { name: 'Late', value: lateCount, color: '#ca8a04' } // Tailwind yellow-600 #ca8a04
    ].filter(d => d.value > 0);

    return { totalEmployees, pendingLeaves, attendanceRate, chartData, leaveTrend, attendanceDistribution };
  }, [employeesData, leavesData, attendancesData]);

  return (
    <div className="flex flex-col min-h-full">
      {/* Page Header */}
      <div className="bg-white px-8 py-6 border-b border-slate-200">
        <div className="max-w-7xl mx-auto w-full">
          <h1 className="text-2xl font-bold text-slate-800">Selamat {getGreeting()}, {displayName} 👋</h1>
          <div className="text-xs text-slate-500 flex items-center gap-1 mt-1 font-medium">
            <span>{todayDateStr}</span>
          </div>
        </div>
      </div>

      <div className="p-8 max-w-7xl mx-auto w-full flex-1">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="bg-white p-6 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200 relative overflow-hidden group hover:shadow-md transition-all duration-300">
            <div className="absolute -right-6 -top-6 text-blue-50 opacity-50 group-hover:scale-110 transition-transform duration-500">
              <Users size={120} />
            </div>
            <div className="relative z-10">
              <div className="flex items-center gap-3 text-slate-500 font-semibold mb-4">
                <div className="p-2 bg-blue-100 rounded-full">
                  <Users size={20} className="text-blue-600" />
                </div>
                <span>Total Employees</span>
              </div>
              <div className="flex items-baseline gap-2">
                <h3 className="text-4xl font-bold text-slate-900 tracking-tight">{metrics.totalEmployees}</h3>
                <span className="text-sm font-medium text-slate-400">active</span>
              </div>
            </div>
          </div>

          <div className="bg-white p-6 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200 relative overflow-hidden group hover:shadow-md transition-all duration-300">
            <div className="absolute -right-6 -top-6 text-green-50 opacity-50 group-hover:scale-110 transition-transform duration-500">
              <Activity size={120} />
            </div>
            <div className="relative z-10">
              <div className="flex items-center gap-3 text-slate-500 font-semibold mb-4">
                <div className="p-2 bg-green-100 rounded-full">
                  <Activity size={20} className="text-green-600" />
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

          <div className="bg-white p-6 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200 relative overflow-hidden group hover:shadow-md transition-all duration-300">
            <div className="absolute -right-6 -top-6 text-yellow-50 opacity-50 group-hover:scale-110 transition-transform duration-500">
              <CalendarClock size={120} />
            </div>
            <div className="relative z-10">
              <div className="flex items-center gap-3 text-slate-500 font-semibold mb-4">
                <div className="p-2 bg-yellow-100 rounded-full">
                  <CalendarClock size={20} className="text-yellow-600" />
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

        <div className="bg-white p-8 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200 mt-6">
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
                    contentStyle={{borderRadius: '8px', border: '1px solid #e2e8f0', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)', padding: '12px'}}
                    formatter={(value: number, name: string, props: any) => [`${value}% (${props.payload?.name})`, 'Attendance Rate']}
                  />
                  <Bar dataKey="rate" fill="#2563eb" radius={[4, 4, 0, 0]} maxBarSize={60} />
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
          <div className="bg-white p-8 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200">
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
                    <Line type="monotone" dataKey="total" name="Requests" stroke="#2563eb" strokeWidth={3} dot={{r: 4, fill: '#2563eb', strokeWidth: 0}} activeDot={{r: 6}} />
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
          <div className="bg-white p-8 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200">
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
    </div>
  );
};
