# DhLauncherSolution – CHANGELOG

## v7
- Mở rộng `client_match` trong manifest: thêm trường `os_versions`, `groups`, `tags` cho rollout phức tạp trong tương lai.
- Cập nhật logic lọc manifest `IsManifestApplicableToClient` để xét thêm hệ điều hành (`os_versions`) bên cạnh `client_ids` và `machine_names`.
- Console test app bổ sung bước hỏi Y/N trên console khi `update_level=ask` thông qua event `UpdateEnforcementEvaluated`.
- Bổ sung bộ manifest mẫu phong phú hơn (1.0.0.0, 1.1.0.0, 1.2.0.0) minh họa `silent`, `ask` + `client_match`, và `force`.

## v8
- Tách luồng **check-only** và **apply** cập nhật ngay trong `AppLauncher.Run` bằng 2 phase:
  - Phase 1: gọi `UpdateManager` với `DryRunOnly = true` để lập plan + bắn events + xác định `update_level` (không chép file).
  - Phase 2: nếu `ConfirmUpdateHandler` cho phép, gọi lại `UpdateManager` với `DryRunOnly = false` để thực thi tải và áp dụng.
- Thêm static delegate `AppLauncher.ConfirmUpdateHandler(UpdateEnforcementInfoEventArgs)` để UI (Console/WinForms) can thiệp quyết định có áp dụng update hay không (đặc biệt với `update_level = ask`).
- `UpdateManager`:
  - Không còn ghi nhận attempt thành công cho nhánh `DryRunOnly` (không ảnh hưởng backoff).
  - Mở rộng lọc manifest theo `client_match.groups` và `client_match.tags` dựa trên `client_identity.json`.
- `AppEnvironment`:
  - Bổ sung `ClientIdentityInfo.Groups` và `ClientIdentityInfo.Tags` + getters `GetClientGroups()` và `GetClientTags()` để installer/admin có thể pre-config nhóm/tag cho client.
- Console/WinForms sample:
  - Console: hỏi Y/N khi `AskUser`, dùng `ConfirmUpdateHandler` để quyết định.
  - WinForms: hiển thị `MessageBox` Yes/No khi `AskUser`.

## v9
- Bổ sung API public trên `UpdateManager`:
  - `CheckOnlyAsync(CancellationToken)` – thực hiện kiểm tra cập nhật ở chế độ dry-run mà không chép file.
  - `ApplyLatestAsync(CancellationToken)` – kiểm tra và áp dụng bản mới nhất theo `UpdateOptions` hiện tại.
- Viết tài liệu hướng dẫn chi tiết trong `Docs/API-UpdateManager-ManualCheckApply.md` với ví dụ Console và WinForms:
  - Cách khởi tạo `AppEnvironment` và `UpdateManager` thủ công.
  - Cách đăng ký events để xem danh sách file thay đổi và dung lượng download.
  - Cách kiểm thử bằng manifest sample trong thư mục `Samples/LocalAppData`.
- Giúp tách bạch rõ ràng hơn giữa hai use-case:
  - Dùng `AppLauncher.Run` cho mô hình launcher/core truyền thống.
  - Dùng `UpdateManager` trực tiếp cho ứng dụng cần custom UI update riêng.

## v10
- Tạo project console `Dh.Launcher.TestRunner` để chạy các "unit test" scenario độc lập qua command-line:
  - Mỗi scenario sử dụng root path riêng (`CreateFromCustomRoot`) không ảnh hưởng tới `%LOCALAPPDATA%` thật.
  - Hỗ trợ tham số `--scenario`, `--root`, `--baseUrl` tiện cho tự động hóa/CI.
- Thêm các scenario mẫu: `NoUpdate`, `SilentUpdateAvailable`, `AskUserDecline`, `ForceUpdate`, `BadManifestJson`.
- Thêm `AppEnvironment.CreateFromCustomRoot(customRootPath, appName)` để phục vụ test/tool tùy biến.
- Bổ sung thư mục `ManifestTestServer` (Node.js + Express) để giả lập server manifest:
  - Endpoint manifest OK, JSON lỗi, HTTP 500, slow response.
- Viết tài liệu `Docs/TESTING-Scenarios-And-ManifestServer.md` mô tả cách build, chạy TestRunner và ManifestTestServer, kèm ví dụ lệnh cho automation.

## v11
- Mở rộng `ManifestTestServer` (Node.js):
  - Thêm manifest:
    - `manifest-1.2.0.0-md5-mismatch.json` – dùng để test lỗi MD5 không khớp.
    - `manifest-1.2.0.0-mirror.json` – dùng để test nhiều URL mirror (1 URL lỗi, 1 URL OK).
    - `manifest-1.2.0.0-groups.json` – dùng để test `client_match` theo `groups` và `tags`.
  - Thêm endpoint file thực tế:
    - `/files/ok/1.2.0.0/Dh.Updater.DebugConsole.Core.dll` – payload có MD5 đúng.
    - `/files/badmd5/...` – payload sai MD5.
    - `/files/mirror/bad/...` (500) và `/files/mirror/good/...` (OK).
  - Thêm endpoint manifest mới: `/manifest/md5-mismatch/latest.json`, `/manifest/mirror/latest.json`, `/manifest/groups/latest.json`.
- Mở rộng `Dh.Launcher.TestRunner` với các scenario nâng cao:
  - `Md5Mismatch` – kỳ vọng update fail do MD5 mismatch (nhưng test coi đó là *expected failure*).
  - `MirrorFailThenOk` – kiểm tra logic thử nhiều URL mirror khi URL đầu lỗi.
  - `ClientMatchGroupsTags` – kiểm tra logic `client_match` theo `groups` + `tags` dựa trên `client_identity.json` giả lập.
- Cập nhật tài liệu `Docs/TESTING-Scenarios-And-ManifestServer.md` để mô tả cách chạy các scenario mới.

## v12
- Bổ sung HTTP timeout + retry policy trong `UpdateOptions`:
  - `HttpTimeoutSeconds`, `HttpRetryCount`, `HttpRetryDelayMs`, `HttpRetryExponential`.
  - Thêm helper `DownloadFileWithPolicyAsync(Uri, CancellationToken)` trong `UpdateManager` để tải file với retry & timeout chuẩn.
- Mở rộng `ManifestTestServer` với endpoint mô phỏng:
  - Partial download (`/files/partial/...`), flaky retry (`/files/flaky/...`), manifest `/manifest/partial/latest.json`.
- Mở rộng `Dh.Launcher.TestRunner`:
  - Thêm tham số `--junit=path` để xuất kết quả theo JUnit XML, phù hợp CI/CD.
  - Thêm các scenario mới: `SlowTimeout`, `FlakyRetrySuccess`, `PartialDownload`.
- Thêm tài liệu:
  - `Docs/TESTING-HTTP-Retry-and-Timeout.md`
  - `Docs/JUNIT-Format.md`
  - `Docs/TESTING-Partial-Download.md`

## v13
- Thêm helper `AppPaths` trong `Dh.AppLauncher.Core.CoreEnvironment` để chuẩn hóa:
  - `GetBinaryRoot()` – thư mục chứa binary/core app.
  - `GetSharedDataRoot(appName)` – thư mục Data dùng chung giữa nhiều version (trong LocalApplicationData).
  - `MapExeRelativeToData(appName, relativePath)` – hỗ trợ map các file từng ghi cạnh exe sang DataRoot.
- Cập nhật `AppLauncher.Run`:
  - Sau khi xác định `AppName`, launcher sẽ set `Environment.CurrentDirectory` về SharedDataRoot.
  - Mọi đường dẫn tương đối (nếu nghiệp vụ không override) sẽ ghi vào DataRoot.
- Cập nhật sample:
  - `Dh.Launcher.ConsoleTest` in ra BinaryRoot, LocalRoot, SharedDataRoot, CurrentDirectory và ghi thử 3 file:
    - Cạnh exe
    - Tương đối (CurrentDirectory)
    - Thông qua `AppPaths.MapExeRelativeToData`.
  - `Dh.Launcher.WinFormsTest` log các path tương tự ra console và thử ghi file tương đối.
- Thêm tài liệu `Docs/APP-PATHS-SharedDataRoot.md` giải thích cách dùng AppPaths và chiến lược refactor IO.

## v14
- Đổi `AppPaths` từ `internal` sang `public` để app nghiệp vụ có thể gọi trực tiếp.
- Thêm `MigrationHelper`:
  - `EnsureOnDataRoot(appName, relativePath, legacyRoot)` – đọc file theo kiểu migrate-on-read, ưu tiên SharedDataRoot, fallback sang thư mục legacy, copy sang SharedDataRoot rồi trả về path DataRoot.
  - `GetDataPathForWrite(appName, relativePath)` – chuẩn hóa đường dẫn ghi file vào SharedDataRoot (tự tạo thư mục nếu cần).
  - `TryInitialBootstrapFromLegacy(env, appName, legacyRoot)` – backup thư mục legacy thành version đầu tiên, copy binary vào `Versions/{version}`, copy data vào SharedDataRoot, tạo `active.json` và manifest migrated.
- Tích hợp `TryInitialBootstrapFromLegacy` trong `AppLauncher.Run` để lần chạy đầu tự bootstrap nếu chưa có `active.json`.
- Thêm tài liệu `Docs/MIGRATION-Helper-IO-and-Bootstrap.md` hướng dẫn cách chèn MigrationHelper/AppPaths vào nghiệp vụ cũ.

## v15
- Bổ sung cơ chế ưu tiên tải gói ZIP tổng nếu manifest có `zip_urls` hoặc `urls` + `sha512`:
  - Thử tải ZIP từ danh sách mirror, kiểm tra SHA-512 của file ZIP.
  - Giải nén vào thư mục staging riêng cho version.
  - Verify MD5 từng file theo `manifest.Files` (map từ `file_md5`/`files`).
  - Nếu mọi thứ hợp lệ → promote staging thành thư mục version final và đặt làm active.
  - Nếu bất kỳ bước nào lỗi → xóa staging và fallback về cơ chế tải từng file như v14.
- Cập nhật `Dh.AppLauncher.Manifest.UpdateManifest`:
  - Thêm `zip_urls` trong `ManifestRaw` (JSON field).
  - Thêm property `ZipUrls` trong `UpdateManifest`.
- Cập nhật `UpdateManager`:
  - Thêm helper `GetZipUrlList`, `VerifySha512Hex`, `ComputeMd5Hex`, `DownloadZipToFileAsync`, `TryApplyZipPackageAsync`.
  - Trong `CheckAndUpdateAsync`, sau khi qua retry policy:
    - Nếu không DryRun và manifest có ZIP + sha512 → thử path ZIP trước.
    - Nếu ZIP ok → raise các event Summary/ChangedFiles và kết thúc thành công.
    - Nếu ZIP fail → ghi log cảnh báo và tiếp tục flow tải từng file như cũ.

## v16
- Bổ sung test server & scenario kiểm thử cho cơ chế ZIP-first, per-file fallback:
  - ManifestTestServer:
    - `/manifest/zip/ok/latest.json` – ZIP + sha512 + file_md5 chuẩn, dùng để test path ZIP thành công.
    - `/manifest/zip/fail-perfile/latest.json` – ZIP cố tình lỗi (sha512 sai / zip không tồn tại), kèm `file_md5` + `urls` per-file để kiểm tra fallback.
    - Thêm static serving:
      - `/static` → thư mục chứa ZIP test.
      - `/files/zipfallback` → thư mục chứa các file .exe/.dll dùng cho per-file fallback.
  - Dh.Launcher.TestRunner:
    - Scenario `ZipPackageOk` – xác nhận update dùng ZIP thành công.
    - Scenario `ZipPackageFailThenPerFileOk` – xác nhận ZIP path thất bại và engine fallback về tải từng file.
- Thêm tài liệu `Docs/TESTING-ZIP-Packages.md` mô tả cách dùng các scenario trên cho QA/CI.

## v17
- Tăng cường logging chi tiết cho luồng ZIP-first trong `UpdateManager.TryApplyZipPackageAsync`:
  - Log khi bắt đầu áp dụng gói ZIP cho một version.
  - Log khi không có ZIP URLs (bỏ qua ZIP path).
  - Log lúc verify SHA-512 của file ZIP.
  - Log trước khi giải nén ZIP và sau khi dọn dẹp file ZIP / thư mục staging.
  - Log lúc verify MD5 từng file sau khi unzip.
  - Log lúc promote thư mục staging thành thư mục version final.
- Bổ sung scenario kiểm thử bổ sung MD5 mismatch nhưng sha512 ZIP đúng:
  - ManifestTestServer:
    - `/manifest/zip/bad-md5/latest.json` – sha512 trùng với file ZIP, nhưng `file_md5` cố tình dùng MD5 của bản per-file ⇒ MD5 của file trong ZIP sẽ mismatch.
  - Dh.Launcher.TestRunner:
    - Scenario `ZipPackageBadMd5ThenPerFileOk`:
      - Engine sẽ:
        1. Tải ZIP thành công, verify sha512 OK.
        2. Unzip và phát hiện MD5 một file không khớp ⇒ rollback ZIP staging.
        3. Fallback sang tải per-file theo `file_md5.urls` và áp dụng version mới.

## v18
- Bổ sung chế độ interactive cho Dh.Launcher.TestRunner (console):
  - Khi không có tham số hoặc dùng `--interactive` / `-i`:
    - Liệt kê toàn bộ scenario (tên + mô tả).
    - Cho phép chọn số thứ tự và chạy 1 scenario ngay trong console.
- Refactor nhỏ:
  - `BuildScenarios` đổi thành `public static` để tái sử dụng từ UI.
  - `TestScenario` đổi thành `public` để WinForms project có thể gọi trực tiếp.
- Tạo project WinForms `Dh.Launcher.TestRunner.WinUI`:
  - Giao diện liệt kê danh sách scenario, hiển thị description, cho phép chọn root & baseUrl.
  - Nút `Load scenarios` gọi `Program.BuildScenarios(...)` từ Dh.Launcher.TestRunner.
  - Nút `Run selected` chạy `Setup` + `Run` cho scenario được chọn, hiển thị status và thời gian thực thi.

## v19
- Tăng cường an toàn đường dẫn & chống lỗi bất ngờ từ manifest:
  - Thêm helper `GetSafePath(root, relativePath)` trong `UpdateManager`:
    - Chuẩn hóa và kiểm tra mọi đường dẫn build từ manifest (`file_md5`, ZIP entries).
    - Nếu path chứa `..` làm chui ra ngoài `Versions/<version>` hoặc staging thì ném exception và coi manifest/ZIP lỗi, không ghi file lung tung.
  - Áp dụng `GetSafePath` cho:
    - Giải nén ZIP (`entry.FullName`).
    - Kiểm tra MD5 sau unzip.
    - Build `targetPath` khi tải từng file per-file.
    - `TryReuseFromOtherVersion` khi reuse file từ version khác.
- Dọn rác staging an toàn:
  - `AppEnvironment.CleanupStagingVersionFolders()`:
    - Xóa các thư mục `Versions/*.__zipstaging` còn sót lại do update bị dừng giữa chừng.
  - `UpdateManager.CheckAndUpdateAsync` gọi cleanup này trước mỗi lần chạy.
- Xử lý active version bị xóa nhầm:
  - Sau khi đọc `currentVersion = _env.GetActiveVersion()`:
    - Nếu thư mục tương ứng không tồn tại → ghi cảnh báo, reset `currentVersion = null` thay vì crash.
- Phòng tránh 2 instance launcher chạy song song:
  - Thêm `LauncherInstanceGuard` trong `Dh.AppLauncher`:
    - Dùng `Mutex` tên `Global\DhLauncher_<AppName>` để đảm bảo 1 instance.
  - `Dh.Launcher.ConsoleTest` và `Dh.Launcher.WinFormsTest` được cập nhật:
    - Bọc `Main` trong `using (new LauncherInstanceGuard("Dh.Updater.SampleApp"))`.
    - Nếu đã có instance khác → báo thông tin và thoát nhẹ nhàng.
