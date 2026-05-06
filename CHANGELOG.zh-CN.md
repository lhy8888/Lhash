# 更新日志

本文档只保留当前维护中的 LHash 发布说明。更早版本条目已经移除，历史上的上游发布日志不在此重复维护。

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
