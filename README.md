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

## 与原版的差异

LHash 当前维护线已经不是原版 fHash 的“小修补分支”。

从产品形态、工程组织、验证体系和发布方式来看，它已经和早期原版拉开了明显差距：

- 不再只是保留原有桌面功能，而是把安全、测试、发布都纳入主线
- 不再依赖单点人工维护，而是建立了持续验证和持续发布能力
- 不再是“界面、核心、桥接混在一起”的历史形态，而是持续朝分层和可扩展方向重构
- 不再把发布看成构建后的附带动作，而是把发布链本身当成质量体系的一部分

如果把原版看作一个传统桌面 Hash 工具，那么当前维护线更接近一套经过系统性重构后的 Windows 发行版本：

- 更强调安全边界
- 更强调架构可演进
- 更强调验证闭环
- 更强调可复现交付

## 安全架构

LHash 当前维护线的安全目标，不是简单在原版外面补几层防护，而是把整个产品重做成一条更可控的本地执行链。
从架构视角看，它强调的是：最小暴露面、明确边界、可验证交付、以及后续可持续收紧。

### 1. 本地优先与最小攻击面

LHash 的维护版定位首先是本地工具，而不是联网平台。
这意味着它默认遵循下面几条原则：

- 本地运行，不依赖账号体系
- 不做遥测，不引入不必要的远程交互
- 不默认上传文件内容
- 不默认把 Hash 值提交给第三方服务
- 尽量减少对额外能力、额外入口和额外依赖的需求

从使用者角度，这样的好处很直接：工具职责更单纯，暴露面更小，安全判断更容易建立。

### 2. 输入边界先收紧，再进入核心计算

LHash 现在更强调“先校验、再进入核心链路”，而不是让各种外部入口直接碰到底层计算。

当前已经重点收紧的边界包括：

- 命令行输入
- `WM_COPYDATA`
- 拖拽入口
- shell 拉起链路
- 右键菜单与 Explorer 集成路径

这背后的原则是：任何来自系统、外壳、消息、路径或用户输入的内容，都应该先经过边界检查，再进入核心计算与结果展示流程。

### 3. 计算正确性本身也是安全的一部分

LHash 不是只防崩溃或恶意输入，也在收紧“错误结果被当成正确结果发布”的风险。

例如当前维护线已经专门修正和加固了：

- Windows 文件读取失败路径
- 异常 I/O 被误当成 EOF 的情况
- 错误状态和结果发布之间的边界

对 Hash 工具来说，错误结果被悄悄显示成“正常结果”，本身就是一种高风险问题。
所以当前安全架构里，正确性保障和输入防护是放在同一层级考虑的。

### 4. 发布链也纳入安全边界

LHash 的安全边界不只停留在运行时，还延伸到构建、测试、打包和发布。

当前发布链已经做到：

- 用统一 CI 跑单元测试、原生运行时测试、架构基线和安全回归
- 用统一构建矩阵验证不同 Windows 工程线不漂移
- 用发布链演练提前验证正式 release 步骤
- 为代码签名保留接入位，支持后续进一步提升发行可信度

这意味着维护版追求的不是“代码写完就结束”，而是“交付出去的东西也要可验证、可追踪、可复现”。

### 5. 安全能力可以继续扩，而不是越改越乱

这轮架构整改还有一个直接收益：后续继续做安全加固时，不需要每次都在 UI、核心、桥接、发布脚本里同时大面积返工。

这让后续更容易继续推进：

- 更严格的输入校验
- 更稳的签名与发布可信链
- 更清晰的权限边界
- 更低风险的功能扩展

简单说，LHash 现在的安全思路已经不只是“补几个点”，而是把产品往“默认更克制、边界更清楚、交付更可信”的方向推。

## 架构优化

这一轮架构整改，不是对原版做零碎修补，而是一轮明确面向未来的系统性重构。
目标不是“把代码拆得更花”，而是把产品做成更容易继续维护、继续扩展、继续发布的形态。

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

换句话说，当前维护线已经不再是“原版代码上叠一些补丁”的状态，而是逐步重构成一套更适合长期维护的 Windows 产品结构。

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
