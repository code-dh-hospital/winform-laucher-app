# MigrationHelper – Hỗ trợ IO cho app cũ & bootstrap lần đầu (v14)

Mục tiêu:

- Cho phép app cũ đang quen ghi/đọc file cạnh exe chuyển dần sang SharedDataRoot mà không cần sửa quá nhiều.
- Khi đọc file: nếu đã có bản ở DataRoot thì dùng, nếu chưa thì tìm trong thư mục launcher cũ, copy sang, rồi dùng DataRoot.
- Lần chạy đầu tiên của launcher: tự động backup thư mục legacy thành version đầu tiên và data chung.

---

## 1. API chính

Namespace: `Dh.AppLauncher.Core.CoreEnvironment`

```csharp
public static class AppPaths
{
    public static string GetBinaryRoot();
    public static string GetSharedDataRoot(string appName);
    public static string MapExeRelativeToData(string appName, string relativePath);
}

public static class MigrationHelper
{
    public static string EnsureOnDataRoot(string appName, string relativePath, string legacyRoot = null);
    public static string GetDataPathForWrite(string appName, string relativePath);
    public static void TryInitialBootstrapFromLegacy(AppEnvironment env, string appName, string legacyRoot = null);
}
```

### 1.1. `EnsureOnDataRoot` – đọc file theo kiểu "migrate-on-read"

Use-case: Bạn có code cũ đọc file `config.exe.xml` cạnh exe.

Thay vì:

```csharp
var exeDir = AppDomain.CurrentDomain.BaseDirectory;
var path = Path.Combine(exeDir, "config.exe.xml");
var xml = File.ReadAllText(path);
```

Bạn có thể chuyển sang:

```csharp
string path = MigrationHelper.EnsureOnDataRoot("Dh.Updater.SampleApp", "config.exe.xml");
string xml = File.Exists(path) ? File.ReadAllText(path) : null;
```

Hành vi:

1. Tính `dataPath = AppPaths.MapExeRelativeToData(appName, relativePath)`.
2. Nếu file đã tồn tại ở DataRoot → trả về luôn.
3. Nếu chưa có:
   - Xác định `legacyRoot`:
     - Nếu không truyền → mặc định `AppPaths.GetBinaryRoot()` (folder launcher cũ).
   - Nếu `legacyRoot` tồn tại:
     - Ghép `legacyPath = Path.Combine(legacyRoot, relativePath)`.
     - Nếu file tồn tại ở đó → copy sang `dataPath`, trả về `dataPath`.
4. Nếu không tìm thấy ở đâu → vẫn trả về `dataPath` để code phía trên tự tạo file mới nếu cần.

### 1.2. `GetDataPathForWrite` – ghi file luôn ở DataRoot

Ví dụ với log:

```csharp
string logPath = MigrationHelper.GetDataPathForWrite("Dh.Updater.SampleApp", Path.Combine("logs", "app.log"));
File.AppendAllText(logPath, message + Environment.NewLine);
```

Hàm sẽ:

- Tính path trong DataRoot.
- Tự tạo thư mục cha nếu chưa có.
- Không động tới thư mục legacy.

---

## 2. Bootstrap lần đầu – `TryInitialBootstrapFromLegacy`

Được gọi tự động trong `AppLauncher.Run(...)`:

```csharp
var env = AppEnvironment.CreateFromDefaultRoot(configSnapshot.AppName);
MigrationHelper.TryInitialBootstrapFromLegacy(env, configSnapshot.AppName, null);
```

Luồng xử lý:

1. Nếu `Config/active.json` đã tồn tại → không làm gì (đã từng migrate / update).
2. Nếu chưa có:
   - Xác định:
     - `localRoot = env.GetLocalRoot()`
     - `configDir = localRoot/Config`
     - `versionsDir = localRoot/Versions`
     - `legacyRoot`:
       - Nếu truyền `null` → dùng `AppPaths.GetBinaryRoot()` (thư mục launcher hiện tại).
   - Tạo version đầu tiên, ví dụ `1.0.0.0`:

     ```text
     Versions/1.0.0.0/   ← binary (exe/dll)
     DataRoot/...        ← data chung
     Config/active.json
     Config/manifests/manifest-1.0.0.0-migrated.json
     ```

3. Duyệt tất cả file trong `legacyRoot` (recursive):

   - Nếu là `.exe` hoặc `.dll`:
     - Copy vào `Versions/1.0.0.0/` giữ nguyên cấu trúc thư mục con.
   - Ngược lại:
     - Copy sang SharedDataRoot thông qua `AppPaths.MapExeRelativeToData(appName, relativePath)`.

4. Ghi `active.json` với:

   ```json
   {
     "version": "1.0.0.0",
     "changed_utc": "2025-..."
   }
   ```

5. Ghi manifest tối thiểu `manifest-1.0.0.0-migrated.json`:

   ```json
   {
     "version": "1.0.0.0",
     "sha512": "",
     "package_type": "migrated_local",
     "urls": [],
     "changelog": "Initial migrated version from legacy launcher folder.",
     "file_md5": {}
   }
   ```

> Lưu ý: manifest này chỉ dùng để đánh dấu và phục vụ chẩn đoán/offline; manifest từ server (nếu version cao hơn) sẽ override trong lần update tiếp theo.

---

## 3. Cách chèn vào nghiệp vụ hiện tại (app cũ)

### 3.1. Đọc config với fallback

Trước đây:

```csharp
var exeDir = AppDomain.CurrentDomain.BaseDirectory;
var cfgPath = Path.Combine(exeDir, "config.exe.xml");
var xml = File.ReadAllText(cfgPath);
```

Giờ:

```csharp
var cfgPath = MigrationHelper.EnsureOnDataRoot("YourAppName", "config.exe.xml");
var xml = File.Exists(cfgPath) ? File.ReadAllText(cfgPath) : null;
```

### 3.2. Ghi log theo relative path con

```csharp
string logRelative = Path.Combine("logs", "general.log");
string logPath = MigrationHelper.GetDataPathForWrite("YourAppName", logRelative);
File.AppendAllText(logPath, DateTime.Now.ToString("O") + " - " + message + Environment.NewLine);
```

### 3.3. Làm việc với thư mục con

Với data trong thư mục con, ví dụ `Data\Templates\a.txt`:

```csharp
string templateRel = Path.Combine("Templates", "a.txt");
string templatePath = MigrationHelper.EnsureOnDataRoot("YourAppName", templateRel);
// Nếu app cũ từng để Templates\a.txt cạnh exe, sẽ được copy sang DataRoot\Templates\a.txt trong lần đọc đầu tiên.
```

---

## 4. Tổng kết chiến lược migrate

1. Lần đầu chạy launcher mới:
   - `TryInitialBootstrapFromLegacy`:
     - Backup toàn bộ thư mục legacy:
       - `.exe/.dll` → version folder.
       - File khác → SharedDataRoot.
     - Tạo `active.json` + manifest migrated.

2. Trong code nghiệp vụ app cũ:
   - Dùng `MigrationHelper.EnsureOnDataRoot` cho các file **đọc** (config, template, data).
   - Dùng `MigrationHelper.GetDataPathForWrite` cho các file **ghi** (log, cache, report).
   - Dần dần refactor phần mới/quan trọng sang dùng `AppPaths.MapExeRelativeToData` trực tiếp.

