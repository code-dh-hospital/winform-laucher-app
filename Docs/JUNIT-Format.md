# JUnit XML – Dh.Launcher.TestRunner (v12)

`Dh.Launcher.TestRunner` hỗ trợ xuất kết quả theo format JUnit XML để tích hợp CI/CD.

## Cách dùng

```bash
Dh.Launcher.TestRunner.exe --scenario=SilentUpdateAvailable --junit=reports/test-results.xml
```

Hoặc chạy nhiều lần với cùng file (các run sau sẽ ghi đè).

## Cấu trúc XML

Ví dụ:

```xml
<testsuite name="DhLauncherTestRunner" tests="3" failures="1">
  <testcase classname="NoUpdate" name="NoUpdate" time="0.051" />
  <testcase classname="SilentUpdateAvailable" name="SilentUpdateAvailable" time="1.271" />
  <testcase classname="Md5Mismatch" name="Md5Mismatch" time="0.123">
    <failure message="Expected failure: MD5 mismatch">Expected failure: MD5 mismatch</failure>
  </testcase>
</testsuite>
```

- `tests`: tổng số scenario đã chạy.
- `failures`: số scenario `Success == false`.
- Mỗi `testcase` tương ứng 1 scenario.

## Tích hợp CI

Trong GitLab CI/YAML:

```yaml
artifacts:
  when: always
  reports:
    junit: reports/test-results.xml
```

Trong Jenkins, chỉ cần trỏ plugin JUnit vào file XML export từ TestRunner.
