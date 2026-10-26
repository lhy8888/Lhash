# 更新日志

本文档记录当前维护中的 LHash 版本。下方历史条目描述各自版本的改动，不重复收录更早的上游发布日志。

## 1.12.6 - 2026-10-26

本版更新哈希库，将 macOS Apple Silicon CLI 纳入正式发布包，并修复 Windows 结果显示与任务状态问题。

### 哈希库与平台支持

- 将原样导入的 OpenSSL 源码更新至 3.5.9，记录官方源码包 SHA-256 和逐文件校验清单。
- Windows x64/ARM64、macOS arm64 默认启用 OpenSSL 上游汇编优化；保留显式 `portable` 模式，复用已构建库前核对目标架构与模式。
- macOS CLI 接入与 Windows 一致的 18 种算法，支持算法选择、JSON 输出以及串行/并行摘要计算。
- macOS 使用通用 arm64 目标支持 Apple Silicon M 系列，移除 Intel 支持；macOS 包仍未签名、未公证。

### Windows 结果显示与任务状态

- 修复增量显示将新文字插入旧选区，导致文字叠画、文件信息与摘要错序的问题；明确在控件末尾追加并保留用户选区。
- 按会话和任务身份处理更新，使重复文件路径保留各自的完成状态。
- 取消或工作线程异常退出后，统一终结未完成任务行，保留已完成结果，并与后续会话隔离。
- 修复工作线程通知与 Windows 对话框管理器消息冲突的问题。

### 响应性与错误处理

- 使用任务到行号的索引和增量状态计数；界面显示文本变化时不再持有生产线程追加结果所需的锁。
- 将目录枚举移至工作线程，保留扫描预算、取消和关闭处理；界面不等待可能阻塞的枚举调用。
- 使用单调时钟统计耗时，避免按定时器消息数量估算时间。
- CLI 返回成功前刷新并检查标准输出，覆盖算法列表与 JSON 输出；写入失败返回非零退出码。

### 发布与验证

- 在同一版本下发布 Windows x64、Windows ARM64 和 macOS arm64 CLI 包，统一提供校验清单、发布清单、CycloneDX SBOM 和构建证明。
- 应用版本元数据更新为 `1.12.6.0`，重写 README，并同步问题模板与发布元数据检查。
- Windows 两架构真实 GUI 测试各扩至 18 项，包含 128 个不同文件的记录顺序、摘要对应，以及追加结果期间的选区保留；新增真实 EDIT 控件测试，以旧 API 行为作为失败对照。
- 验证 Windows x64/ARM64、macOS arm64 的优化与 portable 构建，并覆盖 Linux/macOS sanitizer 和共享引擎/CLI 回归。

实现细节与测试边界见[审计修复记录](docs/AUDIT_FIXES.md)和[测试说明](docs/TESTING.md)。

## 1.12.5 - 2026-05-05

这是一版聚焦于架构清理、发布文档刷新，以及版本元数据对齐的补丁发布。

### 架构清理

- 将活跃的 MFC 桥接层重命名为更清晰的适配器命名
- 从 active 树中移除了已失效的 legacy compatibility 和 managed bridge 孤岛
- 将公共头文件拆分为更清晰的 domain、runtime 与 platform 边界文件

### 文档与发布元数据

- 将当前发布引用更新为 `v1.12.5`
- 刷新 README、changelog、issue 模板和发布元数据测试
- 将维护中的 Windows 版本元数据更新为 `1.12.5.0`

### 一致性清理

- 收紧桥接层与源文件清单的一致性检查
- 明确 OpenSSL provider 的可用性模型
- 保持维护分支与当前 native / CI 边界一致

## 1.12.4 - 2026-04-30

这是一版聚焦于原样导入 OpenSSL 3.5.6 vendor、更新发布文档，以及清理陈旧一致性检查的补丁发布。

### 发布与打包

- 将维护中的 Windows vendor 树升级为原样 OpenSSL 3.5.6
- 删除退役的 `third_party/openssl/3.0.20`
- 保持 Windows x64 / ARM64 发布面与当前 vendor 政策一致

### 文档与发布元数据

- 将当前发布引用更新为 `v1.12.4`
- 刷新 README、changelog、issue 模板和发布元数据测试
- 将维护中的 Windows 版本元数据更新为 `1.12.4.0`

### 一致性清理

- 让算法顺序断言与当前注册顺序保持一致
- 补全新注册的 native runtime 测试名称追踪
- 将退役的 Windows OsUtils 变体移出 active source tree
- 将 `HashDigestRuntimePlan` 改为按值持有 request 和 queue-plan 状态
