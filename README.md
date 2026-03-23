# LHash

![LHash Logo](trunk/source/WinUI/Assets/StoreLogo.scale-400.png)

LHash is a secondary-development Windows hash utility based on [fHash](https://github.com/sunjw/fhash).
This fork focuses on Windows desktop delivery, security hardening, robustness fixes, branding refresh, and reproducible cloud builds.

[中文说明](#中文说明)

## Highlights

- MD5, SHA1, SHA256, and SHA512
- Drag and drop file hashing
- Windows Explorer context menu integration
- English and Simplified Chinese UI
- Hardened Windows file handling and input validation
- Reduced attack surface and removed third-party hash lookup entry points
- GitHub Actions Windows packaging with downloadable artifacts

## What Is Different In LHash

- Rebranded the Windows app from `fHash` to `LHash`
- Replaced the logo, About dialog, product metadata, and package identity
- Updated project links to this repository: [https://github.com/lhy8888/fhash](https://github.com/lhy8888/fhash)
- Added cloud build automation for Windows legacy packaging
- Added a dedicated security regression test project

## Security And Reliability Improvements

- Fixed a Windows read-failure path that could previously be treated as EOF and risk publishing an incorrect partial hash
- Hardened legacy MFC command-line parsing and `WM_COPYDATA` validation to reduce malformed-input and local crash risk
- Hardened shell integrations by improving long-path handling, reducing requested process privilege, and closing leaked handles/resources
- Removed Windows UI entry points that sent hash values to third-party websites
- Reduced unnecessary Windows app attack surface by removing unneeded capabilities and unused dependencies where applicable

## Validation Performed

- Security regression suite for branding, input validation, error propagation, shell hardening, and package metadata
- Real-file hash smoke tests with empty, text, Unicode, and binary samples
- GitHub Actions Windows build workflow with packaged desktop artifacts

## Download

Current Windows builds are produced by GitHub Actions.

1. Open the [Actions page](https://github.com/lhy8888/fhash/actions)
2. Open the latest successful `Windows Build`
3. Download the artifact named `LHash-legacy-x64`
4. Extract and run `LHash.exe`

## Secondary Development Statement

This repository is a secondary-development fork built on top of the original `fHash` project.
The current work mainly targets the Windows code path and includes:

- product branding refresh
- security hardening
- defect fixing
- test expansion
- packaging and CI improvements

## License And Upstream Credit

- License: GPL-2.0
- Upstream project: [fHash](https://github.com/sunjw/fhash)
- Original upstream author: Sun Junwen

---

## 中文说明

LHash 是基于 [fHash](https://github.com/sunjw/fhash) 进行二次开发的 Windows 文件 Hash 工具。
当前版本重点围绕 Windows 桌面端做了品牌升级、安全加固、稳定性修复、测试补强和云端自动打包，更适合继续维护和分发。

### 产品定位

- 面向 Windows 的文件 Hash 校验工具
- 支持 `MD5`、`SHA1`、`SHA256`、`SHA512`
- 支持拖拽计算
- 支持资源管理器右键菜单
- 支持英文和简体中文界面

### 本次二次开发做了什么

#### 1. 品牌升级

- 软件名称从 `fHash` 升级为 `LHash`
- 替换了主图标、程序图标、About 窗口品牌信息
- 更新了产品元数据、程序名、输出文件名和安装包显示名
- 项目链接统一切换到当前仓库：[https://github.com/lhy8888/fhash](https://github.com/lhy8888/fhash)
- 版权所有信息更新为 `2026-LHY`

#### 2. 安全优化

- 修复 Windows 读文件失败被误当作 EOF 的问题，避免在异常场景下产生错误 Hash 结果
- 加固旧版 MFC 命令行解析和 `WM_COPYDATA` 输入校验，降低恶意或畸形输入导致异常的风险
- 加固 Shell Extension 的长路径处理、权限申请和句柄释放，减少本地攻击面
- 禁止把 Hash 值直接提交到第三方网站，避免不必要的信息外泄
- 收缩 Windows 应用攻击面，移除不必要能力和未使用依赖

#### 3. 缺陷修复与稳定性优化

- 修复云端构建中的资源文件结尾问题
- 修复 Windows CI 中中文源码字符集编译问题
- 修复打包脚本路径识别问题，确保输出的就是可运行的 `LHash.exe`
- 统一云端产物命名，避免旧包和新包混淆

#### 4. 工程化与可交付能力

- 新增 GitHub Actions Windows 构建流程
- 新增可下载的云端打包产物 `LHash-legacy-x64`
- 新增安全回归测试工程，覆盖品牌、输入校验、错误传播和 Shell 硬化检查

### 已完成的验证

- 安全回归测试已实际执行并通过
- 真实文件 Hash 样本测试已实际执行并通过
- GitHub Actions Windows 打包链路已打通，可直接下载构建产物

### 下载方式

当前 Windows 版本可从 GitHub Actions 下载：

1. 打开 [Actions](https://github.com/lhy8888/fhash/actions)
2. 进入最新成功的 `Windows Build`
3. 下载产物 `LHash-legacy-x64`
4. 解压后运行 `LHash.exe`

### 二次开发声明

本仓库是基于原项目 `fHash` 的二次开发版本。
当前二开重点主要集中在 Windows 路线，包括：

- 品牌重塑
- 安全加固
- 缺陷修复
- 测试补强
- 自动构建与打包

### 许可与致谢

- 开源协议：GPL-2.0
- 上游项目：[`fHash`](https://github.com/sunjw/fhash)
- 保留对原项目及原作者的致谢与署名
