# Enterprise Employee Management System (EMS)

![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core_8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![React](https://img.shields.io/badge/React-20232A?style=for-the-badge&logo=react&logoColor=61DAFB)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-007ACC?style=for-the-badge&logo=typescript&logoColor=white)
![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-38B2AC?style=for-the-badge&logo=tailwind-css&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2CA5E0?style=for-the-badge&logo=docker&logoColor=white)

Employee Management System (EMS) adalah platform *Human Resource* tingkat *enterprise* yang dirancang untuk mengelola seluruh siklus hidup karyawan mulai dari data personal, absensi, hingga cuti. Sistem ini dibangun dengan fokus pada keamanan (berbasis RBAC granular), skalabilitas, dan kepatuhan terhadap regulasi ketenagakerjaan di Indonesia.

---

## 🎯 Fitur Utama

### 🔐 Authentication & Authorization
*   **Role-Based Access Control (RBAC)**: Otorisasi berbasis permission yang granular (bukan sekadar role-based), memungkinkan fleksibilitas tinggi dalam memberikan akses spesifik (misal: `employee.read`, `leave.approve`).
*   **Secure Authentication**: Menggunakan JWT (JSON Web Token) dengan implementasi enkripsi *password* menggunakan BCrypt.

### 👥 Employee Management
*   **Hierarki & Struktur Organisasi**: Mendukung relasi *Manager-Subordinate* tanpa *circular reference*, terintegrasi dengan struktur Department dan tingkatan Position (CRUD).
*   **Manajemen Dokumen**: Upload, validasi (.pdf, .jpg, .png), download, dan penghapusan fisik dokumen sensitif karyawan (KTP, Ijazah, Kontrak) secara aman.
*   **Audit Trail/Log Otomatis**: Mencatat seluruh histori perubahan data karyawan secara kronologis (*Create/Update/Delete*) lengkap dengan perbandingan nilai lama vs nilai baru yang mendetail.

### 🏖️ Leave Management (Manajemen Cuti)
*   **Multi-level Approval Workflow**: Permintaan cuti secara otomatis dirutekan ke *Manager* terkait untuk persetujuan (Approve/Reject) beserta catatan/alasan penolakan.
*   **Filter Cuti Berbasis Gender (UU Ketenagakerjaan)**: Penyesuaian hak jenis cuti secara otomatis, seperti opsi Cuti Melahirkan (Maternity Leave) yang eksklusif untuk pekerja perempuan.

### 🕒 Attendance System (Absensi)
*   **Geofencing & Validasi Lokasi**: Menghitung jarak menggunakan *Haversine Formula* untuk memastikan karyawan *Clock-In/Out* di dalam batas radius kantor yang valid.
*   **Riwayat Absensi**: Pencatatan waktu kerja secara real-time dan akurat.

### 📊 Dashboard Analytics
*   **Personalisasi Pengguna**: Sapaan dinamis pada *dashboard* berdasarkan profil karyawan yang sedang *login*.
*   Visualisasi data tenaga kerja (Total Karyawan, Statistik Departemen), tren kehadiran, dan status permintaan cuti menggunakan **Recharts**.

---

## 💻 Tech Stack

| Kategori | Teknologi Utama |
| :--- | :--- |
| **Backend** | ASP.NET Core 8 Web API, Entity Framework Core, PostgreSQL, Serilog, FluentValidation, JWT Authentication |
| **Frontend** | React 18, Vite, TypeScript, TanStack Query (React Query), Tailwind CSS, Recharts |
| **Tools & DevOps** | Docker, xUnit |

---

## 🏗️ Arsitektur

Sistem ini menggunakan **Clean Architecture** yang ketat untuk memastikan *separation of concerns*, kemudahan *testing*, dan skalabilitas *codebase*.

```text
EMS Solution
├── EMS.Domain         # (Core) Berisi Entities, Enums, dan Interfaces. Tidak bergantung pada project apa pun.
├── EMS.Application    # (Use Cases) Berisi DTOs, Business Logic, Validation, dan Interfaces dari Service. Bergantung pada Domain.
├── EMS.Infrastructure # (Data) Implementasi EF Core DbContext, Migrations, dan Repositories. Bergantung pada Application.
└── EMS.API            # (Presentation) Controllers, Setup Middleware, JWT Config. Bergantung pada Application & Infrastructure.
```

---

## 🗂️ Entity Relationship Diagram (ERD)

![ERD](./docs/erd.png)

*(Catatan: Diagram di atas merupakan representasi skema database EMS. File ERD tersedia pada direktori `/docs`)*

---

## 🚀 Cara Menjalankan Secara Lokal

Ikuti langkah-langkah di bawah ini untuk menjalankan aplikasi di lingkungan lokal Anda.

### Prasyarat
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [Node.js](https://nodejs.org/)
*   PostgreSQL Server (berjalan di port default 5432)

### 1. Clone Repository & Setup Database
```bash
git clone https://github.com/your-username/EmployeeManagementSystem.git
cd EmployeeManagementSystem
```
Buat database baru di PostgreSQL Anda. Kemudian perbarui nilai *Connection String* pada file `EMS.API/appsettings.Development.json` dengan kredensial PostgreSQL Anda.

### 2. Jalankan Migrasi Database
Buka terminal pada *root folder* lalu jalankan:
```bash
dotnet ef database update --project EMS.Infrastructure --startup-project EMS.API
```

### 3. Jalankan Backend (API)
```bash
cd EMS.API
dotnet run
```
API akan berjalan di `http://localhost:5000`.

### 4. Jalankan Frontend (React SPA)
Buka terminal baru di *root folder*, lalu:
```bash
cd ems-frontend
npm install
npm run dev
```
Aplikasi React akan berjalan di `http://localhost:3000`.

### Kredensial Default (Administrator)
Gunakan kredensial ini untuk *login* pertama kali:
*   **Email**: `admin@ems.local`
*   **Password**: `Admin123!`

---

## 🐳 Cara Menjalankan dengan Docker

Instruksi singkat menggunakan Docker Compose:

```bash
docker-compose up -d
```
Tunggu beberapa saat sampai seluruh kontainer *ready*. Aplikasi dapat diakses di URL yang sama: Frontend (`http://localhost:3000`) dan Backend (`http://localhost:5000`).

---

## 🔐 Environment Variables

Konfigurasi variabel *environment* yang dibutuhkan (misal di `appsettings.json`):

| Variabel | Deskripsi | Contoh Nilai |
| :--- | :--- | :--- |
| `ConnectionStrings:DefaultConnection` | Koneksi database PostgreSQL | `Host=localhost;Database=ems;Username=postgres;Password=mypassword;` |
| `JwtSettings:Secret` | Kunci rahasia untuk tanda tangan token | `SangatRahasia_ContohKunciPanjangMinimal32Karakter_!` |
| `JwtSettings:Issuer` | Penerbit token | `EMSSystem` |
| `JwtSettings:Audience` | Penerima token yang valid | `EMSUsers` |
| `JwtSettings:ExpiryMinutes` | Durasi token berlaku (dalam menit) | `60` |

---

## 📚 API Documentation

Setelah backend berjalan, dokumentasi *endpoint* API (Swagger UI) dapat diakses melalui:
👉 **[http://localhost:5000/swagger](http://localhost:5000/swagger)**

---

## 🧠 Keputusan Teknis Penting

1. **Permission-based Authorization**: Kami memilih *permission-based authorization* alih-alih murni *role-based* (*staff*, *admin*) agar pengelolaan akses jauh lebih *granular* dan *scalable*. Hal ini mengizinkan penciptaan Role baru secara dinamis di masa depan (melalui database) tanpa perlu memodifikasi kode.
2. **Geofencing Menggunakan Haversine Formula**: Perhitungan Geofencing pada modul absensi memakai formula Haversine untuk menghitung jarak koordinat GPS secara akurat berdasarkan lengkung permukaan bumi, meminimalisir kemungkinan celah absen palsu dari radius yang salah.
3. **Audit Trail Menggunakan Override SaveChanges**: Interseptor langsung disematkan pada fungsi `SaveChangesAsync()` EF Core untuk menghasilkan sistem *audit logging* yang efisien; sistem ini memastikan bahwa hanya field yang benar-benar mengalami mutasi yang akan tersimpan ke *database*.

---

## 📸 Screenshot

### Dashboard
![Dashboard](./docs/screenshots/dashboard.png)

### Manajemen Karyawan
![Employees](./docs/screenshots/employees.png)

### Manajemen Cuti (Leaves)
![Leaves](./docs/screenshots/leaves.png)

### Absensi (Attendance)
![Attendance](./docs/screenshots/attendance.png)

*(Catatan: Lihat keseluruhan hasil tangkapan layar lainnya pada direktori `/docs/screenshots`)*

---

## 📝 Lisensi & Kontak

Proyek ini dibuat untuk keperluan *Enterprise Resource Planning*. 
Lisensi mengikuti ketentuan perusahaan. 

Untuk pertanyaan, keluhan, atau *feedback*, silakan hubungi tim pengembangan *Human Resource Tech*.
