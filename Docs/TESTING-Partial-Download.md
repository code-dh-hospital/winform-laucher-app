# TESTING – Partial Download / Interrupted Download

Mục tiêu: đảm bảo engine không bao giờ để version active bị hỏng khi việc tải file bị ngắt giữa chừng.

## Cách giả lập

Dùng `ManifestTestServer`:

- Endpoint manifest: `/manifest/partial/latest.json`
- Endpoint file: `/files/partial/1.2.0.0/Dh.Updater.DebugConsole.Core.dll`

Server sẽ:

- Gửi một vài bytes text ('PARTIAL-...') rồi chủ động đóng kết nối.

## Scenario `PartialDownload` trong TestRunner

Chạy:

```bash
Dh.Launcher.TestRunner.exe --scenario=PartialDownload --baseUrl=http://localhost:3000
```

Kỳ vọng:

- `CheckOnly` có thể thành công (tùy cấu hình).
- Khi `ApplyLatestAsync` tải file, gặp lỗi partial → ném exception.
- Thư mục version mới không chứa file DLL hỏng (temp file phải được xóa).
- `Config/active.json` vẫn trỏ về version cũ.

## Lưu ý triển khai

- Nên tải file vào `.part` rồi mới move sang tên chính thức.
- Nếu có exception trong quá trình download hoặc verify MD5:
  - Xóa `.part`.
  - Không thay đổi active version.
