# 更新日志

本文档只保留当前维护中的 LHash 发布说明；更早版本条目已移除，历史上游 fHash 的长篇日志不在此重复维护。

## 1.12.3 - 2026-04-29

这是一次面向 Windows 发布打包、Windows ARM64 包线，以及 macOS CLI MVP 的补丁发布；macOS CLI MVP 现在与 macOS core 支持契约并行存在。

### 发布与打包

- 将 Windows x64 发布产物命名规范化为 `LHash-windows-x64`
- 新增 Windows ARM64 MFC 打包与 tag release 发布
- 新增 macOS arm64 CLI MVP workflow 以及对应的 `tar.gz` workflow artifact

### Core 与文档对齐

- 继续让 core build matrix 与 smoke 覆盖保持在共享 core entry point 上
- 明确 macOS core support 与 macOS CLI MVP workflow 是分开跟踪的
- 让 release verification 与 supply-chain 文档与当前 artifact 布局保持一致

### 运行时与安全基线

- 保持之前已经加固过的 digest-update 和文件打开契约
- 继续保持 Windows / Darwin 安全回归在当前主线形态下为绿灯
