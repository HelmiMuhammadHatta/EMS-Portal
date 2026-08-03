import React, { useState, useEffect, useRef } from 'react';
import * as pdfjsLib from 'pdfjs-dist';
import { 
  X, 
  Download, 
  ExternalLink, 
  ZoomIn, 
  ZoomOut, 
  RotateCw, 
  ChevronLeft, 
  ChevronRight, 
  FileText, 
  Loader2, 
  Maximize2, 
  Minimize2,
  AlertCircle
} from 'lucide-react';
import { format } from 'date-fns';

// Set up PDF.js worker
pdfjsLib.GlobalWorkerOptions.workerSrc = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${pdfjsLib.version || '3.11.174'}/pdf.worker.min.js`;

interface DocumentViewerModalProps {
  isOpen: boolean;
  onClose: () => void;
  document: {
    id: string;
    fileName: string;
    documentType: string;
    fileSize: number;
    uploadedAt: string;
  } | null;
  fetchDocumentBlob: () => Promise<Blob>;
}

export const DocumentViewerModal: React.FC<DocumentViewerModalProps> = ({
  isOpen,
  onClose,
  document: docInfo,
  fetchDocumentBlob
}) => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pdfDoc, setPdfDoc] = useState<pdfjsLib.PDFDocumentProxy | null>(null);
  const [numPages, setNumPages] = useState<number>(0);
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [scale, setScale] = useState<number>(1.2);
  const [rotation, setRotation] = useState<number>(0);
  const [blobUrl, setBlobUrl] = useState<string | null>(null);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [viewMode, setViewMode] = useState<'all' | 'single'>('all');

  const canvasContainerRef = useRef<HTMLDivElement>(null);
  const modalContainerRef = useRef<HTMLDivElement>(null);
  const renderTasksRef = useRef<{ [key: number]: any }>({});

  const isPdf = docInfo?.fileName.toLowerCase().endsWith('.pdf') ?? false;
  const isImage = docInfo ? /\.(jpg|jpeg|png)$/i.test(docInfo.fileName) : false;

  // Load Document
  useEffect(() => {
    if (!isOpen || !docInfo) {
      setPdfDoc(null);
      setNumPages(0);
      setCurrentPage(1);
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

        if (isPdf) {
          const arrayBuffer = await blob.arrayBuffer();
          if (!isMounted) return;

          const loadingTask = pdfjsLib.getDocument({
            data: new Uint8Array(arrayBuffer),
            cMapUrl: 'https://cdn.jsdelivr.net/npm/pdfjs-dist@3.11.174/cmaps/',
            cMapPacked: true,
          });

          const loadedPdf = await loadingTask.promise;
          if (!isMounted) return;

          setPdfDoc(loadedPdf);
          setNumPages(loadedPdf.numPages);
          setCurrentPage(1);
          setLoading(false);
        } else {
          setLoading(false);
        }
      } catch (err: any) {
        console.error('Error rendering document:', err);
        if (isMounted) {
          setError(err.message || 'Gagal memproses dan menampilkan dokumen.');
          setLoading(false);
        }
      }
    };

    load();

    return () => {
      isMounted = false;
    };
  }, [isOpen, docInfo]); // Remove fetchDocumentBlob from dependencies to avoid loop

  // Render PDF Pages to Canvases
  useEffect(() => {
    if (!pdfDoc || !canvasContainerRef.current) return;

    const renderPages = async () => {
      const container = canvasContainerRef.current;
      if (!container) return;

      // Cancel ongoing renders
      Object.values(renderTasksRef.current).forEach(task => {
        try { task?.cancel(); } catch (_) {}
      });
      renderTasksRef.current = {};

      const pagesToRender = viewMode === 'all' 
        ? Array.from({ length: numPages }, (_, i) => i + 1)
        : [currentPage];

      for (const pageNum of pagesToRender) {
        try {
          const page = await pdfDoc.getPage(pageNum);
          const viewport = page.getViewport({ scale, rotation });

          let canvas = container.querySelector(`canvas[data-page="${pageNum}"]`) as HTMLCanvasElement;
          if (!canvas) {
            canvas = document.createElement('canvas');
            canvas.setAttribute('data-page', pageNum.toString());
            canvas.className = 'shadow-[0_4px_12px_rgba(0,0,0,0.1)] rounded-lg bg-white my-4 block mx-auto transition-transform';
            container.appendChild(canvas);
          }

          const context = canvas.getContext('2d');
          if (!context) continue;

          canvas.height = viewport.height;
          canvas.width = viewport.width;

          const renderContext = {
            canvasContext: context,
            viewport: viewport
          };

          const renderTask = page.render(renderContext);
          renderTasksRef.current[pageNum] = renderTask;
          await renderTask.promise;
        } catch (err: any) {
          if (err?.name !== 'RenderingCancelledException') {
            console.error(`Error rendering page ${pageNum}:`, err);
          }
        }
      }

      // Clean up extra canvases if switched from 'all' to 'single'
      const existingCanvases = container.querySelectorAll('canvas');
      existingCanvases.forEach(c => {
        const pageNum = parseInt(c.getAttribute('data-page') || '0', 10);
        if (!pagesToRender.includes(pageNum)) {
          c.remove();
        }
      });
    };

    renderPages();
  }, [pdfDoc, scale, rotation, currentPage, viewMode, numPages]);

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

          {/* PDF Viewer Controls (Zoom, Rotate, Navigation) */}
          {isPdf && !loading && !error && (
            <div className="flex items-center gap-1.5 bg-[#F1F5F9] px-3 py-1.5 rounded-xl border border-[#E2E8F0]">
              {/* Zoom Out */}
              <button 
                onClick={() => setScale(prev => Math.max(0.6, prev - 0.2))}
                className="p-1.5 text-slate-500 hover:text-slate-900 hover:bg-slate-200 rounded-lg transition-colors cursor-pointer"
                title="Perkecil (-)"
              >
                <ZoomOut size={16} strokeWidth={1.5} />
              </button>

              <span className="text-xs font-mono font-bold text-[#0F172A] px-1.5 min-w-[50px] text-center">
                {Math.round(scale * 100)}%
              </span>

              {/* Zoom In */}
              <button 
                onClick={() => setScale(prev => Math.min(2.5, prev + 0.2))}
                className="p-1.5 text-slate-500 hover:text-slate-900 hover:bg-slate-200 rounded-lg transition-colors cursor-pointer"
                title="Perbesar (+)"
              >
                <ZoomIn size={16} strokeWidth={1.5} />
              </button>

              <div className="h-5 w-px bg-slate-300 mx-1.5" />

              {/* Rotate */}
              <button 
                onClick={() => setRotation(prev => (prev + 90) % 360)}
                className="p-1.5 text-slate-500 hover:text-slate-900 hover:bg-slate-200 rounded-lg transition-colors cursor-pointer"
                title="Putar 90°"
              >
                <RotateCw size={16} strokeWidth={1.5} />
              </button>

              {/* View Mode Toggle */}
              <button 
                onClick={() => setViewMode(prev => prev === 'all' ? 'single' : 'all')}
                className="px-2.5 py-1 ml-1 text-xs font-semibold text-slate-600 bg-white border border-slate-200 hover:bg-slate-50 hover:text-slate-900 rounded-lg transition-colors cursor-pointer shadow-sm"
                title={viewMode === 'all' ? 'Mode Satu Halaman' : 'Mode Semua Halaman'}
              >
                {viewMode === 'all' ? 'Semua Hal' : 'Per Hal'}
              </button>

              {/* Single page navigation */}
              {viewMode === 'single' && numPages > 1 && (
                <>
                  <div className="h-5 w-px bg-slate-300 mx-1.5" />
                  <button 
                    onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                    disabled={currentPage <= 1}
                    className="p-1.5 text-slate-500 hover:text-slate-900 hover:bg-slate-200 rounded-lg disabled:opacity-30 cursor-pointer"
                  >
                    <ChevronLeft size={16} strokeWidth={1.5} />
                  </button>
                  <span className="text-xs font-semibold text-slate-700 px-1.5">
                    {currentPage} / {numPages}
                  </span>
                  <button 
                    onClick={() => setCurrentPage(prev => Math.min(numPages, prev + 1))}
                    disabled={currentPage >= numPages}
                    className="p-1.5 text-slate-500 hover:text-slate-900 hover:bg-slate-200 rounded-lg disabled:opacity-30 cursor-pointer"
                  >
                    <ChevronRight size={16} strokeWidth={1.5} />
                  </button>
                </>
              )}
            </div>
          )}

          {/* Right Action Buttons */}
          <div className="flex items-center gap-2 shrink-0">
            {blobUrl && (
              <>
                <button
                  onClick={() => window.open(blobUrl, '_blank')}
                  className="px-3.5 py-2 bg-white hover:bg-slate-50 text-slate-700 hover:text-slate-900 rounded-xl text-xs font-semibold flex items-center gap-1.5 transition-colors border border-[#E2E8F0] shadow-sm cursor-pointer"
                  title="Buka di tab baru browser"
                >
                  <ExternalLink size={15} strokeWidth={1.5} />
                  <span className="hidden sm:inline">Tab Baru</span>
                </button>
                <button
                  onClick={handleDownload}
                  className="px-3.5 py-2 bg-[#2563EB] hover:bg-blue-700 text-white rounded-xl text-xs font-semibold flex items-center gap-1.5 transition-colors shadow-sm cursor-pointer"
                  title="Unduh file"
                >
                  <Download size={15} strokeWidth={1.5} />
                  <span className="hidden sm:inline">Unduh</span>
                </button>
              </>
            )}
            <button
              onClick={toggleFullscreen}
              className="p-2 bg-white hover:bg-slate-50 text-slate-500 hover:text-slate-900 border border-[#E2E8F0] shadow-sm rounded-xl transition-colors cursor-pointer ml-2"
              title={isFullscreen ? "Keluar Layar Penuh" : "Layar Penuh"}
            >
              {isFullscreen ? <Minimize2 size={16} strokeWidth={1.5} /> : <Maximize2 size={16} strokeWidth={1.5} />}
            </button>
            <button
              onClick={onClose}
              className="p-2 bg-white hover:bg-[#FEE2E2] text-slate-500 hover:text-[#DC2626] border border-[#E2E8F0] shadow-sm rounded-xl transition-colors cursor-pointer ml-1"
              title="Tutup pratinjau (Esc)"
            >
              <X size={16} strokeWidth={1.5} />
            </button>
          </div>
        </div>

        {/* Viewer Content Body */}
        <div className="flex-1 bg-[#EEF2F7] relative overflow-auto p-4 md:p-6 flex flex-col items-center justify-start min-h-0">
          {loading && (
            <div className="my-auto flex flex-col items-center justify-center gap-3 p-8">
              <div className="w-14 h-14 bg-white rounded-2xl shadow-sm flex items-center justify-center border border-blue-100">
                <Loader2 size={28} className="animate-spin text-[#2563EB]" />
              </div>
              <p className="text-sm font-semibold text-[#0F172A] mt-2">Memuat dokumen...</p>
              <p className="text-xs text-[#64748B]">Mempersiapkan pratinjau PDF di layar Anda</p>
            </div>
          )}

          {error && (
            <div className="my-auto max-w-md bg-white border border-[#DC2626] text-[#0F172A] p-6 rounded-2xl text-center shadow-lg">
              <div className="w-14 h-14 bg-red-50 rounded-full flex items-center justify-center mx-auto mb-4">
                <AlertCircle size={28} className="text-[#DC2626]" strokeWidth={1.5} />
              </div>
              <p className="font-bold text-base mb-2">Gagal Menampilkan Pratinjau</p>
              <p className="text-sm text-[#64748B] mb-6">{error}</p>
              <div className="flex items-center justify-center gap-3">
                <button
                  onClick={handleDownload}
                  className="px-5 py-2.5 bg-[#2563EB] hover:bg-blue-700 text-white rounded-xl text-sm font-semibold inline-flex items-center gap-2 transition-colors shadow-sm"
                >
                  <Download size={16} /> Unduh File
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

          {/* PDF Canvas Rendering Container */}
          {isPdf && !loading && !error && (
            <div 
              ref={canvasContainerRef} 
              className="w-full flex flex-col items-center justify-center min-h-full"
            />
          )}

          {/* Image Viewer */}
          {isImage && !loading && !error && blobUrl && (
            <div className="my-auto flex items-center justify-center max-w-full max-h-full p-2">
              <img
                src={blobUrl}
                alt={docInfo.fileName}
                className="max-h-[75vh] max-w-full object-contain rounded-xl shadow-lg border border-[#E2E8F0] bg-white"
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
              <p className="text-sm text-[#64748B] mb-6">Format file ini belum didukung untuk dibaca langsung. Silakan unduh untuk membukanya secara lokal.</p>
              <button
                onClick={handleDownload}
                className="px-5 py-2.5 w-full bg-[#2563EB] hover:bg-blue-700 text-white rounded-xl text-sm font-semibold inline-flex items-center justify-center gap-2 transition-colors shadow-sm"
              >
                <Download size={16} strokeWidth={1.5} /> Unduh Berkas
              </button>
            </div>
          )}
        </div>

        {/* Bottom Footer Info */}
        {isPdf && !loading && !error && (
          <div className="px-5 py-3 bg-white border-t border-[#E2E8F0] text-xs font-medium text-[#64748B] flex items-center justify-between shrink-0">
            <span>Total: <strong className="text-[#0F172A]">{numPages}</strong> Halaman</span>
            <span>Gunakan tombol kontrol di bilah atas untuk zoom & rotasi tampilan</span>
          </div>
        )}
      </div>
    </div>
  );
};
