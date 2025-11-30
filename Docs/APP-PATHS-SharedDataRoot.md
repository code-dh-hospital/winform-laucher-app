# AppPaths & SharedDataRoot (v13)

Mục tiêu:

- Chuẩn hóa cách lấy BinaryRoot (thư mục chứa exe phiên bản hiện tại).
- Chuẩn hóa DataRoot dùng chung cho nhiều version (SharedDataRoot).
- Giảm tối đa việc nghiệp vụ phải biết chi tiết launcher/version.

## 1. AppPaths

```csharp
public static class AppPaths
{
    public static string GetBinaryRoot();
    public static string GetSharedDataRoot(string appName);
    public static string MapExeRelativeToData(string appName, string relativePath);
}
```

- `GetBinaryRoot()` → `AppDomain.CurrentDomain.BaseDirectory` (thường là `...\Versions\x.y.z.w`).
- `GetSharedDataRoot(appName)` →
  - `%LOCALAPPDATA%\DhLauncherApps\{appName}\Data`
  - Tự tạo thư mục nếu chưa có.
- `MapExeRelativeToData(appName, relativePath)` → dùng để map file “trước đây ghi cạnh exe” sang DataRoot.

## 2. AppLauncher & CurrentDirectory

Trong `AppLauncher.Run(...)`:

- Sau khi đọc `launcher.json` và có `AppName`, launcher sẽ:
  - Tính `SharedDataRoot = AppPaths.GetSharedDataRoot(AppName)`
  - Set `Environment.CurrentDirectory = SharedDataRoot`

=> Hệ quả:

- Đường dẫn tương đối `File.WriteAllText("config.json", ...)` sẽ ghi vào SharedDataRoot.
- Binary vẫn chạy trong folder version → logic update/rollback không bị ảnh hưởng.

## 3. Demo trong ConsoleTest

`Dh.Launcher.ConsoleTest` in ra:

- BinaryRoot
- LocalRoot (từ AppEnvironment)
- SharedDataRoot
- CurrentDirectory

và thử ghi 3 file:

1. `console_exedir_test.txt` – cạnh exe (BinaryRoot).
2. `console_relative_test.txt` – theo `CurrentDirectory` (SharedDataRoot).
3. `console_mapped_test.txt` – thông qua `AppPaths.MapExeRelativeToData`.

## 4. Hướng dẫn refactor nghiệp vụ hiện tại

- Code cũ:
  - Nếu dùng đường dẫn tương đối → đã mặc định ghi vào SharedDataRoot (sau khi launcher set CurrentDirectory).
  - Nếu dùng `AppDomain.CurrentDomain.BaseDirectory` để ghi log/config → nên chuyển dần sang:
    - `AppPaths.MapExeRelativeToData(appName, "logs\...")`

Ưu điểm:

- Các version mới/cũ có thể chia sẻ chung data.
- Không còn phụ thuộc quyền ghi ở Program Files.
- Cấu trúc thư mục Data rõ ràng, dễ backup/chẩn đoán.
