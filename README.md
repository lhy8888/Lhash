# LHash

![LHash Logo](trunk/source/WinUI/Assets/StoreLogo.scale-400.png)

![Windows Build](https://github.com/lhy8888/fhash/actions/workflows/windows-build.yml/badge.svg?branch=future-winui-was2)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)

LHash is a secondary-development Windows hash utility based on [fHash](https://github.com/sunjw/fhash).
This fork focuses on Windows security hardening, robustness fixes, safer desktop delivery, and reproducible cloud builds.

[中文说明](#中文说明)

## Quick Links

- Repository: [https://github.com/lhy8888/fhash](https://github.com/lhy8888/fhash)
- Actions Builds: [Windows Build](https://github.com/lhy8888/fhash/actions/workflows/windows-build.yml)
- Releases: [https://github.com/lhy8888/fhash/releases](https://github.com/lhy8888/fhash/releases)
- Upstream Reference: [https://github.com/sunjw/fhash](https://github.com/sunjw/fhash)

## Highlights

- MD5, SHA1, SHA256, and SHA512
- Drag and drop file hashing
- Windows Explorer context menu integration
- English and Simplified Chinese UI
- Hardened Windows file handling and input validation
- Reduced attack surface and removed third-party hash lookup entry points
- GitHub Actions Windows packaging with downloadable artifacts

## Security Focus In LHash

- Safer Windows file error handling to prevent incorrect partial-hash results after read failures
- Hardened command-line and `WM_COPYDATA` input validation in the legacy MFC path
- Hardened shell integration around long paths, privilege scope, and handle/resource lifetime
- Removed hash submission entry points to third-party websites
- Kept Windows attack surface smaller by removing unnecessary capabilities and unused dependencies

## Security And Reliability Improvements

- Fixed a Windows read-failure path that could previously be treated as EOF and risk publishing an incorrect partial hash
- Hardened legacy MFC command-line parsing and `WM_COPYDATA` validation to reduce malformed-input and local crash risk
- Hardened shell integrations by improving long-path handling, reducing requested process privilege, and closing leaked handles/resources
- Removed Windows UI entry points that sent hash values to third-party websites
- Reduced unnecessary Windows app attack surface by removing unneeded capabilities and unused dependencies where applicable

## Validation Performed

- Security regression suite for input validation, error propagation, shell hardening, and package consistency
- Real-file hash smoke tests with empty, text, Unicode, and binary samples
- GitHub Actions Windows build workflow with packaged desktop artifacts


## Secondary Development Statement

This repository is a maintained Windows-focused fork built on top of the original `fHash` project.
The current work mainly targets the Windows code path and includes:

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

LHash 是基于 [fHash](https://github.com/sunjw/fhash) 持续维护的 Windows 文件 Hash 工具分支。
当前版本重点围绕 Windows 路线做了安全加固、稳定性修复、测试补强和云端交付优化，更适合继续维护和分发。

### 产品定位

- 面向 Windows 的文件 Hash 校验工具
- 支持 `MD5`、`SHA1`、`SHA256`、`SHA512`
- 支持拖拽计算
- 支持资源管理器右键菜单
- 支持英文和简体中文界面

### 当前版本重点

#### 1. 安全强化

- 修复 Windows 读文件失败被误当作 EOF 的问题，避免在异常场景下产生错误 Hash 结果
- 加固旧版 MFC 命令行解析和 `WM_COPYDATA` 输入校验，降低恶意或畸形输入导致异常的风险
- 加固 Shell Extension 的长路径处理、权限申请和句柄释放，减少本地攻击面
- 禁止把 Hash 值直接提交到第三方网站，避免不必要的信息外泄
- 收缩 Windows 应用攻击面，移除不必要能力和未使用依赖

#### 2. 缺陷修复与稳定性优化

- 修复云端构建中的资源文件结尾问题
- 修复 Windows CI 中中文源码字符集编译问题
- 修复打包脚本路径识别问题，确保输出的就是可运行的 `LHash.exe`
- 统一云端产物命名，避免旧包和新包混淆

### 已完成的验证

- 安全回归测试已实际执行并通过
- 真实文件 Hash 样本测试已实际执行并通过
- GitHub Actions Windows 打包链路已打通，可直接下载构建产物


### 维护方向说明

本仓库基于原项目 `fHash` 持续维护，当前重点主要集中在 Windows 路线，包括：

- 安全加固
- 缺陷修复
- 测试补强
- 自动构建与打包

### 许可与致谢

- 开源协议：GPL-2.0
- 上游项目：[`fHash`](https://github.com/sunjw/fhash)
- 保留对原项目及原作者的致谢与署名
