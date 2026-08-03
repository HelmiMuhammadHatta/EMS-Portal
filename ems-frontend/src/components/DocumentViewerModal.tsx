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
  }, [isOpen, docInfo]);

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
            canvas.className = 'shadow-lg rounded-lg bg-white my-3 border border-slate-200 block mx-auto transition-transform';
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
    <div className="fixed inset-0 bg-slate-900/80 backdrop-blur-sm z-50 flex items-center justify-center p-2 sm:p-4">
      <div 
        ref={modalContainerRef}
        className={`bg-slate-900 rounded-2xl w-full flex flex-col shadow-2xl border border-slate-800 overflow-hidden animate-in zoom-in-95 duration-200 transition-all ${
          isFullscreen ? 'h-screen max-w-full rounded-none' : 'max-w-5xl h-[92vh]'
        }`}
      >
        {/* Top Control Bar */}
        <div className="px-4 py-3 bg-slate-900 text-white flex flex-wrap items-center justify-between gap-3 border-b border-slate-800 shrink-0">
          
          {/* File Info */}
          <div className="flex items-center gap-3 min-w-0">
            <div className="w-9 h-9 rounded-xl bg-blue-600/20 text-blue-400 border border-blue-500/30 flex items-center justify-center shrink-0">
              <FileText size={18} />
            </div>
            <div className="min-w-0">
              <div className="flex items-center gap-2">
                <span className="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-blue-500/20 text-blue-300 border border-blue-400/30">
                  {docInfo.documentType}
                </span>
                <h3 className="text-sm font-bold text-slate-100 truncate" title={docInfo.fileName}>
                  {docInfo.fileName}
                </h3>
              </div>
              <p className="text-[11px] text-slate-400 mt-0.5">
                {(docInfo.fileSize / 1024).toFixed(1)} KB · Diunggah {format(new Date(docInfo.uploadedAt), 'd MMM yyyy, HH:mm')}
              </p>
            </div>
          </div>

          {/* PDF Viewer Controls (Zoom, Rotate, Navigation) */}
          {isPdf && !loading && !error && (
            <div className="flex items-center gap-1.5 bg-slate-800/90 px-3 py-1.5 rounded-xl border border-slate-700/80">
              {/* Zoom Out */}
              <button 
                onClick={() => setScale(prev => Math.max(0.6, prev - 0.2))}
                className="p-1 text-slate-300 hover:text-white hover:bg-slate-700 rounded-lg transition-colors cursor-pointer"
                title="Perkecil (-)"
              >
                <ZoomOut size={15} />
              </button>

              <span className="text-xs font-mono font-medium text-slate-300 px-1 min-w-[44px] text-center">
                {Math.round(scale * 100)}%
              </span>

              {/* Zoom In */}
              <button 
                onClick={() => setScale(prev => Math.min(2.5, prev + 0.2))}
                className="p-1 text-slate-300 hover:text-white hover:bg-slate-700 rounded-lg transition-colors cursor-pointer"
                title="Perbesar (+)"
              >
                <ZoomIn size={15} />
              </button>

              <div className="h-4 w-px bg-slate-700 mx-1" />

              {/* Rotate */}
              <button 
                onClick={() => setRotation(prev => (prev + 90) % 360)}
                className="p-1 text-slate-300 hover:text-white hover:bg-slate-700 rounded-lg transition-colors cursor-pointer"
                title="Putar 90°"
              >
                <RotateCw size={15} />
              </button>

              {/* View Mode Toggle */}
              <button 
                onClick={() => setViewMode(prev => prev === 'all' ? 'single' : 'all')}
                className="px-2 py-0.5 text-[11px] font-semibold text-slate-300 hover:text-white hover:bg-slate-700 rounded-md transition-colors cursor-pointer"
                title={viewMode === 'all' ? 'Mode Satu Halaman' : 'Mode Semua Halaman'}
              >
                {viewMode === 'all' ? 'Semua Hal' : 'Per Hal'}
              </button>

              {/* Single page navigation */}
              {viewMode === 'single' && numPages > 1 && (
                <>
                  <div className="h-4 w-px bg-slate-700 mx-1" />
                  <button 
                    onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                    disabled={currentPage <= 1}
                    className="p-1 text-slate-300 hover:text-white hover:bg-slate-700 rounded disabled:opacity-30 cursor-pointer"
                  >
                    <ChevronLeft size={15} />
                  </button>
                  <span className="text-xs text-slate-300 px-1">
                    {currentPage} / {numPages}
                  </span>
                  <button 
                    onClick={() => setCurrentPage(prev => Math.min(numPages, prev + 1))}
                    disabled={currentPage >= numPages}
                    className="p-1 text-slate-300 hover:text-white hover:bg-slate-700 rounded disabled:opacity-30 cursor-pointer"
                  >
                    <ChevronRight size={15} />
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
                  className="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 hover:text-white rounded-xl text-xs font-semibold flex items-center gap-1.5 transition-colors border border-slate-700 cursor-pointer"
                  title="Buka di tab baru browser"
                >
                  <ExternalLink size={14} />
                  <span className="hidden sm:inline">Tab Baru</span>
                </button>
                <button
                  onClick={handleDownload}
                  className="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-xs font-semibold flex items-center gap-1.5 transition-colors shadow-sm cursor-pointer"
                  title="Unduh file"
                >
                  <Download size={14} />
                  <span className="hidden sm:inline">Unduh</span>
                </button>
              </>
            )}
            <button
              onClick={toggleFullscreen}
              className="p-1.5 bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white rounded-xl transition-colors cursor-pointer"
              title={isFullscreen ? "Keluar Layar Penuh" : "Layar Penuh"}
            >
              {isFullscreen ? <Minimize2 size={16} /> : <Maximize2 size={16} />}
            </button>
            <button
              onClick={onClose}
              className="p-1.5 bg-slate-800 hover:bg-red-600 text-slate-300 hover:text-white rounded-xl transition-colors cursor-pointer ml-1"
              title="Tutup pratinjau (Esc)"
            >
              <X size={18} />
            </button>
          </div>
        </div>

        {/* Viewer Content Body */}
        <div className="flex-1 bg-slate-950/90 relative overflow-auto p-4 flex flex-col items-center justify-start min-h-0">
          {loading && (
            <div className="my-auto flex flex-col items-center justify-center gap-3 p-8 text-slate-300">
              <Loader2 size={38} className="animate-spin text-blue-500" />
              <p className="text-sm font-semibold">Memuat dan membaca dokumen PDF...</p>
              <p className="text-xs text-slate-500">Mempersiapkan pratinjau visual di layar Anda</p>
            </div>
          )}

          {error && (
            <div className="my-auto max-w-md bg-red-950/40 border border-red-800 text-red-200 p-6 rounded-2xl text-center">
              <AlertCircle size={36} className="mx-auto text-red-400 mb-3" />
              <p className="font-bold text-sm mb-1">Gagal Menampilkan Pratinjau</p>
              <p className="text-xs text-red-300/80 mb-4">{error}</p>
              <div className="flex items-center justify-center gap-2">
                <button
                  onClick={handleDownload}
                  className="px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-xs font-semibold inline-flex items-center gap-2"
                >
                  <Download size={14} /> Unduh File Saja
                </button>
                <button
                  onClick={onClose}
                  className="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-xl text-xs font-semibold"
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
              className="w-full flex flex-col items-center justify-center min-h-full py-2"
            />
          )}

          {/* Image Viewer */}
          {isImage && !loading && !error && blobUrl && (
            <div className="my-auto flex items-center justify-center max-w-full max-h-full p-2">
              <img
                src={blobUrl}
                alt={docInfo.fileName}
                className="max-h-[80vh] max-w-full object-contain rounded-xl shadow-2xl border border-slate-700"
              />
            </div>
          )}

          {/* Fallback for other file types */}
          {!isPdf && !isImage && !loading && !error && (
            <div className="my-auto text-center p-8 bg-slate-900 rounded-2xl border border-slate-800">
              <FileText size={48} className="mx-auto text-slate-500 mb-3" />
              <p className="text-sm font-semibold text-slate-200">Pratinjau langsung tidak tersedia untuk format file ini</p>
              <p className="text-xs text-slate-400 mt-1 mb-4">Anda dapat mengunduh berkas untuk membukanya secara lokal</p>
              <button
                onClick={handleDownload}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-xs font-semibold inline-flex items-center gap-2"
              >
                <Download size={14} /> Unduh Berkas
              </button>
            </div>
          )}
        </div>

        {/* Bottom Footer Info */}
        {isPdf && !loading && !error && (
          <div className="px-4 py-2 bg-slate-900 border-t border-slate-800 text-xs text-slate-400 flex items-center justify-between shrink-0">
            <span>Total: <strong>{numPages}</strong> Halaman</span>
            <span>Gunakan tombol zoom & putar di bilah atas untuk menyesuaikan tampilan</span>
          </div>
        )}
      </div>
    </div>
  );
};
