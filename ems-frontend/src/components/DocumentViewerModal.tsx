import React, { useState, useEffect, useRef } from 'react';
import { X, Download, Maximize, Minimize, FileText, Loader2, AlertCircle } from 'lucide-react';
import { format } from 'date-fns';

interface DocumentViewerModalProps {
  isOpen: boolean;
  onClose: () => void;
  document: any | null; // docInfo
  fetchDocumentBlob: () => Promise<Blob>; // Function to fetch blob from API
}

export const DocumentViewerModal: React.FC<DocumentViewerModalProps> = ({
  isOpen,
  onClose,
  document: docInfo,
  fetchDocumentBlob,
}) => {
  const [blobUrl, setBlobUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const modalContainerRef = useRef<HTMLDivElement>(null);

  const isPdf = docInfo?.fileName.toLowerCase().endsWith('.pdf') ?? false;
  const isImage = docInfo ? /\.(jpg|jpeg|png)$/i.test(docInfo.fileName) : false;

  useEffect(() => {
    if (!isOpen || !docInfo) {
      setError(null);
      if (blobUrl) {
        window.URL.revokeObjectURL(blobUrl);
        setBlobUrl(null);
      }
      return;
    }

    let isMounted = true;
    setLoading(true);
    setError(null);

    const load = async () => {
      try {
        const blob = await fetchDocumentBlob();
        if (!isMounted) return;

        if (!blob || blob.size === 0) {
          throw new Error('Berkas tidak ditemukan atau kosong.');
        }

        const mimeType = isPdf 
          ? 'application/pdf' 
          : (docInfo.fileName.toLowerCase().endsWith('.png') ? 'image/png' : 'image/jpeg');
          
        const typedBlob = new Blob([blob], { type: mimeType });
        const url = window.URL.createObjectURL(typedBlob);
        setBlobUrl(url);
        setLoading(false);
      } catch (err: any) {
        console.error('Error rendering document:', err);
        if (isMounted) {
          setError(err.message || 'Gagal mengambil data dokumen dari server.');
          setLoading(false);
        }
      }
    };

    load();

    return () => {
      isMounted = false;
    };
  }, [isOpen, docInfo]);

  if (!isOpen || !docInfo) return null;

  const handleDownload = () => {
    if (!blobUrl) return;
    const a = document.createElement('a');
    a.href = blobUrl;
    a.download = docInfo.fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
  };

  const toggleFullscreen = () => {
    if (!modalContainerRef.current) return;
    if (!isFullscreen) {
      if (modalContainerRef.current.requestFullscreen) {
        modalContainerRef.current.requestFullscreen();
      }
      setIsFullscreen(true);
    } else {
      if (document.exitFullscreen) {
        document.exitFullscreen();
      }
      setIsFullscreen(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm z-50 flex items-center justify-center p-3 sm:p-6 transition-all duration-300">
      <div 
        ref={modalContainerRef}
        className={`bg-[#F8FAFC] rounded-[12px] w-full flex flex-col shadow-2xl border border-[#E2E8F0] overflow-hidden animate-in zoom-in-95 duration-200 transition-all ${
          isFullscreen ? 'h-screen max-w-full rounded-none' : 'max-w-5xl h-[92vh]'
        }`}
      >
        {/* Top Control Bar - Bright Theme */}
        <div className="px-5 py-3.5 bg-white flex flex-wrap items-center justify-between gap-3 border-b border-[#E2E8F0] shrink-0">
          
          {/* File Info */}
          <div className="flex items-center gap-3 min-w-0">
            <div className="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0 border border-blue-100">
              <FileText size={20} strokeWidth={1.5} />
            </div>
            <div className="min-w-0">
              <div className="flex items-center gap-2 mb-1">
                <span className="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-slate-100 text-slate-600 border border-slate-200">
                  {docInfo.documentType}
                </span>
                <h3 className="text-sm font-bold text-[#0F172A] truncate" title={docInfo.fileName}>
                  {docInfo.fileName}
                </h3>
              </div>
              <p className="text-xs text-[#64748B]">
                {(docInfo.fileSize / 1024).toFixed(1)} KB • Diunggah {format(new Date(docInfo.uploadedAt), 'd MMM yyyy, HH:mm')}
              </p>
            </div>
          </div>

          {/* Right Controls */}
          <div className="flex items-center gap-1.5 shrink-0 ml-auto">
            <button 
              onClick={handleDownload}
              disabled={loading || !!error}
              className="px-3 py-1.5 hover:bg-slate-100 text-slate-700 rounded-lg text-sm font-medium transition-colors flex items-center gap-2 disabled:opacity-50"
              title="Unduh File"
            >
              <Download size={16} /> <span className="hidden sm:inline">Unduh</span>
            </button>
            <div className="w-px h-6 bg-slate-200 mx-1"></div>
            <button 
              onClick={toggleFullscreen}
              className="p-1.5 hover:bg-slate-100 text-slate-700 rounded-lg transition-colors"
              title={isFullscreen ? "Keluar Layar Penuh" : "Layar Penuh"}
            >
              {isFullscreen ? <Minimize size={18} /> : <Maximize size={18} />}
            </button>
            <button 
              onClick={onClose}
              className="p-1.5 hover:bg-red-50 text-slate-500 hover:text-red-600 rounded-lg transition-colors ml-1"
              title="Tutup Preview"
            >
              <X size={20} />
            </button>
          </div>
        </div>

        {/* Viewer Content Area */}
        <div className="flex-1 overflow-hidden relative flex flex-col items-center justify-center bg-[#F1F5F9]">
          
          {loading && (
            <div className="absolute inset-0 z-10 flex flex-col items-center justify-center bg-[#F1F5F9]/80 backdrop-blur-sm">
              <Loader2 size={36} className="text-blue-600 animate-spin mb-4" />
              <p className="text-sm font-semibold text-slate-700">Memuat berkas...</p>
              <p className="text-xs text-slate-500 mt-1">Mengambil dari server secara aman</p>
            </div>
          )}

          {error && (
            <div className="absolute inset-0 z-10 flex flex-col items-center justify-center bg-white">
              <div className="w-16 h-16 bg-red-50 rounded-2xl flex items-center justify-center mb-4 border border-red-100">
                <AlertCircle size={32} className="text-red-500" strokeWidth={1.5} />
              </div>
              <p className="font-bold text-base mb-2">Gagal Menampilkan Pratinjau</p>
              <p className="text-sm text-[#64748B] mb-6">{error}</p>
              <div className="flex items-center justify-center gap-3">
                <button
                  onClick={handleDownload}
                  className="px-5 py-2.5 bg-[#2563EB] hover:bg-blue-700 text-white rounded-xl text-sm font-semibold inline-flex items-center gap-2 transition-colors shadow-sm"
                >
                  <Download size={16} /> Unduh Secara Lokal
                </button>
                <button
                  onClick={onClose}
                  className="px-5 py-2.5 bg-white border border-[#E2E8F0] hover:bg-slate-50 text-[#0F172A] rounded-xl text-sm font-semibold transition-colors"
                >
                  Tutup
                </button>
              </div>
            </div>
          )}

          {/* IFrame PDF Viewer (Native Browser Viewer) */}
          {isPdf && !loading && !error && blobUrl && (
            <iframe 
              src={blobUrl} 
              title={docInfo.fileName}
              className="w-full h-full border-0"
              style={{ display: 'block' }}
            />
          )}

          {/* Image Viewer */}
          {isImage && !loading && !error && blobUrl && (
            <div className="my-auto flex items-center justify-center w-full h-full p-4">
              <img
                src={blobUrl}
                alt={docInfo.fileName}
                className="max-h-full max-w-full object-contain rounded-xl shadow-lg border border-[#E2E8F0] bg-white"
              />
            </div>
          )}

          {/* Fallback for other file types */}
          {!isPdf && !isImage && !loading && !error && (
            <div className="my-auto text-center p-8 bg-white rounded-2xl border border-[#E2E8F0] shadow-sm max-w-md w-full">
              <div className="w-16 h-16 bg-slate-50 rounded-2xl flex items-center justify-center mx-auto mb-4 border border-slate-100">
                <FileText size={32} className="text-[#64748B]" strokeWidth={1.5} />
              </div>
              <p className="text-base font-bold text-[#0F172A] mb-1">Pratinjau Tidak Tersedia</p>
              <p className="text-sm text-[#64748B] mb-6">Format file ini belum didukung untuk dibaca secara langsung (native). Silakan unduh untuk membukanya.</p>
              <button
                onClick={handleDownload}
                className="px-5 py-2.5 w-full bg-[#2563EB] hover:bg-blue-700 text-white rounded-xl text-sm font-semibold inline-flex items-center justify-center gap-2 transition-colors shadow-sm"
              >
                <Download size={16} strokeWidth={1.5} /> Unduh Berkas
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
