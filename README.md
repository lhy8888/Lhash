# LHash

![LHash Logo](trunk/source/WinUI/Assets/StoreLogo.scale-400.png)

![Windows Build](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml/badge.svg)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)

LHash is a maintained Windows-focused hash utility forked from [fHash](https://github.com/sunjw/fhash).
The current maintained release line starts at `v1.10.0`.

## 中文说明

LHash 是基于 [fHash](https://github.com/sunjw/fhash) 持续维护的 Windows 文件 Hash 工具分支。
当前这一版不再把重点放在“历史堆叠兼容”，而是围绕三个方向持续收口：

- 安全功能
- 架构优化
- 可验证、可发布、可持续维护

## 当前版本

- 当前正式版本：[`v1.10.0`](https://github.com/lhy8888/Lhash/releases/tag/v1.10.0)
- 当前正式下载入口：[`GitHub Releases`](https://github.com/lhy8888/Lhash/releases)
- 当前主分支：`future-winui-was2`
- 当前 CI：[`Windows Build workflow`](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml)

## 安全功能

LHash 当前维护线重点做的是“本地工具安全化”，主要包括：

- 加固命令行输入处理，减少异常参数导致的崩溃或错误行为
- 加固 `WM_COPYDATA` 输入校验，避免畸形消息污染桌面进程路径
- 加固拖拽、shell 拉起和右键菜单链路
- 修正 Windows 文件读取失败路径，避免错误地把异常当成 EOF 并产出错误 Hash
- 移除维护版 Windows 发行线里自动把 Hash 发送到第三方站点的入口
- 收紧打包、构建、发布流程，让发布物可复现、可回溯

当前维护版默认定位是：

- 本地运行
- 不要求账号
- 不做遥测
- 不默认上传文件内容
- 不默认上传 Hash 到第三方服务

## 架构优化

这一轮架构整改，核心目标不是“把代码拆得更花”，而是把产品做成更容易继续维护和扩展的形态。

从使用者视角看，当前这套架构主要解决的是下面几件事：

- 一套核心计算能力，可以稳定服务不同界面和不同发布形态，而不是每条产品线各维护一套
- 界面显示和底层计算尽量分开，减少“改一个按钮结果把核心逻辑带坏”的连锁问题
- 老界面可以继续用，但新能力也能更容易往里加，不必每次都大面积返工
- 发布、测试、打包尽量走统一链路，降低“本地能用、发布后出问题”的概率

可以把现在的产品结构简单理解成：

- 输入任务
- 核心计算
- 结果生成
- 界面展示

这比过去“界面、计算、结果结构全部缠在一起”的方式更清楚，也更容易继续演进。

## 这对使用者有什么意义

- 更稳定：不同界面共用同一套核心后，功能漂移和隐藏回归会更少
- 更容易扩展：以后增加新算法、新界面、新发布方式，改动范围会更可控
- 更容易持续维护：旧功能保留的同时，不会越来越难改
- 更容易验证：现在很多变更都能通过统一测试和发布流程提前暴露问题

## 这对未来扩展有什么帮助

当前这套结构更适合后续继续做这些事情：

- 增加新的 Hash 算法，而不是长期固定只围着当前几种算法打补丁
- 继续补强 WinUI、命令行、批处理等不同入口
- 继续收紧发布流程、签名流程和交付质量
- 在不推翻现有产品的前提下，逐步替换历史负担较重的部分

## 测试与验证

当前仓库不是“能编过就算完成”，而是带验证门槛的。

每个正式发布候选应通过：

- .NET 单元测试
- 原生 C++ 运行时测试
- 架构基线检查
- 安全回归检查
- Windows 原生构建矩阵
- 发布链演练

目前 GitHub Actions 已覆盖：

- legacy 桌面程序打包
- WinUI / CLR bridge 编译
- UWP / WinRT bridge 编译
- shell extension 编译
- publish-release 演练和正式 tag 发布

## 发布说明

- 分支构建会生成发布演练包
- `v*` 标签会触发正式 GitHub Release
- 当前正式发布目标是 Windows legacy 桌面程序
- 可选代码签名接入已经在 CI 里留好位置，但真正消除“发布者未知”仍需要真实签名证书

## 仓库文档

- 英文更新日志：[CHANGELOG.md](CHANGELOG.md)
- 中文更新日志：[CHANGELOG.zh-CN.md](CHANGELOG.zh-CN.md)
- 隐私说明：[PRIVACY.md](PRIVACY.md)
- 开源协议：[LICENSE](LICENSE)

## Upstream Credit

LHash is built on top of the original [fHash](https://github.com/sunjw/fhash) project by Sun Junwen.
This fork keeps upstream credit and GPL-2.0 licensing intact while maintaining its own tested Windows release line.
