# TESTING – HTTP Retry & Timeout (v12)

v12 bổ sung một số tham số trong `UpdateOptions` để control HTTP:

```csharp
public int HttpTimeoutSeconds { get; set; } = 20;
public int HttpRetryCount { get; set; } = 3;
public int HttpRetryDelayMs { get; set; } = 500;
public bool HttpRetryExponential { get; set; } = true;
```

Engine sử dụng helper:

```csharp
private async Task<byte[]> DownloadFileWithPolicyAsync(Uri uri, CancellationToken ct)
```

- Mỗi URL sẽ được retry tối đa `HttpRetryCount` lần.
- Mỗi lần retry sẽ delay `HttpRetryDelayMs` (hoặc exponential nếu `HttpRetryExponential = true`).
- Một request đơn lẻ có timeout `HttpTimeoutSeconds`.

## Scenario test liên quan

- `SlowTimeout`:
  - Dùng endpoint `/manifest/partial/latest.json` + `/files/partial/...` từ `ManifestTestServer`.
  - Kỳ vọng: download lỗi do timeout/connection reset, engine log lỗi & không ghi đè active version.

- `FlakyRetrySuccess`:
  - Dùng endpoint `/files/flaky/...`:
    - Lần 1: 500.
    - Lần 2: trả payload OK.
  - Kỳ vọng: engine retry và cuối cùng thành công.

- `PartialDownload`:
  - Sử dụng `/files/partial/...` để server đóng kết nối giữa chừng.
  - Kỳ vọng: engine coi là lỗi download; không để lại file hỏng ở thư mục version.
