# 更新日志

这里记录的是 LHash 自己维护的发行线变化。

上游 fHash 的长期历史版本记录不再在这里重复维护。
当前这份日志从 `1.10.0` 开始，专门对应 LHash 的维护和发布。

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
