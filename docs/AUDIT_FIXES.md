# 本轮审计修复与回归映射

本轮基于 `d2fc635362dfc5c6e9c26861e2dfb5ae662b0d37` 的复审结果实施修复。
以下记录代码变更和验证范围；不表示程序已经没有其他缺陷，也不把 Linux
诊断、API 替身或源码约束检查等同于 Windows/macOS 原生执行。

## 核心、文件和界面

| 审计方向 | 本轮修复 | 回归入口 |
| --- | --- | --- |
| worker 生命周期、关闭/ESC、启动失败 | 用实际 worker/session 所有权管理退出，线程结束前保留状态和对象；统一关闭与取消路径，回收失败启动状态 | `tests/ui_lifecycle`；真实 MFC 程序的 `tests/windows-ui` |
| Copy 和 Verify | Copy 在同步保护下取得结果快照；摘要匹配检查完整长度、十六进制字符和等值，不再接受短子串 | `tests/ui_lifecycle`、Windows native suite；真实 Copy 交互仍需原生验证 |
| 队列、线程池与异常恢复 | 生产者/消费者异常触发终止与唤醒；等待已启动任务后再释放状态，处理部分线程创建失败；入口异常也清理 working 状态 | `tests/digest_queue`、`tests/engine_failure` |
| 短读、EINTR、文件变化 | 正数短读继续累积，只有零字节读取视为 EOF；处理 EINTR；比较前后文件身份/metadata 和实际读取量，拒绝明显变化 | `tests/file_integrity`、`tests/core_runtime` |
| 路径与时间格式化 | POSIX 打开阶段防止特殊文件竞争导致阻塞，打开后检查类型和身份；时间格式化不再共享 `localtime` 静态结果 | `tests/file_integrity`；Linux wrap 注入不冒充 macOS 原生注入 |
| Windows 输入、Shell 与资源限制 | 修正含空格程序路径引用；收紧 IPC 入口；统一数量限制，增加目录遍历预算/取消；修正剪贴板所有权和版本查询边界 | `tests/windows-inputs`；原生 Explorer/UIPI/剪贴板仍需相应平台确认 |
| 字符串、SHA1 和编码 | SHA1 用固定偏移编码，避免重叠格式化；修正字符转换容量和失败处理、非法编码及 ctype 参数问题 | `tests/string_safety`、CLI 独立向量、provider 套件 |
| OpenSSL 适配器 | 加强模式/长度/空参数校验、失败后状态与复用；用专用上下文隔离外部 OpenSSL 配置；保证应用缓存先于 OpenSSL 全局清理释放，并清理静态链接库的线程资源 | `tests/openssl-provider`，含限制性配置和独立进程退出回归；实际 MFC 关闭测试 |
| CLI 输出与算法覆盖 | 特殊文件名安全显示/JSON 编码，统一错误和数量限制；复用 Windows 的 18 个注册算法、默认选择及稳定标识 | `tests/cli/test_cli.py`、`tests/cli/test_large_file.py` |

读取前后 metadata 检查用于拒绝可观察到的不稳定文件，不承诺提供文件系统快照。
MD5/SHA1 及非密码学校验算法保留为有标识的兼容选项；计算 SHA256 可明确使用
`lhash -a sha256 <file>`。

## 交付问题 D01–D07

| 编号 | 修复及回归 |
| --- | --- |
| D01：清单覆盖、ARM64 遗漏与路径错误 | 主发布一次生成包含 x64、ARM64、manifest、SBOM 的 basename 清单；后处理不再覆盖；两架构均为 attestation subject。测试在实际下载目录执行校验，并验证篡改/缺项会失败。 |
| D02：GITHUB_TOKEN 事件链缺口 | SBOM 和两类证明直接在主发布 job 中生成，成功后才发布；独立 workflow 改为手工验证和重新出具证明，不依赖 `release.published` 自动触发。 |
| D03：macOS 包校验路径 | 包内 SHA256.txt 记录 `lhash`；workflow 将 tarball 解到新目录并实际校验。 |
| D04：版本为 unknown | 发布 checkout 对应提交，核对两个 ZIP 内完整 commit 和实际程序版本；缺失/不一致即失败。文件名接受正确 commit 的 7–40 位前缀。 |
| D05：tag 插入 shell 源码 | 手工 tag 经环境变量和带引号参数传入；无害命令替换形式的 tag 回归验证其保持字面值。 |
| D06：pristine 仅检查标记 | 增加独立于 vendor 目录的 5779 文件 SHA256 清单，拒绝修改、增加、删除和链接；清单来源为已核验官方归档。 |
| D07：benchmark 日志两个写者 | 分离 console 与脚本内部 combined 日志；ARM64 使用匹配的 MSBuild 架构/工具集。冷缓存 benchmark 原生运行仍待执行。 |

所有 workflow 顶层 action 已固定到经官方仓库解析的提交 SHA，发布写权限缩到
需要的 job。CodeQL 增加真实 Windows C++ 应用构建分析，并保留 C# 分析。
这些是已接入的控制；当前修复提交的 CodeQL 与发布服务结果须查看实际运行记录。

## macOS 和 OpenSSL

OpenSSL 保持 **3.5.9**，本轮没有修改其上游源文件。macOS CLI 现在必须链接
指定 vendor root 的静态 OpenSSL，提供与 Windows 一致的 18 个算法注册项，支持
`--list-algorithms`、`--all`、`-a/--algorithm`、`--json` 及单/并行策略。
未启用 vendor 时 CMake 拒绝构建算法不完整的 CLI。

macOS 仅支持 Apple Silicon/arm64，未恢复 Intel。通用 arm64 构建不等于已经在
M5/M6 或每一款 M 系列机器上实测。CLI tarball 仍是未签名、未公证的 workflow
artifact，不能混同于 Windows tagged release。

## 本地验证与待执行验证

本轮最后一次 Linux 集成 **ASan/UBSan CTest 17/17 通过**；关闭 vendor 和 CLI
的基线配置 **CTest 16/16 通过**。它们使用显式 POSIX
诊断后端验证共享逻辑；不能替代 Darwin/APFS 和 Win32/MFC 的实际行为。
交付回归另有 **13 项 release 测试和 2 项完整性测试通过**；完整性测试包含正确树、
修改、增加和删除检查，以及 `core.autocrlf=true` 检出后的 5779 文件逐字节核验。
vendor 专用 Git attributes 保留官方原始字节，包括原本采用 CRLF 的测试文件。
Actionlint 1.7.12、Python 语法和定向差异检查通过。

真实 MFC GUI 脚本已完成 PowerShell 解析及嵌入 C# 编译检查，并接入 Windows x64
构建后、签名/打包前。它只操作自己启动的进程，检查 close、ESC/cancel 和快速
start/close 和空闲关闭；活动场景须观察到真实读取进度后再关闭/取消。
超时或没有交互桌面都失败，不跳过后标绿，保留逐例 JSON、故障进程事件和日志。
**本地 Linux 没有执行这些真实 Windows GUI 场景。**

首次真实 MFC 回归发现哈希中关闭会以 `0xC0000005` 退出。后续退出顺序探针
确认：仅创建私有 `OSSL_LIB_CTX` 并不足以提前注册 OpenSSL 全局退出处理，首次
fetch 的较晚注册会让全局清理先于应用缓存析构。修复显式初始化和线程资源清理，
并保留独立进程、实际窗口及空闲/读取中关闭对照；原生结果须核对相应修复提交。

当前修复提交仍需取得以下运行证据：

- Windows x64/ARM64 原生 engine、provider、queue 与完整 CMake 回归；x64 真实 GUI。
- macOS ARM64 的完整 CLI、文件策略和 sanitizer 回归。
- 当前提交的 C++/C# CodeQL 分析。
- 手工 benchmark 冷缓存流程，以及实际 tag 发布时的 SBOM/attestation 服务。

之前提交的绿色 CI 不能替代本次修改的验证结果。真实 Copy/剪贴板/Explorer/UIPI
以及更多文件系统和具体设备测试仍有独立覆盖边界。

构建和测试命令见 [TESTING.md](TESTING.md)；交付流程见
[SUPPLY_CHAIN_SECURITY.md](SUPPLY_CHAIN_SECURITY.md)；下载后的校验步骤见
[RELEASE_VERIFICATION.md](RELEASE_VERIFICATION.md)。
