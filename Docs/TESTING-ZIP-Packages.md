# Kiểm thử ZIP Packages (v16)

Các scenario kiểm thử cho cơ chế ZIP-first, per-file fallback:

## 1. ManifestTestServer

Đã bổ sung các endpoint:

- `GET /manifest/zip/ok/latest.json`
  - Trả về manifest:
    - `version`: `1.2.3.4`
    - `sha512`: SHA-512 của file ZIP `static/app-zip-ok-1.2.3.4.zip`
    - `zip_urls`: `["/static/app-zip-ok-1.2.3.4.zip"]`
    - `file_md5`:
      - `MyApp.exe` – MD5 khớp với nội dung trong ZIP
      - `Lib.dll`   – MD5 khớp với nội dung trong ZIP

- `GET /manifest/zip/fail-perfile/latest.json`
  - Trả về manifest:
    - `version`: `2.0.0.0`
    - `sha512`: cố tình sai (all zero) để ZIP path thất bại kiểm tra hash.
    - `zip_urls`: `["/static/app-zip-bad-2.0.0.0.zip"]` (file không tồn tại).
    - `file_md5`:
      - `MyApp.exe` với MD5 khớp file `{rel_f1}`
      - `Lib.dll`   với MD5 khớp file `{rel_f2}`
      - `urls` trỏ đến `/files/zipfallback/...`.

`server.js` cũng đã:

- `app.use('/static', express.static(...))` → thư mục `ManifestTestServer/static`
- `app.use('/files/zipfallback', express.static(...))` → `ManifestTestServer/files_zipfallback`

## 2. TestRunner scenarios

Trong `Dh.Launcher.TestRunner`:

- **ZipPackageOk**
  - Root: `{{customRoot}}/ZipPackageOk`
  - `PrepareBasicSampleRoot(..., active="1.0.0.0", latest="1.2.3.4", baseUrl)`
  - Nếu `baseUrl` không rỗng → sửa `Config/launcher.json` từ `latest.json` sang `zip/ok/latest.json`.
  - Kỳ vọng:
    - Lần check/apply:
      - Sử dụng path ZIP:
        - Download ZIP `app-zip-ok-1.2.3.4.zip`.
        - Verify SHA512.
        - Unzip.
        - Verify MD5 từng file.
      - Không cần tải per-file.

- **ZipPackageFailThenPerFileOk**
  - Root: `{{customRoot}}/ZipPackageFailThenPerFileOk`
  - `PrepareBasicSampleRoot(..., active="1.0.0.0", latest="2.0.0.0", baseUrl)`
  - Sửa `launcher.json` để trỏ `zip/fail-perfile/latest.json`.
  - Kỳ vọng:
    - Path ZIP:
      - Thử tải ZIP từ `/static/app-zip-bad-2.0.0.0.zip` → lỗi hoặc sai hash.
      - `TryApplyZipPackageAsync` trả `false`.
    - Engine fallback sang per-file:
      - Tải `MyApp.exe` + `Lib.dll` từ `/files/zipfallback/...`.
      - Verify MD5 khớp.
      - Áp dụng version mới thành công.

## 3. Cách chạy

Ví dụ:

```bash
# Chạy manifest server (Node):
node ManifestTestServer/server.js

# Chạy TestRunner với baseUrl trỏ tới server:
Dh.Launcher.TestRunner.exe --scenario=ZipPackageOk --baseUrl=http://localhost:3000
Dh.Launcher.TestRunner.exe --scenario=ZipPackageFailThenPerFileOk --baseUrl=http://localhost:3000
```

Nếu dùng `--junit=...`:
- Cả hai scenario sẽ được ghi vào báo cáo JUnit giống các scenario khác.
