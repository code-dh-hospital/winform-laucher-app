# Testing – Scenarios & ManifestTestServer (Node.js)

Tài liệu này mô tả cách:

- Chạy các "unit test" scenario độc lập bằng `Dh.Launcher.TestRunner` (console).
- Sử dụng Node.js `ManifestTestServer` để giả lập server manifest chuẩn / lỗi / chậm.

---

## 1. Test Runner – Dh.Launcher.TestRunner

### 1.1. Build

- Đảm bảo đã copy `Newtonsoft.Json.dll` vào thư mục `libs` ở root solution.
- Build project `Dh.AppLauncher.Core` trước (Debug).
- Build project `Dh.Launcher.TestRunner`.

### 1.2. Cách chạy

Từ thư mục chứa `Dh.Launcher.TestRunner.exe`:

```bash
Dh.Launcher.TestRunner.exe
```

- Không truyền tham số: chương trình sẽ liệt kê các scenario và cho nhập tên.

Hoặc chạy trực tiếp một scenario:

```bash
Dh.Launcher.TestRunner.exe --scenario=SilentUpdateAvailable
Dh.Launcher.TestRunner.exe --scenario=BadManifestJson --baseUrl=http://localhost:3000
```

Các tham số:

- `--scenario=NAME` : tên scenario (NoUpdate, SilentUpdateAvailable, AskUserDecline, ForceUpdate, BadManifestJson).
- `--root=PATH` : thư mục root test (mặc định: `.\_TestRoots`).
- `--baseUrl=URL` : base URL của manifest server (ví dụ `http://localhost:3000`).

### 1.3. Các Scenario hiện có

1. **NoUpdate**
   - `activeVersion = 1.0.0.0`, `latestVersion = 1.0.0.0`.
   - Kỳ vọng:
     - `CheckOnly` báo không có bản mới.
     - Không chạy `ApplyLatest`.

2. **SilentUpdateAvailable**
   - `activeVersion = 1.0.0.0`, `latestVersion = 1.2.0.0` (update_level = silent).
   - Kỳ vọng:
     - `CheckOnly` phát hiện bản mới.
     - `ApplyLatest` tải và đổi `active.json` sang 1.2.0.0.

3. **AskUserDecline**
   - `activeVersion = 1.0.0.0`, `latestVersion = 1.1.0.0` (update_level = ask).
   - Kỳ vọng:
     - `CheckOnly` phát hiện bản mới, báo level=AskUser.
     - Scenario giả lập người dùng từ chối → không gọi `ApplyLatest`.

4. **ForceUpdate**
   - `activeVersion = 1.0.0.0`, `latestVersion = 1.2.0.0` (update_level = force).
   - Kỳ vọng:
     - `CheckOnly` phát hiện bản mới, level=Force.
     - `ApplyLatest` phải thành công; nếu không, scenario coi như fail.

5. **BadManifestJson**
   - Dùng `--baseUrl=http://localhost:3000` (ManifestTestServer).
   - Launcher sẽ trỏ đến `/manifest/bad/latest.json`.
   - Kỳ vọng:
     - `CheckOnly` hoặc `ApplyLatest` ném exception parse JSON → dùng để test nhánh lỗi server.

Mỗi scenario tự tạo root riêng trong `--root` để không ảnh hưởng đến dữ liệu thật.

---

## 2. ManifestTestServer (Node.js)

### 2.1. Cài đặt

Yêu cầu có Node.js và npm.

Trong thư mục `ManifestTestServer`:

```bash
npm install
npm start
```

Server sẽ lắng nghe ở `http://localhost:3000` (hoặc port khác nếu đặt `PORT`).

### 2.2. Các endpoint chính

- `GET /manifest/latest.json`
  - Trả về manifest OK (`manifest-1.2.0.0-ok.json`).
- `GET /manifest/1.0.0.0.json`
  - Manifest v1.0.0.0 (silent).
- `GET /manifest/1.1.0.0.json`
  - Manifest v1.1.0.0 (ask).
- `GET /manifest/bad/latest.json`
  - Trả về JSON bị lỗi để test parse error.
- `GET /manifest/error/latest.json`
  - Trả về HTTP 500.
- `GET /manifest/slow/latest.json`
  - Trả về manifest OK sau 10 giây (test timeout).

### 2.3. Kết nối với TestRunner

Khi chạy `Dh.Launcher.TestRunner`, truyền `--baseUrl=http://localhost:3000` để:

- `launcher.json` trong các scenario sẽ dùng URL:
  - `http://localhost:3000/manifest/latest.json` (mặc định).
- Riêng scenario `BadManifestJson` sẽ sửa URL thành:
  - `http://localhost:3000/manifest/bad/latest.json`.

---

## 3. Tự động hóa & CI

- Có thể chạy từng scenario độc lập trong script CI:

```bash
Dh.Launcher.TestRunner.exe --scenario=NoUpdate
Dh.Launcher.TestRunner.exe --scenario=SilentUpdateAvailable
Dh.Launcher.TestRunner.exe --scenario=AskUserDecline
Dh.Launcher.TestRunner.exe --scenario=ForceUpdate
Dh.Launcher.TestRunner.exe --scenario=BadManifestJson --baseUrl=http://localhost:3000
```

- Kết hợp cùng `ManifestTestServer` để giả lập các tình huống:
  - Server trả JSON lỗi.
  - Server trả 500.
  - Server chậm (slow response) để test timeout & retry (phần này có thể phát triển thêm trong `UpdateManager`).



### 1.4. Các Scenario nâng cao (v11)

6. **Md5Mismatch** (cần ManifestTestServer)
   - Dùng: `--scenario=Md5Mismatch --baseUrl=http://localhost:3000`
   - Manifest: `/manifest/md5-mismatch/latest.json` trả về `manifest-1.2.0.0-md5-mismatch.json`.
   - Server file: `/files/badmd5/1.2.0.0/Dh.Updater.DebugConsole.Core.dll` trả payload khác với MD5 trong manifest.
   - Kỳ vọng:
     - Update engine phát hiện lỗi MD5 (throw hoặc fail), scenario log `"Expected failure"`.

7. **MirrorFailThenOk** (cần ManifestTestServer)
   - Dùng: `--scenario=MirrorFailThenOk --baseUrl=http://localhost:3000`
   - Manifest: `/manifest/mirror/latest.json` với `urls` gồm 2 URL: 1 lỗi, 1 OK.
   - Server file:
     - `/files/mirror/bad/...` trả 500.
     - `/files/mirror/good/...` trả payload chuẩn.
   - Kỳ vọng:
     - Engine thử URL đầu thất bại, chuyển sang URL thứ hai, update thành công.

8. **ClientMatchGroupsTags** (cần ManifestTestServer)
   - Dùng: `--scenario=ClientMatchGroupsTags --baseUrl=http://localhost:3000`
   - Manifest: `/manifest/groups/latest.json` chỉ cho phép group `"HOSPITAL-A"` & tag `"beta"`.
   - TestRunner tự tạo `client_identity.json` với `groups=["HOSPITAL-A"]`, `tags=["beta"]`.
   - Kỳ vọng:
     - Manifest được chọn và update diễn ra bình thường.
