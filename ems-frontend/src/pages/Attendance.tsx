import { useState, useRef, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { attendanceService } from '../services/apiService';
import { toast } from 'sonner';
import { Camera, MapPin, MapPinned, ListChecks, CheckCircle, Clock, Timer, UserCircle, LogOut, Download, Calendar as CalendarIcon, X } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';

export const Attendance = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [loadingGeo, setLoadingGeo] = useState(false);
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [isCameraOn, setIsCameraOn] = useState(false);
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const startCamera = async () => {
      try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user' } });
        if (videoRef.current) {
          videoRef.current.srcObject = stream;
          setIsCameraOn(true);
        }
      } catch (err) {
        toast.error("Unable to access camera. Please allow camera permissions.");
        setIsCameraOn(false);
      }
    };
    startCamera();

    return () => {
      if (videoRef.current?.srcObject) {
        const stream = videoRef.current.srcObject as MediaStream;
        stream.getTracks().forEach(track => track.stop());
      }
    };
  }, []);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['attendances', startDate, endDate],
    queryFn: () => attendanceService.getAttendances({ page: 1, pageSize: 50, startDate: startDate || undefined, endDate: endDate || undefined })
  });

  const { data: summary } = useQuery({
    queryKey: ['attendance-summary', user?.employeeId],
    queryFn: () => attendanceService.getSummary(user?.employeeId!, new Date().getMonth() + 1, new Date().getFullYear()),
    enabled: !!user?.employeeId
  });

  const clockInMutation = useMutation({
    mutationFn: attendanceService.clockIn,
    onSuccess: () => {
      toast.success("Clock-in successful!");
      queryClient.invalidateQueries({ queryKey: ['attendances'] });
    },
    onError: (err: any) => toast.error(err.response?.data?.errors?.[0] || "Clock-in failed")
  });

  const clockOutMutation = useMutation({
    mutationFn: attendanceService.clockOut,
    onSuccess: () => {
      toast.success("Clock-out successful!");
      queryClient.invalidateQueries({ queryKey: ['attendances'] });
    },
    onError: (err: any) => toast.error(err.response?.data?.errors?.[0] || "Clock-out failed")
  });

  const handleExport = async () => {
    try {
      const blob = await attendanceService.export({ startDate: startDate || undefined, endDate: endDate || undefined });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Attendances_Export_${new Date().toISOString().split('T')[0]}.xlsx`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
    } catch (e) {
      toast.error("Failed to export attendances");
    }
  };

  const capturePhoto = (): string | null => {
    if (videoRef.current && canvasRef.current) {
      const video = videoRef.current;
      const canvas = canvasRef.current;
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      const ctx = canvas.getContext('2d');
      if (ctx) {
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
        return canvas.toDataURL('image/jpeg', 0.8);
      }
    }
    return null;
  };

  const handleClockAction = async (type: 'in' | 'out') => {
    if (!navigator.geolocation) {
      toast.error("Geolocation is not supported by your browser");
      return;
    }
    
    setLoadingGeo(true);
    
    const photoBase64 = capturePhoto();
    if (!photoBase64) {
      toast.error("Failed to capture photo. Make sure camera is enabled.");
      setLoadingGeo(false);
      return;
    }

    navigator.geolocation.getCurrentPosition(
      async (position) => {
        try {
          const lat = position.coords.latitude;
          const lon = position.coords.longitude;
          
          let locationName = "Unknown Location";
          try {
            const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lon}`);
            const data = await res.json();
            locationName = data.address?.city || data.address?.town || data.address?.village || data.address?.county || data.display_name?.split(',')[0] || "Unknown Location";
          } catch (e) {
            console.error("Geocoding failed", e);
          }

          const payload = { latitude: lat, longitude: lon, locationName, photoBase64 };
          if (type === 'in') clockInMutation.mutate(payload);
          else clockOutMutation.mutate(payload);
        } finally {
          setLoadingGeo(false);
        }
      },
      (error) => {
        setLoadingGeo(false);
        toast.error("Location access denied or failed: " + error.message);
      }
    );
  };

  const getStatusBadge = (status: string) => {
    switch(status) {
      case 'OnTime':
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700">On Time</span>;
      case 'Late':
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-700">Late</span>;
      default:
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-red-100 text-red-700">Early Leave</span>;
    }
  };

  return (
    <div className="flex flex-col min-h-full">
      {/* Page Header */}
      <div className="bg-white px-8 py-6 border-b border-slate-200">
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 max-w-7xl mx-auto w-full">
          <div>
            <h1 className="text-2xl font-bold text-slate-800">Attendance</h1>
            <div className="text-xs text-slate-500 flex items-center gap-1 mt-1 font-medium">
              <span>EMS Portal</span>
              <span className="text-slate-300">/</span>
              <span className="text-slate-600">Attendance</span>
            </div>
          </div>
        </div>
      </div>

      <div className="p-8 max-w-7xl mx-auto w-full flex-1">
        <div className="space-y-6">
          <div className="bg-white p-6 sm:p-8 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200">
            <div className="flex flex-col lg:flex-row items-center gap-8">
              
              {/* Camera Section */}
              <div className="relative flex-shrink-0">
                <div className="w-56 h-56 sm:w-64 sm:h-64 rounded-full overflow-hidden bg-slate-100 border-4 border-white shadow-md relative">
                  <video 
                    ref={videoRef} 
                    autoPlay 
                    playsInline 
                    muted 
                    className="w-full h-full object-cover"
                  />
                  <canvas ref={canvasRef} className="hidden" />
                  {!isCameraOn && (
                    <div className="absolute inset-0 flex flex-col items-center justify-center text-slate-400 bg-slate-100">
                      <Camera size={32} className="mb-2" />
                      <span className="text-sm font-medium">Camera off</span>
                    </div>
                  )}
                </div>
              </div>
              
              {/* Action Section */}
              <div className="flex-1 flex flex-col items-center lg:items-start text-center lg:text-left">
                <h2 className="text-2xl font-bold text-slate-800 tracking-tight">Live Check-in</h2>
                <p className="text-slate-500 mt-2 max-w-md">Position yourself in front of the camera and ensure your location services are enabled to clock in or out.</p>
                
                <div className="mt-6 flex flex-col sm:flex-row gap-4 w-full sm:w-auto">
                  <button 
                    disabled={loadingGeo || clockInMutation.isPending}
                    onClick={() => handleClockAction('in')} 
                    className="flex items-center justify-center gap-2 bg-blue-600 text-white px-8 py-3 rounded-md font-semibold shadow-sm hover:bg-blue-700 transition-all disabled:opacity-50 w-full sm:w-auto"
                  >
                    {loadingGeo || clockInMutation.isPending ? <Timer size={18} className="animate-pulse" /> : <Clock size={18} />}
                    Clock In
                  </button>
                  <button 
                    disabled={loadingGeo || clockOutMutation.isPending}
                    onClick={() => handleClockAction('out')} 
                    className="flex items-center justify-center gap-2 bg-slate-800 text-white px-8 py-3 rounded-md font-semibold shadow-sm hover:bg-slate-900 transition-all disabled:opacity-50 w-full sm:w-auto"
                  >
                    {loadingGeo || clockOutMutation.isPending ? <Timer size={18} className="animate-pulse" /> : <LogOut size={18} />}
                    Clock Out
                  </button>
                </div>
                
                <div className="mt-4 flex items-center gap-2 text-xs font-medium text-slate-500 bg-slate-50 px-3 py-1.5 rounded-full border border-slate-200">
                  <MapPin size={14} className="text-blue-500" />
                  Location tracking enabled
                </div>
              </div>
            </div>
          </div>

          {summary?.data && (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="bg-white p-5 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200">
                <div className="text-xs font-semibold text-slate-500 mb-1 uppercase tracking-wider">Total Present</div>
                <div className="text-3xl font-bold text-green-600">{summary.data.totalPresent}</div>
                <div className="text-xs text-slate-400 mt-1 font-medium">This month</div>
              </div>
              <div className="bg-white p-5 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200">
                <div className="text-xs font-semibold text-slate-500 mb-1 uppercase tracking-wider">Total Late</div>
                <div className="text-3xl font-bold text-yellow-600">{summary.data.totalLate}</div>
                <div className="text-xs text-slate-400 mt-1 font-medium">This month</div>
              </div>
              <div className="bg-white p-5 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200">
                <div className="text-xs font-semibold text-slate-500 mb-1 uppercase tracking-wider">Absent/Early</div>
                <div className="text-3xl font-bold text-red-600">{summary.data.totalEarlyLeave + summary.data.totalAbsent}</div>
                <div className="text-xs text-slate-400 mt-1 font-medium">This month</div>
              </div>
              <div className="bg-white p-5 rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200">
                <div className="text-xs font-semibold text-slate-500 mb-1 uppercase tracking-wider">Working Hours</div>
                <div className="text-3xl font-bold text-blue-600">{summary.data.totalWorkingHours}h</div>
                <div className="text-xs text-slate-400 mt-1 font-medium">This month</div>
              </div>
            </div>
          )}

          <div className="bg-white rounded-lg shadow-[0_1px_3px_rgba(0,0,0,0.08)] border border-slate-200 overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-200 bg-white flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
              <div className="flex items-center gap-2">
                <ListChecks size={18} className="text-slate-500" />
                <h3 className="font-semibold text-slate-800 text-lg">Recent Attendance Logs</h3>
              </div>
              <div className="flex items-center gap-3 w-full sm:w-auto">
                <div className="flex items-center gap-2 bg-white border border-slate-200 rounded-md px-3 py-1.5 shadow-sm">
                  <CalendarIcon size={16} className="text-slate-400" />
                  <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} className="border-none bg-transparent outline-none text-sm text-slate-600 w-32" />
                  <span className="text-slate-300">-</span>
                  <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} className="border-none bg-transparent outline-none text-sm text-slate-600 w-32" />
                  {(startDate || endDate) && <button onClick={() => { setStartDate(''); setEndDate(''); }} className="text-slate-400 hover:text-slate-700 p-1"><X size={14}/></button>}
                </div>
                <button onClick={handleExport} className="flex items-center gap-2 bg-white border border-slate-200 text-slate-700 px-4 py-2 rounded-md hover:bg-slate-50 transition-colors text-sm font-medium shadow-sm">
                  <Download size={16} />
                  Export
                </button>
              </div>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-slate-50 border-b border-slate-200">
                  <tr>
                    <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Employee</th>
                    <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Date</th>
                    <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Location</th>
                    <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Photo</th>
                    <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Clock In</th>
                    <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Clock Out</th>
                    <th className="px-6 py-4 font-semibold text-slate-600 uppercase tracking-wider text-xs">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {isLoading ? (
                    Array.from({ length: 4 }).map((_, i) => (
                      <tr key={i} className="animate-pulse">
                        <td className="px-6 py-4"><div className="h-4 w-32 bg-slate-200 rounded"></div></td>
                        <td className="px-6 py-4"><div className="h-4 w-24 bg-slate-200 rounded"></div></td>
                        <td className="px-6 py-4"><div className="h-4 w-24 bg-slate-200 rounded"></div></td>
                        <td className="px-6 py-4"><div className="h-10 w-10 bg-slate-200 rounded-md"></div></td>
                        <td className="px-6 py-4"><div className="h-4 w-16 bg-slate-200 rounded"></div></td>
                        <td className="px-6 py-4"><div className="h-4 w-16 bg-slate-200 rounded"></div></td>
                        <td className="px-6 py-4"><div className="h-6 w-16 bg-slate-200 rounded-full"></div></td>
                      </tr>
                    ))
                  ) : isError ? (
                    <tr>
                      <td colSpan={7} className="px-6 py-12 text-center text-red-600 font-medium">
                        {(error as any)?.response?.data?.message || (error as any)?.message || 'Failed to load data.'}
                      </td>
                    </tr>
                  ) : !data?.data?.data || data.data.data.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="px-6 py-24">
                        <div className="flex flex-col items-center justify-center space-y-4">
                          <div className="w-20 h-20 bg-slate-50 rounded-full flex items-center justify-center">
                            <ListChecks size={40} className="text-slate-300" />
                          </div>
                          <div className="text-center">
                            <h3 className="text-lg font-semibold text-slate-800 mb-1">Belum ada log absensi</h3>
                            <p className="text-sm text-slate-500">Anda belum melakukan check-in atau check-out.</p>
                          </div>
                        </div>
                      </td>
                    </tr>
                  ) : (
                    data.data.data.map((att: any) => (
                      <tr key={att.id} className="hover:bg-slate-50/50 transition-colors">
                        <td className="px-6 py-4 font-semibold text-slate-900">{att.employeeName}</td>
                        <td className="px-6 py-4 font-medium">{new Date(att.clockIn).toLocaleDateString()}</td>
                        <td className="px-6 py-4">
                          <div className="flex items-center gap-1.5 text-slate-600 text-xs max-w-[150px] truncate" title={att.locationName}>
                            <MapPinned size={14} className="text-slate-400 shrink-0" />
                            <span className="truncate">{att.locationName || '-'}</span>
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          {att.photoUrl ? (
                            <img src={`http://localhost:5000${att.photoUrl}`} alt="Clock In" className="h-10 w-10 rounded-md object-cover border border-slate-200 shadow-sm" />
                          ) : <span className="text-slate-400 text-xs">-</span>}
                        </td>
                        <td className="px-6 py-4 font-medium text-emerald-600">
                          {new Date(att.clockIn).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}
                        </td>
                        <td className="px-6 py-4 font-medium text-amber-600">
                          {att.clockOut ? new Date(att.clockOut).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'}) : '-'}
                        </td>
                        <td className="px-6 py-4">
                          {getStatusBadge(att.status)}
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
    </div>
  );
};
