# Winform Launcher / Updater — Repository Overview

Một README tổng hợp, hướng dẫn rõ ràng cho người mới vào dự án: mục đích, cấu trúc, luồng chạy, cách build/chạy, triển khai thực tế, gợi ý debug và quy trình đóng góp.

---

## Mô tả ngắn
Đây là thư viện/ứng dụng launcher & updater cho ứng dụng Windows (WinForms / WinUI) cùng nhiều dự án demo/test. Mục tiêu:
- Cung cấp core logic để kiểm tra bản cập nhật, tải về, áp dụng cập nhật và khởi động ứng dụng.
- Có các phiên bản demo/runner (WinForms, WinUI, Console) để kiểm thử.
- Có một server thử nghiệm (ManifestTestServer) để phục vụ manifest và gói cập nhật mẫu.

---

## Thành phần chính trong repo
(Các tên thư mục theo cấu trúc repo)
- Dh.AppLauncher.Core  
  Thư viện cốt lõi chứa logic launcher/updater (kiểm tra manifest, so sánh phiên bản, tải file, xác thực, apply update, khởi chạy process).
- Dh.Launcher.ConsoleTest  
  Ứng dụng console dùng để test luồng launcher trong môi trường console.
- Dh.Launcher.TestRunner.WinUI  
  Ví dụ/runner cho WinUI — tích thử UI hiện đại.
- Dh.Launcher.TestRunner  
  Runner chung (điểm khởi chạy cho các test).
- Dh.Launcher.WinFormsTest  
  Ví dụ/runner cho WinForms (giao diện desktop cổ điển).
- Dh.Updater.DebugConsole.Core  
  Phiên bản debug/console của component updater, có thể dùng để in log chi tiết khi phát triển.
- Dh.Updater.DebugWinForms.Core  
  Phiên bản debug với giao diện WinForms để quan sát tiến trình update trực quan.
- ManifestTestServer  
  Server dùng để phục vụ manifest và file cập nhật (dùng khi test triển khai thực tế).
- Samples  
  Ví dụ manifest / các package mẫu để thử nghiệm.
- Docs  
  Tài liệu thêm (nếu có).
- README.txt  
  File lịch sử/điểm cập nhật nhanh (changelog ngắn).

---

## Công dụng & nhiệm vụ của các module (tóm tắt)
- AppLauncher.Core: nhiệm vụ chính — kiểm tra manifest từ server, quyết định cập nhật, tải về, verify (checksum/signature nếu có), áp dụng update (thường bằng cách giải nén ghi đè hoặc chạy installer phụ trợ), cuối cùng khởi chạy ứng dụng mục tiêu.
- Updater.Debug*: các công cụ để phát triển/quan sát luồng cập nhật (log chi tiết, UI debug).
- TestRunner / WinFormsTest / WinUI: ví dụ để dev/test tích hợp với launcher — giúp kiểm thử luồng update trên từng UI target.
- ManifestTestServer: cung cấp manifest dạng HTTP để dễ mô phỏng server hosting bản phát hành.

---

## Luồng chạy (high-level)
1. Launcher khởi động (bootstrapper).
2. Đọc cấu hình (URL manifest, channel, phiên bản hiện tại).
3. Gọi manifest server để lấy manifest mới nhất.
4. So sánh phiên bản: nếu có bản mới > phiên bản hiện tại:
   - Thông báo người dùng (nếu cần) hoặc tự động tải.
   - Tải các file cần thiết (checksum, chữ ký nếu áp dụng).
   - Xác thực integrity (checksum, optional signature).
   - Áp dụng update (ghi đè file, chạy installer, hoặc giải nén).
   - Khởi động ứng dụng chính.
5. Nếu không có update, khởi trực tiếp ứng dụng mục tiêu.

Lưu ý: Có thể có lock/đồng bộ (nếu launcher tự cập nhật chính nó) — thường cần một bootstrapper tách biệt để cập nhật launcher nếu cần.

---

## Yêu cầu môi trường
- Hệ điều hành: Windows 10/11 (các runner WinForms/WinUI).
- .NET SDK: .NET 6 / .NET 7 trở lên (dự án WinForms/WinUI thường dùng .NET 6+). Nếu repo dùng phiên bản khác, mở solution trong Visual Studio để xem TargetFramework.
- Visual Studio 2022 / Visual Studio Code (kèm .NET SDK).
- Quyền mạng để gọi manifest server/host file cập nhật.

---

## Build & chạy (local)
Các hướng dẫn dưới đây mang tính tổng quát — điều chỉnh project path / framework nếu cần.

1. Clone repo:
   - git clone https://github.com/o6s220110g-lg/winform-laucher-app.git

2. Build toàn bộ solution hoặc từng project:
   - dotnet build ./Dh.AppLauncher.Core
   - dotnet build ./Dh.Launcher.ConsoleTest
   - Hoặc build solution nếu có file .sln:
     - dotnet build YourSolution.sln

3. Chạy ManifestTestServer (server thử nghiệm manifest):
   - cd ManifestTestServer
   - dotnet run
   - Mặc định server sẽ phục vụ manifest & assets tại một cổng (xem console output hoặc file cấu hình). Dùng Browser hoặc curl để kiểm tra endpoint.

4. Chạy một runner/test app trỏ tới manifest:
   - Ví dụ: chạy runner console
     - dotnet run --project ./Dh.Launcher.ConsoleTest
   - Hoặc chạy WinForms/WinUI TestRunner từ Visual Studio (set project làm startup).

5. Cấu hình để runner dùng manifest test server:
   - Trong thư mục Samples (hoặc file cấu hình appsettings.json / code), cập nhật URL manifest thành http://localhost:<port>/manifest.json hoặc tương tự.
   - Khởi động ManifestTestServer trước rồi mới chạy launcher.

---

## Ví dụ manifest (mẫu, tham khảo)
Một manifest thường chứa metadata bản phát hành, danh sách artifact với URL và checksum. Ví dụ (JSON mẫu, chỉnh theo format project thực tế):

```json
{
  "version": "1.2.3",
  "publishedAt": "2025-11-30T00:00:00Z",
  "artifacts": [
    {
      "name": "MyApp-v1.2.3.zip",
      "url": "https://example.com/releases/MyApp-v1.2.3.zip",
      "checksum": "sha256:abcdef...",
      "size": 12345678
    }
  ],
  "notes": "Sửa lỗi và cải tiến hiệu năng"
}
```

(Thực tế manifest structure có thể khác — xem file trong Samples để biết định dạng repo đang dùng.)

---

## Triển khai thực tế (production)
1. Chuẩn bị release artifacts (zip/installer/exe) và tính checksum + (nên) ký số.
2. Đặt artifacts lên một hosting ổn định (CDN / web server) với URL công khai (HTTPS).
3. Tạo manifest cập nhật (JSON/XML tùy design) trỏ tới artifacts. Đặt manifest ở một endpoint cố định (ví dụ: https://updates.example.com/manifest-prod.json).
4. Cấu hình launcher clients để trỏ tới URL manifest production:
   - Có thể cài mặc định trong registry, file cấu hình, hoặc param khi cài đặt.
5. Triển khai launcher client:
   - Đóng gói installer cho launcher (nếu cần) và phát hành cho người dùng.
6. Cơ chế rollback:
   - Thiết kế manifest/launcher để giữ bản cũ trong trường hợp update lỗi (ví dụ: keep a backup copy before apply).
7. Bảo mật:
   - Phục vụ qua HTTPS.
   - Sử dụng checksum/cryptographic signatures để đảm bảo integrity.
   - Đặt CORS / auth nếu manifest server private.

---

## Logging & Debugging
- Dùng các project DebugConsole / DebugWinForms để theo dõi log chi tiết.
- Xem console output hoặc file log (nếu repo đã có cơ chế log file).
- Khi launcher không tải được manifest: kiểm tra URL, firewall, chứng chỉ SSL.
- Khi checksum mismatch: kiểm tra quá trình upload artifact, encoding, hoặc tạm thời file bị hỏng trên server.

---

## Quy trình phát triển & đóng góp
- Branching: dùng GitFlow / feature-branch (ví dụ: feature/xxx, fix/yyy).
- Commit message: ngắn gọn, có description nếu cần. Tham khảo Conventional Commits nếu muốn.
- PR: mở pull request, miêu tả rõ thay đổi, kèm hướng dẫn test cho reviewer.
- Tests: Thêm test cho logic core (so sánh version, verify checksum, tải file). Chạy unit tests trước khi merge.
- Code style: theo chuẩn C# (naming PascalCase cho public, camelCase cho local), dùng analyzers nếu repo có cấu hình.
- Môi trường phát triển: nếu cần, thêm file CONTRIBUTING.md (khuyến khích).

---

## Hướng dẫn debug nhanh (checklist)
- Launcher không khởi động:
  - Kiểm tra quyền exec, antivirus/quarantine.
- Không tải được manifest:
  - Kiểm tra URL, port, firewall, HTTPS certificate.
- File tải về không thể dùng:
  - Kiểm tra checksum, encoding, cách giải nén.
- Launcher tự cập nhật thất bại:
  - Đảm bảo có bootstrapper tách biệt hoặc cơ chế restart an toàn; không ghi đè file đang chạy.

---

## Changelog (tóm tắt từ README.txt)
(Dòng thời gian cập nhật phiên bản trong repo)
- Dh.AppLauncher.Core - v14 → được cập nhật nhiều lần (timestamps)
- Tiếp tục cập nhật lên v15, v16, v17, v18, v19 (xem log commit để biết chi tiết).
- (Chi tiết timestamp):
  - Updated to v14 at: 2025-11-29T15:29:21.834245Z
  - Updated to v14 at: 2025-11-29T15:36:30.439895Z
  - Updated to v14 at: 2025-11-29T15:44:16.627595Z
  - Updated to v14 at: 2025-11-29T15:58:03.734874Z
  - Updated to v14 at: 2025-11-29T16:03:52.032172Z
  - Updated to v14 at: 2025-11-29T16:12:48.554184Z
  - Updated to v14 at: 2025-11-29T16:28:39.646495Z
  - Updated to v14 at: 2025-11-29T16:43:28.228253Z
  - Updated to v15 at: 2025-11-30T00:48:11.627545Z
  - Updated to v16 at: 2025-11-30T01:06:50.701546Z
  - Updated to v17 at: 2025-11-30T01:15:45.496537Z
  - Updated to v18 at: 2025-11-30T01:20:05.856730Z
  - Updated to v19 at: 2025-11-30T01:43:46.041925Z

(Để biết chi tiết các thay đổi, vui lòng kiểm tra Git commit history.)

---

## Tài liệu tham khảo & nơi tìm file quan trọng
- Xem folder `Dh.AppLauncher.Core` để đọc logic chính.
- Xem `ManifestTestServer` để biết format manifest và cách phục vụ file.
- Xem các project test (WinForms/WinUI/Console) để biết cách launcher tích hợp với ứng dụng mục tiêu.
- Nếu cần template manifest / packages, xem `Samples`.

---

## Gợi ý tiếp theo cho người mới vào
- Bước 1: chạy ManifestTestServer local và kiểm tra endpoint manifest.
- Bước 2: chạy một runner (Console/WinForms) trỏ tới endpoint đó để quan sát luồng update.
- Bước 3: đọc source trong Dh.AppLauncher.Core, tìm entry point kiểm tra manifest, đặt breakpoint để theo dõi logic.
- Bước 4: nếu muốn thay đổi luồng update (ví dụ: thay checksum -> signature), implement ở core và thêm tests.

---

Nếu bạn muốn, tôi có thể:
- Viết mẫu file manifest cụ thể theo format repo (nếu bạn cung cấp một ví dụ manifest trong repo).
- Soạn CONTRIBUTING.md / CHECKLIST release & deploy chi tiết.
- Viết script nhỏ để build + tạo package release (PowerShell / bash) dựa trên cấu trúc hiện tại.
