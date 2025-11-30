# API sử dụng trực tiếp `UpdateManager` (manual check/apply)

Ngoài việc dùng `AppLauncher.Run(...)`, bạn có thể gọi trực tiếp `UpdateManager` để tự control luồng kiểm tra & áp dụng cập nhật.

## 1. Các hàm mới

```csharp
public Task<UpdateResult> CheckOnlyAsync(CancellationToken cancellationToken)
public Task<UpdateResult> ApplyLatestAsync(CancellationToken cancellationToken)
```

### `CheckOnlyAsync(...)`

- Bên trong sẽ set tạm `_options.DryRunOnly = true` rồi gọi lại `CheckAndUpdateAsync(...)`.
- Không chép file, không đổi active version.
- Dùng để:
  - Lấy thông tin version mới nhất (nếu có).
  - Xem `UpdateEnforcementLevel` mà server yêu cầu (`silent|ask|force`).
  - Nhận các event: `SummaryChangedFilesAvailable`, `ChangedFilesSummaryAvailable`, `UpdateSummaryAvailable`… để hiển thị UI preview.

### `ApplyLatestAsync(...)`

- Set tạm `_options.DryRunOnly = false` rồi gọi lại `CheckAndUpdateAsync(...)`.
- Nếu có bản mới → sẽ load manifest, tải file, verify MD5, update active version.
- Nếu không có bản mới → chỉ check rồi thoát, không chép file.

> Lưu ý: Cả hai hàm đều trả về `UpdateResult` giống như `CheckAndUpdateAsync` để bạn có thể tận dụng chung logic xử lý kết quả.

---

## 2. Cách sử dụng trong ứng dụng Console

Ví dụ trong một app console riêng (không phải AppLauncher.Run), bạn muốn làm nút "Check update" thủ công:

```csharp
using System;
using System.Threading;
using Dh.AppLauncher.Core.CoreEnvironment;
using Dh.AppLauncher.Core.Update;

class ManualUpdateSample
{
    static void Main(string[] args)
    {
        var env = AppEnvironment.CreateFromDefaultRoot("Dh.Updater.SampleApp");
        var cfg = env.GetConfigSnapshot();

        var opt = new UpdateOptions();
        opt.ManifestUrls = cfg.LatestManifestUrls;
        opt.KeepVersions = cfg.KeepVersions;
        opt.MaxParallelDownloads = 3;
        opt.MaxUpdateAttemptsPerVersion = cfg.MaxUpdateAttemptsPerVersion;
        opt.FailedVersionRetryMinutes = cfg.FailedVersionRetryMinutes;
        opt.AllowDowngrade = cfg.AllowDowngrade;
        opt.DefaultUpdateLevel = UpdateEnforcementLevel.Silent;

        var mgr = new UpdateManager(env, opt);

        // Đăng ký event để xem file nào sẽ đổi
        mgr.SummaryChangedFilesAvailable += (s, e) =>
        {
            Console.WriteLine("PLAN: NewVersion={0}, DryRun={1}, ChangedFiles={2}, PlanBytes={3}",
                e.NewVersion,
                e.IsDryRun,
                e.ChangedFiles.Count,
                e.TotalPlannedDownloadBytes);
        };

        Console.WriteLine("=== CHECK ONLY ===");
        var checkResult = mgr.CheckOnlyAsync(CancellationToken.None).GetAwaiter().GetResult();
        Console.WriteLine("CheckOnly: cur={0}, new={1}, level={2}, updateApplied={3}",
            checkResult.CurrentVersion,
            checkResult.NewVersion,
            checkResult.EnforcementLevel,
            checkResult.UpdateApplied);

        if (string.IsNullOrWhiteSpace(checkResult.NewVersion))
        {
            Console.WriteLine("Không có bản mới, thoát.");
            return;
        }

        Console.Write("Có bản mới {0}. Bạn có muốn tải & áp dụng không? (Y/N): ", checkResult.NewVersion);
        var key = Console.ReadKey();
        Console.WriteLine();
        if (key.Key == ConsoleKey.N)
        {
            Console.WriteLine("Không áp dụng, dùng version hiện tại.");
            return;
        }

        Console.WriteLine("=== APPLY LATEST ===");
        var applyResult = mgr.ApplyLatestAsync(CancellationToken.None).GetAwaiter().GetResult();
        Console.WriteLine("Apply: cur={0}, new={1}, updateApplied={2}",
            applyResult.CurrentVersion,
            applyResult.NewVersion,
            applyResult.UpdateApplied);
    }
}
```

### Cách kiểm thử

1. Copy đoạn code trên vào một project Console .NET Framework 4.5, add reference tới `Dh.AppLauncher.Core.dll`.
2. Chép thư mục `Samples/LocalAppData/Dh.Updater.SampleApp` vào đúng `%LOCALAPPDATA%\Dh.Updater.SampleApp`.
3. Chỉnh file manifest trong `Config/manifests` (ví dụ `manifest-1.2.0.0.json`) để version mới hơn version đang active.
4. Chạy console:
   - Xem log `PLAN` từ event `SummaryChangedFilesAvailable`.
   - Nhấn `Y` để cho phép `ApplyLatestAsync` chép file.
   - Kiểm tra lại thư mục `Versions` và file `Config\active.json` xem version đã đổi chưa.

---

## 3. Cách sử dụng trong WinForms (nút "Check update")

Giả sử bạn có form chính `MainForm`, có một nút `btnCheckUpdate` và listbox `lstLog` để xem log kết quả.

```csharp
using System;
using System.Threading;
using System.Windows.Forms;
using Dh.AppLauncher.Core.CoreEnvironment;
using Dh.AppLauncher.Core.Update;

public partial class MainForm : Form
{
    private AppEnvironment _env;
    private UpdateManager _updateManager;

    public MainForm()
    {
        InitializeComponent();
        InitUpdateManager();
    }

    private void InitUpdateManager()
    {
        _env = AppEnvironment.CreateFromDefaultRoot("Dh.Updater.SampleApp");
        var cfg = _env.GetConfigSnapshot();

        var opt = new UpdateOptions();
        opt.ManifestUrls = cfg.LatestManifestUrls;
        opt.KeepVersions = cfg.KeepVersions;
        opt.MaxParallelDownloads = 3;
        opt.MaxUpdateAttemptsPerVersion = cfg.MaxUpdateAttemptsPerVersion;
        opt.FailedVersionRetryMinutes = cfg.FailedVersionRetryMinutes;
        opt.AllowDowngrade = cfg.AllowDowngrade;
        opt.DefaultUpdateLevel = UpdateEnforcementLevel.Silent;

        _updateManager = new UpdateManager(_env, opt);
        _updateManager.SummaryChangedFilesAvailable += (s, e) =>
        {
            this.BeginInvoke(new Action(() =>
            {
                lstLog.Items.Add(string.Format("PLAN v{0}, files={1}, bytes={2}",
                    e.NewVersion, e.ChangedFiles.Count, e.TotalPlannedDownloadBytes));
            }));
        };
    }

    private void btnCheckUpdate_Click(object sender, EventArgs e)
    {
        try
        {
            var check = _updateManager.CheckOnlyAsync(CancellationToken.None).GetAwaiter().GetResult();
            lstLog.Items.Add(string.Format("CheckOnly: cur={0}, new={1}, level={2}",
                check.CurrentVersion, check.NewVersion, check.EnforcementLevel));

            if (string.IsNullOrWhiteSpace(check.NewVersion))
            {
                MessageBox.Show("Hiện không có bản cập nhật mới.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                "Có bản cập nhật mới " + check.NewVersion + "\nBạn có muốn tải & áp dụng không?",
                "Update available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }

            var apply = _updateManager.ApplyLatestAsync(CancellationToken.None).GetAwaiter().GetResult();
            lstLog.Items.Add(string.Format("Apply: cur={0}, new={1}, applied={2}",
                apply.CurrentVersion, apply.NewVersion, apply.UpdateApplied));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi update: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
```

> Lưu ý: ví dụ trên là **manual** không đi qua `AppLauncher.Run`, phù hợp với những app muốn toàn quyền điều khiển khi nào check & khi nào apply.

---

## 4. Khi nào nên dùng `AppLauncher.Run`, khi nào nên dùng `UpdateManager` trực tiếp

- **Dùng `AppLauncher.Run` khi**:
  - Bạn có cấu trúc launcher + core app, cần self-update + tạo version folder + active version tự động.
  - Muốn tận dụng toàn bộ flow đã dựng sẵn (auto-check, timer, enforcement force, assembly resolve…).
- **Dùng `UpdateManager` trực tiếp khi**:
  - App của bạn không phải dạng launcher-core truyền thống, chỉ cần cơ chế download/update file đơn giản.
  - Bạn muốn custom UI phức tạp (wizard update, chọn file, pause/resume…) và chỉ cần engine tải/verify + manifest logic.

