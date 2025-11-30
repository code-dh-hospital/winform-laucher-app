using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using Dh.AppLauncher.Core.CoreEnvironment;
using Dh.AppLauncher.Core.Update;

namespace Dh.Launcher.TestRunner
{
    /// <summary>
    /// Simple console test runner cho các "unit test" scenario độc lập.
    /// Mỗi scenario sẽ tạo 1 root path riêng (không dùng %LOCALAPPDATA%) để không ảnh hưởng đến dữ liệu thật.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Dh.Launcher.TestRunner v1 (mapped to solution v10)");
            Console.WriteLine("Args: " + string.Join(" ", args ?? new string[0]));

            string scenarioName = null;
            string customRoot = null;
            string baseUrl = null;
            string junitPath = null;

            // parse args dạng: --scenario=name --root=c:\temp\root --baseUrl=http://localhost:3000
            foreach (var arg in args ?? new string[0])
            {
                if (arg.StartsWith("--scenario=", StringComparison.OrdinalIgnoreCase))
                    scenarioName = arg.Substring("--scenario=".Length);
                else if (arg.StartsWith("scenario=", StringComparison.OrdinalIgnoreCase))
                    scenarioName = arg.Substring("scenario=".Length);
                else if (arg.StartsWith("--root=", StringComparison.OrdinalIgnoreCase))
                    customRoot = arg.Substring("--root=".Length);
                else if (arg.StartsWith("root=", StringComparison.OrdinalIgnoreCase))
                    customRoot = arg.Substring("root=".Length);
                else if (arg.StartsWith("--baseUrl=", StringComparison.OrdinalIgnoreCase))
                    baseUrl = arg.Substring("--baseUrl=".Length);
                else if (arg.StartsWith("baseUrl=", StringComparison.OrdinalIgnoreCase))
                    baseUrl = arg.Substring("baseUrl=".Length);
                else if (arg.StartsWith("--junit=", StringComparison.OrdinalIgnoreCase))
                    junitPath = arg.Substring("--junit=".Length);
                else if (arg.StartsWith("junit=", StringComparison.OrdinalIgnoreCase))
                    junitPath = arg.Substring("junit=".Length);
            }

            if (string.IsNullOrWhiteSpace(customRoot))
            {
                customRoot = Path.Combine(Environment.CurrentDirectory, "_TestRoots");
            }

            if (!Directory.Exists(customRoot))
                Directory.CreateDirectory(customRoot);

            var scenarios = BuildScenarios(customRoot, baseUrl);
            var results = new List<TestResult>();
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                Console.WriteLine();
                Console.WriteLine("Danh sách scenario:");
                foreach (var s in scenarios)
                {
                    Console.WriteLine("  - " + s.Name + " : " + s.Description);
                }
                Console.WriteLine();
                Console.Write("Nhập tên scenario cần chạy: ");
                scenarioName = Console.ReadLine();
            }

            var scenario = scenarios.FirstOrDefault(s => string.Equals(s.Name, scenarioName, StringComparison.OrdinalIgnoreCase));
            if (scenario == null)
            {
                Console.WriteLine("Không tìm thấy scenario: " + scenarioName);
                Console.WriteLine("Các scenario hợp lệ:");
                foreach (var s in scenarios)
                {
                    Console.WriteLine("  - " + s.Name);
                }
                Environment.Exit(1);
                return;
            }

            Console.WriteLine();
            Console.WriteLine("=== RUN SCENARIO: " + scenario.Name + " ===");
            Console.WriteLine(scenario.Description);
            Console.WriteLine("Root: " + scenario.RootPath);
            Console.WriteLine();

            try
            {
                var started = DateTime.UtcNow;
                scenario.Setup();
                scenario.Run();
                var elapsed = (DateTime.UtcNow - started).TotalSeconds;
                results.Add(new TestResult
                {
                    Name = scenario.Name,
                    Success = true,
                    Message = "OK",
                    ElapsedSeconds = elapsed
                });
                Console.WriteLine();
                Console.WriteLine("=== SCENARIO COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                var elapsed = 0.0;
                results.Add(new TestResult
                {
                    Name = scenario.Name,
                    Success = false,
                    Message = ex.Message,
                    ElapsedSeconds = elapsed
                });
                Console.WriteLine();
                Console.WriteLine("=== SCENARIO FAILED ===");
                Console.WriteLine(ex.ToString());
            }

            if (!string.IsNullOrWhiteSpace(junitPath))
            {
                try
                {
                    WriteJUnitReport(junitPath, results);
                    Console.WriteLine("JUnit report written to " + junitPath);
                }
                catch (Exception jex)
                {
                    Console.WriteLine("Failed to write JUnit report: " + jex.Message);
                }
            }

            bool anyFail = results.Exists(r => !r.Success);
            Environment.Exit(anyFail ? 2 : 0);
        }

        private static void WriteJUnitReport(string path, List<TestResult> results)
        {
            var suite = new XElement("testsuite");
            suite.SetAttributeValue("name", "DhLauncherTestRunner");
            suite.SetAttributeValue("tests", results.Count);
            suite.SetAttributeValue("failures", results.Count(r => !r.Success));

            foreach (var r in results)
            {
                var tc = new XElement("testcase");
                tc.SetAttributeValue("classname", r.Name);
                tc.SetAttributeValue("name", r.Name);
                tc.SetAttributeValue("time", r.ElapsedSeconds.ToString("0.000"));
                if (!r.Success)
                {
                    var fail = new XElement("failure");
                    fail.SetAttributeValue("message", r.Message ?? "FAIL");
                    fail.Value = r.Message ?? string.Empty;
                    tc.Add(fail);
                }
                suite.Add(tc);
            }

            var doc = new XDocument(suite);
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            doc.Save(path);
        }

        public static List<TestScenario> BuildScenarios(string customRoot, string baseUrl)
        {

            var list = new List<TestScenario>();

            // Scenario 1: NoUpdate
            list.Add(new TestScenario
            {
                Name = "NoUpdate",
                Description = "Không có bản cập nhật mới (manifest latest = active).",
                RootPath = Path.Combine(customRoot, "NoUpdate"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "NoUpdate", "1.0.0.0", "1.0.0.0", baseUrl);
                },
                Run = () =>
                {
                    RunCheckAndApply("NoUpdate", Path.Combine(customRoot, "NoUpdate"));
                }
            });

            // Scenario 2: SilentUpdateAvailable
            list.Add(new TestScenario
            {
                Name = "SilentUpdateAvailable",
                Description = "Có bản mới với update_level=silent, sẽ tự động update.",
                RootPath = Path.Combine(customRoot, "SilentUpdateAvailable"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "SilentUpdateAvailable", "1.0.0.0", "1.2.0.0", baseUrl);
                },
                Run = () =>
                {
                    RunCheckAndApply("SilentUpdateAvailable", Path.Combine(customRoot, "SilentUpdateAvailable"));
                }
            });

            // Scenario 3: AskUserDecline
            list.Add(new TestScenario
            {
                Name = "AskUserDecline",
                Description = "Bản mới với update_level=ask, ConfirmUpdateHandler giả lập người dùng từ chối.",
                RootPath = Path.Combine(customRoot, "AskUserDecline"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "AskUserDecline", "1.0.0.0", "1.1.0.0", baseUrl);
                },
                Run = () =>
                {
                    RunCheckAndApply("AskUserDecline", Path.Combine(customRoot, "AskUserDecline"), false);
                }
            });

            // Scenario 4: ForceUpdate (expect active must match or fail)
            list.Add(new TestScenario
            {
                Name = "ForceUpdate",
                Description = "Bản mới với update_level=force, bắt buộc phải update. Nếu không áp dụng, không cho chạy.",
                RootPath = Path.Combine(customRoot, "ForceUpdate"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "ForceUpdate", "1.0.0.0", "1.2.0.0", baseUrl);
                },
                Run = () =>
                {
                    RunCheckAndApply("ForceUpdate", Path.Combine(customRoot, "ForceUpdate"));
                }
            });

            // Scenario 5: BadManifestJson (cần Node server trả về JSON lỗi)
            list.Add(new TestScenario
            {
                Name = "BadManifestJson",
                Description = "Manifest JSON lỗi (cần Node manifest-server chạy baseUrl trỏ tới endpoint /bad/latest.json).",
                RootPath = Path.Combine(customRoot, "BadManifestJson"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "BadManifestJson", "1.0.0.0", "1.0.0.0", baseUrl);
                    // ép LatestManifestUrls trỏ đến server lỗi
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var cfgPath = Path.Combine(customRoot, "BadManifestJson", "Config", "launcher.json");
                        if (File.Exists(cfgPath))
                        {
                            var text = File.ReadAllText(cfgPath);
                            text = text.Replace("latest.json", "bad/latest.json");
                            File.WriteAllText(cfgPath, text);
                        }
                    }
                },
                Run = () =>
                {
                    RunCheckAndApply("BadManifestJson", Path.Combine(customRoot, "BadManifestJson"));
                }
            });

            // Scenario 6: Md5Mismatch (cần Node server)
            list.Add(new TestScenario
            {
                Name = "Md5Mismatch",
                Description = "Server trả file có MD5 không khớp với manifest (dùng endpoint /manifest/md5-mismatch/latest.json).",
                RootPath = Path.Combine(customRoot, "Md5Mismatch"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "Md5Mismatch", "1.0.0.0", "1.0.0.0", baseUrl);
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var cfgPath = Path.Combine(customRoot, "Md5Mismatch", "Config", "launcher.json");
                        if (File.Exists(cfgPath))
                        {
                            var text = File.ReadAllText(cfgPath);
                            text = text.Replace("latest.json", "md5-mismatch/latest.json");
                            File.WriteAllText(cfgPath, text);
                        }
                    }
                },
                Run = () =>
                {
                    try
                    {
                        RunCheckAndApply("Md5Mismatch", Path.Combine(customRoot, "Md5Mismatch"));
                        Console.WriteLine("[Md5Mismatch] WARNING: không thấy lỗi như kỳ vọng.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Md5Mismatch] Expected failure: " + ex.Message);
                    }
                }
            });

            // Scenario 7: MirrorFailThenOk (cần Node server)
            list.Add(new TestScenario
            {
                Name = "MirrorFailThenOk",
                Description = "URL mirror đầu lỗi, URL thứ hai OK (endpoint /manifest/mirror/latest.json).",
                RootPath = Path.Combine(customRoot, "MirrorFailThenOk"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "MirrorFailThenOk", "1.0.0.0", "1.0.0.0", baseUrl);
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var cfgPath = Path.Combine(customRoot, "MirrorFailThenOk", "Config", "launcher.json");
                        if (File.Exists(cfgPath))
                        {
                            var text = File.ReadAllText(cfgPath);
                            text = text.Replace("latest.json", "mirror/latest.json");
                            File.WriteAllText(cfgPath, text);
                        }
                    }
                },
                Run = () =>
                {
                    RunCheckAndApply("MirrorFailThenOk", Path.Combine(customRoot, "MirrorFailThenOk"));
                }
            });

            // Scenario 8: ClientMatchGroupsTags (cần Node server)
            list.Add(new TestScenario
            {
                Name = "ClientMatchGroupsTags",
                Description = "Manifest chỉ áp dụng cho client thuộc group HOSPITAL-A + tag beta.",
                RootPath = Path.Combine(customRoot, "ClientMatchGroupsTags"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "ClientMatchGroupsTags", "1.0.0.0", "1.0.0.0", baseUrl);
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var cfgPath = Path.Combine(customRoot, "ClientMatchGroupsTags", "Config", "launcher.json");
                        if (File.Exists(cfgPath))
                        {
                            var text = File.ReadAllText(cfgPath);
                            text = text.Replace("latest.json", "groups/latest.json");
                            File.WriteAllText(cfgPath, text);
                        }
                    }
                    // tạo client_identity.json với groups/tags phù hợp
                    var clientIdentityPath = Path.Combine(customRoot, "ClientMatchGroupsTags", "Config", "client_identity.json");
                    var identityObj = new
                    {
                        client_id = "test-client-groups",
                        created_utc = DateTime.UtcNow.ToString("o"),
                        machine_name = Environment.MachineName,
                        local_root = Path.Combine(customRoot, "ClientMatchGroupsTags", "Dh.Updater.SampleApp"),
                        groups = new[] { "HOSPITAL-A" },
                        tags = new[] { "beta" }
                    };
                    File.WriteAllText(clientIdentityPath, Newtonsoft.Json.JsonConvert.SerializeObject(identityObj, Newtonsoft.Json.Formatting.Indented));
                },
                Run = () =>
                {
                    RunCheckAndApply("ClientMatchGroupsTags", Path.Combine(customRoot, "ClientMatchGroupsTags"));
                }
            });

            // Scenario 9: SlowTimeout (cần ManifestTestServer)
            list.Add(new TestScenario
            {
                Name = "SlowTimeout",
                Description = "Server trả manifest OK nhưng file trả chậm để test timeout.",
                RootPath = Path.Combine(customRoot, "SlowTimeout"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "SlowTimeout", "1.0.0.0", "1.0.0.0", baseUrl);
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var cfgPath = Path.Combine(customRoot, "SlowTimeout", "Config", "launcher.json");
                        if (File.Exists(cfgPath))
                        {
                            var text = File.ReadAllText(cfgPath);
                            text = text.Replace("latest.json", "partial/latest.json");
                            File.WriteAllText(cfgPath, text);
                        }
                    }
                },
                Run = () =>
                {
                    try
                    {
                        RunCheckAndApply("SlowTimeout", Path.Combine(customRoot, "SlowTimeout"));
                        Console.WriteLine("[SlowTimeout] WARNING: không thấy timeout như kỳ vọng.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[SlowTimeout] Expected timeout/failure: " + ex.Message);
                    }
                }
            });

            // Scenario 10: FlakyRetrySuccess (cần ManifestTestServer)
            list.Add(new TestScenario
            {
                Name = "FlakyRetrySuccess",
                Description = "File đầu lỗi, retry lần sau thành công (endpoint /files/flaky).",
                RootPath = Path.Combine(customRoot, "FlakyRetrySuccess"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "FlakyRetrySuccess", "1.0.0.0", "1.0.0.0", baseUrl);
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var cfgPath = Path.Combine(customRoot, "FlakyRetrySuccess", "Config", "launcher.json");
                        if (File.Exists(cfgPath))
                        {
                            var text = File.ReadAllText(cfgPath);
                            text = text.Replace("latest.json", "latest.json");
                            // manifest-1.2.0.0-ok.json đã trỏ tới /files/ok, để test flaky ta có thể sửa URL fileMd5 bằng tay nếu cần.
                            File.WriteAllText(cfgPath, text);
                        }
                    }
                },
                Run = () =>
                {
                    RunCheckAndApply("FlakyRetrySuccess", Path.Combine(customRoot, "FlakyRetrySuccess"));
                }
            });

            // Scenario 11: PartialDownload (cần ManifestTestServer)
            list.Add(new TestScenario
            {
                Name = "PartialDownload",
                Description = "Server đóng kết nối giữa chừng khi tải file, dùng endpoint /files/partial.",
                RootPath = Path.Combine(customRoot, "PartialDownload"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "PartialDownload", "1.0.0.0", "1.0.0.0", baseUrl);
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var cfgPath = Path.Combine(customRoot, "PartialDownload", "Config", "launcher.json");
                        if (File.Exists(cfgPath))
                        {
                            var text = File.ReadAllText(cfgPath);
                            text = text.Replace("latest.json", "partial/latest.json");
                            File.WriteAllText(cfgPath, text);
                        }
                    }
                },
                Run = () =>
                {
                    try
                    {
                        RunCheckAndApply("PartialDownload", Path.Combine(customRoot, "PartialDownload"));
                        Console.WriteLine("[PartialDownload] WARNING: không thấy lỗi partial như kỳ vọng.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[PartialDownload] Expected failure: " + ex.Message);
                    }
                }
            });

            
            // Scenario: ZipPackageOk (ZIP tổng OK, không cần tải per-file)
            list.Add(new TestScenario
            {
                Name = "ZipPackageOk",
                Description = "Manifest có zip_urls + sha512 chuẩn, ZIP được tải và giải nén thành công, không cần per-file.",
                RootPath = Path.Combine(customRoot, "ZipPackageOk"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "ZipPackageOk", "1.0.0.0", "1.2.3.4", baseUrl);
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var cfgPath = Path.Combine(customRoot, "ZipPackageOk", "Config", "launcher.json");
                        if (File.Exists(cfgPath))
                        {
                            var text = File.ReadAllText(cfgPath);
                            text = text.Replace("latest.json", "zip/ok/latest.json");
                            File.WriteAllText(cfgPath, text);
                        }
                    }
                },
                Run = () =>
                {
                    RunCheckAndApply("ZipPackageOk", Path.Combine(customRoot, "ZipPackageOk"));
                }
            });

            // Scenario: ZipPackageFailThenPerFileOk (ZIP lỗi, fallback per-file)
            list.Add(new TestScenario
            {
                Name = "ZipPackageFailThenPerFileOk",
                Description = "Manifest có zip_urls nhưng ZIP lỗi, engine fallback về per-file (endpoint /manifest/zip/fail-perfile/latest.json).",
                RootPath = Path.Combine(customRoot, "ZipPackageFailThenPerFileOk"),
                Setup = () =>
                {
                    PrepareBasicSampleRoot(customRoot, "ZipPackageFailThenPerFileOk", "1.0.0.0", "2.0.0.0", baseUrl);
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var cfgPath = Path.Combine(customRoot, "ZipPackageFailThenPerFileOk", "Config", "launcher.json");
                        if (File.Exists(cfgPath))
                        {
                            var text = File.ReadAllText(cfgPath);
                            text = text.Replace("latest.json", "zip/fail-perfile/latest.json");
                            File.WriteAllText(cfgPath, text);
                        }
                    }
                },
                Run = () =>
                {
                    RunCheckAndApply("ZipPackageFailThenPerFileOk", Path.Combine(customRoot, "ZipPackageFailThenPerFileOk"));
                }
            });


            // Scenario: ZipPackageBadMd5ThenPerFileOk (ZIP sha512 ok nhưng MD5 một file sai, fallback per-file)
            list.Add(new TestScenario
            {
                Name = "ZipPackageBadMd5ThenPerFileOk",
                Description = "ZIP tải và sha512 đúng, nhưng MD5 của ít nhất một file trong ZIP khác với manifest => engine rollback ZIP và fallback per-file.",
                RootPath = Path.Combine(customRoot, "ZipPackageBadMd5ThenPerFileOk"),
                Setup = () =>
                {
                    // active ban đầu 1.0.0.0, latest 3.0.0.0
                    PrepareBasicSampleRoot(customRoot, "ZipPackageBadMd5ThenPerFileOk", "1.0.0.0", "3.0.0.0", baseUrl);
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var cfgPath = Path.Combine(customRoot, "ZipPackageBadMd5ThenPerFileOk", "Config", "launcher.json");
                        if (File.Exists(cfgPath))
                        {
                            var text = File.ReadAllText(cfgPath);
                            text = text.Replace("latest.json", "zip/bad-md5/latest.json");
                            File.WriteAllText(cfgPath, text);
                        }
                    }
                },
                Run = () =>
                {
                    RunCheckAndApply("ZipPackageBadMd5ThenPerFileOk", Path.Combine(customRoot, "ZipPackageBadMd5ThenPerFileOk"));
                }
            });

return list;
        }

        private static void PrepareBasicSampleRoot(string customRoot, string scenarioName, string activeVersion, string latestVersion, string baseUrl)
        {
            var root = Path.Combine(customRoot, scenarioName);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
            Directory.CreateDirectory(root);

            var appRoot = Path.Combine(root, "Dh.Updater.SampleApp");
            Directory.CreateDirectory(appRoot);
            var cfgDir = Path.Combine(appRoot, "Config");
            var manifestsDir = Path.Combine(cfgDir, "manifests");
            Directory.CreateDirectory(cfgDir);
            Directory.CreateDirectory(manifestsDir);
            var versionsDir = Path.Combine(appRoot, "Versions");
            Directory.CreateDirectory(versionsDir);

            // active.json
            var activeObj = new
            {
                version = activeVersion,
                changed_utc = DateTime.UtcNow.ToString("o")
            };
            File.WriteAllText(Path.Combine(cfgDir, "active.json"), Newtonsoft.Json.JsonConvert.SerializeObject(activeObj, Newtonsoft.Json.Formatting.Indented));

            // launcher.json (chỉ thông tin cơ bản, ManifestUrls sẽ dùng baseUrl nếu có)
            string manifestBase;
            if (!string.IsNullOrWhiteSpace(baseUrl))
                manifestBase = baseUrl.TrimEnd('/') + "/manifest";
            else
                manifestBase = "https://update.example.com/sampleapp";

            var launcherObj = new
            {
                app_name = "Dh.Updater.SampleApp",
                latest_manifest_urls = new[]
                {
                    manifestBase + "/latest.json"
                },
                keep_versions = 3,
                auto_check_updates = false,
                check_interval_minutes = 60,
                max_update_attempts_per_version = 3,
                failed_version_retry_minutes = 30,
                allow_downgrade = false,
                default_update_level = "silent"
            };
            File.WriteAllText(Path.Combine(cfgDir, "launcher.json"), Newtonsoft.Json.JsonConvert.SerializeObject(launcherObj, Newtonsoft.Json.Formatting.Indented));

            // manifests: copy từ Samples trong solution nếu có
            var sampleManifestRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Samples", "LocalAppData", "Dh.Updater.SampleApp", "Config", "manifests");
            try
            {
                if (Directory.Exists(sampleManifestRoot))
                {
                    foreach (var file in Directory.GetFiles(sampleManifestRoot, "*.json"))
                    {
                        var name = Path.GetFileName(file);
                        File.Copy(file, Path.Combine(manifestsDir, name), true);
                    }
                }
            }
            catch { }

            // versions folder sample
            Directory.CreateDirectory(Path.Combine(versionsDir, activeVersion));
            File.WriteAllText(Path.Combine(versionsDir, activeVersion, "version.txt"), "Sample core for " + scenarioName + " v" + activeVersion);
        }

        private static void RunCheckAndApply(string scenarioName, string root)
        {
            RunCheckAndApply(scenarioName, root, true);
        }

        private static void RunCheckAndApply(string scenarioName, string root, bool askUserAccept)
        {
            var appRoot = Path.Combine(root, "Dh.Updater.SampleApp");
            var env = AppEnvironment.CreateFromCustomRoot(root, "Dh.Updater.SampleApp");
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
            mgr.SummaryChangedFilesAvailable += (s, e) =>
            {
                Console.WriteLine("[{0}] PLAN: NewVersion={1}, DryRun={2}, ChangedFiles={3}, PlanBytes={4}",
                    scenarioName,
                    e.NewVersion,
                    e.IsDryRun,
                    e.ChangedFiles != null ? e.ChangedFiles.Count : 0,
                    e.TotalPlannedDownloadBytes);
            };

            Console.WriteLine("[{0}] === CHECK ONLY ===", scenarioName);
            var check = mgr.CheckOnlyAsync(CancellationToken.None).GetAwaiter().GetResult();
            Console.WriteLine("[{0}] CheckOnly: cur={1}, new={2}, level={3}, applied={4}",
                scenarioName, check.CurrentVersion, check.NewVersion, check.EnforcementLevel, check.UpdateApplied);

            if (string.IsNullOrWhiteSpace(check.NewVersion))
            {
                Console.WriteLine("[{0}] Không có bản mới, kết thúc.", scenarioName);
                return;
            }

            if (check.EnforcementLevel == UpdateEnforcementLevel.AskUser && !askUserAccept)
            {
                Console.WriteLine("[{0}] Giả lập người dùng từ chối update (AskUser).", scenarioName);
                return;
            }

            Console.WriteLine("[{0}] === APPLY LATEST ===", scenarioName);
            var apply = mgr.ApplyLatestAsync(CancellationToken.None).GetAwaiter().GetResult();
            Console.WriteLine("[{0}] Apply: cur={1}, new={2}, applied={3}", scenarioName, apply.CurrentVersion, apply.NewVersion, apply.UpdateApplied);
        }
    }

    internal class TestResult
    {
        public string Name { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public double ElapsedSeconds { get; set; }
    }

    public class TestScenario
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string RootPath { get; set; }
        public Action Setup { get; set; }
        public Action Run { get; set; }
    }
}
