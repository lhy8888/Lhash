<div align="center">

# LHash

### 拖入文件，让校验一目了然。

**轻巧的本地文件校验工具 · Windows 与 Mac 原生界面 · 18 种算法**

[**下载最新版本**](https://github.com/lhy8888/Lhash/releases/latest) · [查看本次更新](https://github.com/lhy8888/Lhash/releases/tag/v1.13.0) · [使用文档](docs/README.md)

![Version](https://img.shields.io/badge/version-1.13.0-2563EB)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS-0078D6)
![Algorithms](https://img.shields.io/badge/algorithms-18-16A34A)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)

</div>

下载的软件是否完整？备份和原文件是否一致？交付文件时，如何附上一份清楚的校验记录？

把文件交给 **LHash**。打开或拖入文件，即可计算校验值、核对已有摘要，一键复制或导出结果。所有计算都在本机完成，无需账号，无需上传文件，离线也能使用。

## v1.13.0：Mac，也有熟悉的 LHash 桌面体验

全新的 **macOS 图形界面**正式加入 LHash。沿用 Windows 版熟悉的操作布局，以 Mac 原生窗口、快捷键和拖放操作，完成从文件选择到结果导出的整个流程。

- **打开就会用**：打开文件、打开文件夹、验证、全部复制、导出、设置，常用操作集中呈现。
- **进度看得见**：文件、算法、状态与进度逐项展示，处理过程清晰可见。
- **专为 M 系列 Mac 提供原生版本**：支持 macOS 14 及以上，提供随系统语言显示的中文与英文界面。
- **跨平台使用同一套算法**：Mac 图形版、Mac 命令行版与 Windows 版均提供 18 种算法，换电脑也能延续熟悉的校验习惯。

## 为日常文件校验而做

| 特色 | 带来的便利 |
| --- | --- |
| **本地处理，离线可用** | 软件包、工作文件和个人资料都留在自己的电脑上。 |
| **拖放即用，支持批量** | 选择多个文件或整个文件夹，集中完成校验，省去反复操作。 |
| **一轮计算，多种结果** | 默认生成 SHA-256、SHA-512 与 SHA3-256，也可自由组合需要的算法。 |
| **粘贴摘要，快速核对** | 使用“验证”查找与预期校验值完全匹配的结果，减少手工逐字对照。 |
| **复制、导出，方便分享** | 把结果带到聊天、文档或交付清单中，也可保存为文本留档。 |
| **任务进度，清楚掌握** | 查看各文件的处理状态、总体进度、耗时与速度，按需停止当前任务。 |

从一个安装包，到一批照片、压缩包或系统镜像，LHash 把文件校验变成顺手的一步。

## 下载适合你的版本

**[前往 v1.13.0 下载页面 →](https://github.com/lhy8888/Lhash/releases/tag/v1.13.0)**

| 你的设备 | 选择的下载包 | 使用方式 |
| --- | --- | --- |
| Windows，Intel / AMD 64 位电脑 | `LHash-windows-x64-*.zip` | 解压后打开 `LHash.exe` |
| Windows，ARM64 电脑 | `LHash-windows-arm64-*.zip` | 解压后打开原生 ARM64 版 `LHash.exe` |
| Mac，Apple Silicon M 系列 | **`LHash-macos-arm64-app-*.zip`** | 解压后将 `LHash.app` 拖入“应用程序” |
| Mac，需要脚本或终端操作 | `LHash-macos-arm64-cli-*.tar.gz` | 解压后在终端使用 `lhash` |

Mac 版需要 **macOS 14 或更新版本，以及 Apple Silicon M 系列芯片**。首次打开应用如需系统授权，请参阅 [Mac 安装与使用指南](docs/MACOS_GUI.md#get-the-app)。

下载页同时提供文件校验值；需要核对下载包时，可查看[下载校验指南](docs/RELEASE_VERIFICATION.md)。

## 三步完成校验

1. **加入文件**：打开 LHash，拖入文件，或使用“打开”“打开文件夹”。
2. **获取结果**：默认即可计算三种常用摘要；需要其他算法时，在“设置”中选择。
3. **核对或保存**：点击“验证”粘贴预期摘要，或使用“全部复制”“导出”保存计算结果。

核对下载文件时，选择与发布方给出的校验值对应的算法。例如，对方提供 SHA-256，就使用 SHA-256 结果进行比较。

## 18 种算法，按需选择

日常下载校验、历史文件比对、批量数据处理，一套工具覆盖多种需求。Windows 与 Mac 提供一致的算法选择。

| 算法系列 | 可用算法 |
| --- | --- |
| SHA-2 | SHA-256、SHA-384、SHA-512 |
| SHA-3 | SHA3-256、SHA3-384、SHA3-512 |
| BLAKE2 | BLAKE2b-512、BLAKE2s-256 |
| BLAKE3 | BLAKE3-256、BLAKE3-512、BLAKE3 XOF（128 字节） |
| SHAKE | SHAKE128-256、SHAKE256-512 |
| 兼容性摘要 | MD5、SHA-1 |
| 快速校验 | XXH3-64、XXH3-128、CRC32C |

日常完整性校验推荐使用 **SHA-256**。MD5、SHA-1 适合比对已有的历史摘要；XXH3、CRC32C 用于非密码学用途的快速校验。

<details>
<summary><strong>喜欢终端？Mac 命令行版也为你准备好了。</strong></summary>

在解压目录中运行，计算文件的 SHA-256：

```sh
./lhash -a sha256 "/path/to/file.iso"
```

还可批量处理多个文件、选择全部算法，或输出 JSON，方便接入自己的脚本与工作流程。更多示例见 [Mac 命令行指南](docs/MACOS_CORE_SUPPORT.md)。

</details>

## 开源，与大家一起打磨

LHash 的源代码与更新记录公开可查。欢迎通过 [Issues](https://github.com/lhy8888/Lhash/issues) 分享使用体验、报告问题或提出建议；喜欢这个小工具，也欢迎点一个 **Star**。

[更新日志](CHANGELOG.zh-CN.md) · [Mac 使用指南](docs/MACOS_GUI.md) · [完整文档](docs/README.md) · [构建指南](docs/CORE_BUILD.md) · [测试说明](docs/TESTING.md) · [安全政策](SECURITY.md)

开源许可：**GPL-2.0-only with an OpenSSL linking exception**。详见 [LICENSE](LICENSE) 与 [OpenSSL 链接例外](LICENSE-OPENSSL-EXCEPTION.md)。

---

**让文件校验成为顺手的一步。 [下载 LHash →](https://github.com/lhy8888/Lhash/releases/latest)**
