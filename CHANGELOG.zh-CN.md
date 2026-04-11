# 更新日志

这里记录的是 LHash 自己维护的发行线变化。

上游 fHash 的长期历史版本记录不再在这里重复维护。
当前这份日志从 `1.10.0` 开始，专门对应 LHash 的维护和发布。

## 1.12.1 - 2026-04-11

这是一次以安全、兼容性和运行时正确性为主的补丁版本，用来收口 OpenSSL 3 EVP 接入后的边缘问题。

### 安全与兼容

- 收紧 legacy shell 启动辅助逻辑，子进程默认不再继承父进程句柄
- 强化旧系统上的 DLL 安全加载回退路径，避免兼容模式重新退回最弱的裸 `LoadLibrary` 语义
- 修正 ARM64 在 legacy Windows 架构检测里的误判问题，避免 shell extension 查找走错分支
- 修复右键菜单移除逻辑中错误码聚合写法，避免把实际失败误判成成功

### 运行时正确性

- 完成算法描述符注册表的 snapshot 收口，避免跨线程枚举时继续暴露共享 live view
- 修复 native runtime tests 暴露出的注册表初始化边缘问题
- 清理 legacy `sha256.cpp` 中重复且失效的历史宏残留

### 交付与 CI

- 继续复用 OpenSSL vendor 缓存，保持日常原生构建链路快速完成
- 保留 WinUI 预览能力，但不把预览 bridge 重新放回普通 `push` 的日常构建链

## 1.12.0 - 2026-04-10

这一轮新增了固定版本的 OpenSSL 3 EVP 算法族，并配套补强了运行时安全收口与仓库瘦身。

### 算法能力

- 以固定版本方式接入 `OpenSSL 3 EVP`
- 新增独立的 `SHA-256`、`SHA-384` 和 `SHA-512` 描述符，不改动原有 `SHA256` / `SHA512` 的 id 与显示名称
- 新增 `SHA3-256`、`SHA3-384`、`SHA3-512`、`BLAKE2b-512`、`BLAKE2s-256`、`SHAKE128-256`、`SHAKE256-512`
- 新增算法通过独立 provider 与 `third_party/openssl` vendor 目录接入，不污染原有内置 SHA-2 实现

### 构建与授权

- 维护版原生构建新增 OpenSSL vendor 构建步骤
- 增加 `GPL-2.0-only` 下的 OpenSSL linking exception 说明
- 保留原有内置 SHA-2 实现不动，方便后续逐步停用其中一条算法族

### 安全与维护

- 修复 Windows 可执行文件版本信息读取在异常版本资源下的崩溃/泄漏风险
- 收紧 `WM_COPYDATA` 发送方校验和载荷解析
- 清理旧 `sha256.cpp` 里重复且失效的历史宏残留
- 将旧 UWP、WAP、macOS 与废弃 shell/bridge 平台树移出 live tree，统一归档

### CI 与交付

- 将日常 `push` 构建收口到当前维护中的原生桌面主线
- 对 OpenSSL vendor 构建产物增加缓存，显著缩短普通 Action 耗时

## 1.11.0 - 2026-04-09

这是维护版第一次把 BLAKE3 正式接入发行线，并同步补强运行时回归与验证门禁。

### 算法能力

- 以固定版本方式接入官方 BLAKE3 C 实现
- 新增 `BLAKE3-256`、`BLAKE3-512`、`BLAKE3 XOF` 三个算法描述符
- 以固定版本方式接入官方 `XXH3-64`、`XXH3-128` 和 `CRC32C`
- 基于 benchmark 结果，当前保留的 BLAKE3 SIMD 路径为：
  - `x64`：`SSE2`、`SSE4.1`、`AVX2`、`AVX512`
  - `Win32`：`SSE2`、`SSE4.1`、`AVX2`
  - `ARM64`：`NEON`

### 运行时与架构

- 将 `HashExecutionContext` 中的进度接收口收成 non-owning observer seam，并提供空对象回退
- 原生运行时测试新增 BLAKE3 的大小写行为、未知算法 id、结果顺序与并发稳定性覆盖
- 原生运行时测试新增官方 `XXH3` / `CRC32C` 向量、未知算法 id、结果顺序与并发稳定性覆盖
- 继续保持正式原生发行线轻量、可携带的交付方式

### 验证体系

- 扩展 unit-tests、refactor baseline、security regression 对 BLAKE3 和运行时契约的约束
- 扩展 unit-tests、refactor baseline、security regression 对固定版本 `XXH3` / `CRC32C` provider 的约束
- 新增 `x64 / Win32 / ARM64` 三平台原生 benchmark 流水线
- 当前 benchmark 结果显示，`BLAKE3-256` 在大文件场景大致提升为：
  - `x64`：`481 MiB/s -> 1641 MiB/s`
  - `Win32`：`390 MiB/s -> 1369 MiB/s`
  - `ARM64`：`532 MiB/s -> 901 MiB/s`

## 1.10.1 - 2026-04-09

这是 `1.10.0` 之后的首个维护更新版本。

### 桌面体验

- 收紧正式原生桌面版的按钮区和任务区布局
- 保持结果区稳定，同时让任务栏保留历史文件状态
- 修复导出为空的问题，当前可见 Hash 结果会正确导出为 UTF-8 文本
- 将算法相关入口统一收进设置，并把菜单文案改为“算法选择”

### 版本与发布元数据

- 维护版产品版本号提升到 `1.10.1`
- 将 legacy、WinUI 预览线、UWP 预览线的版本资源统一到 `1.10.1.0`

## 1.10.0 - 2026-03-30

LHash 维护版发行线的首个正式版本。

### 发布与交付

- 维护版产品版本号重置为 `1.10.0`
- 为 `v*` 标签打通正式 GitHub Release
- 为分支构建补齐发布演练包和发布清单
- 在 CI 中保留可选代码签名接入能力

### 安全加固

- 加固 Windows 桌面路径中的命令行和 `WM_COPYDATA` 输入处理
- 加固 shell 集成与发布打包流程
- 在当前维护版 Windows 发行线中移除第三方哈希提交入口

### 架构整改

- 引入 `HashRequest`、`HashResult`、`ProgressEvent`、`HashProgressSink`、`HashExecutionContext`
- 将核心执行主线推进到 `HashResult` 契约
- 让 WinUI native 构建统一经过 `fHashNativeCore`
- 将 legacy `ResultData` 逐步降级为兼容层

### 验证体系

- 新增独立 xUnit 单元测试框架
- 新增原生 C++ 运行时测试
- Windows 原生构建矩阵现在受单元测试、安全回归、原生运行时测试共同 gate
- 在正式打标签前完成了 CI 发布链演练
