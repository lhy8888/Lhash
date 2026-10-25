# 测试范围与复现命令

以下是持续集成配置的覆盖范围，实际通过情况应以对应提交的 GitHub Actions
运行结果及日志为准。新增测试、编译成功、原生执行成功是不同的证据。

## 平台矩阵

| 平台 | Runner | 执行内容 |
| --- | --- | --- |
| Windows x64 | `windows-2022`，VS2022 | Core smoke、真实文件 engine、49 项 Windows native、OpenSSL provider、digest queue、MFC 应用构建 |
| Windows ARM64 | `windows-11-arm`，VS2026 | 原生执行 Core smoke、真实文件 engine、49 项 Windows native、OpenSSL provider、digest queue；MFC 发布目标另由 x64 runner 交叉编译 |
| macOS ARM64 | `macos-14` | Core smoke、真实文件 engine、Darwin 文件检查、CLI、OpenSSL provider、digest queue |
| macOS x64 | `macos-15-intel` | 与 macOS ARM64 相同的测试 |
| macOS ARM64 + ASan/UBSan | `macos-14` | 重新编译 OpenSSL、engine 和测试代码并执行 provider、queue、engine、Darwin、CLI 检查 |
| Linux x64 | `ubuntu-24.04` | 实际 OpenSSL provider、digest queue、67 项源码约束和 4 项源码回归；不构建 Windows/Darwin 文件后端 |

工作流分别为 [Core Build Matrix](../.github/workflows/core-m2.yml)、
[Windows Build](../.github/workflows/windows-build.yml) 和
[Portable Runtime Tests](../.github/workflows/portable-runtime-tests.yml)。
Windows 工作流还执行 .NET 仓库约束测试和源码回归检查，并构建 x64/ARM64 MFC 应用。
Native 测试可执行文件检查自身架构及 OpenSSL 版本，ARM64 作业会实际运行程序。

## 各套测试证明什么

- **Core engine：43 个用例。** 通过真实 Windows/Darwin 文件后端运行
  `RunHashRequest`，覆盖固定摘要值、空文件、块/缓冲区边界、Unicode 路径、
  缺失文件、目录、重复输入、错误后继续、取消、多请求并发，以及三种执行策略。
  当前 CMake core 使用 OpenSSL stub，覆盖 MD5/SHA1/CRC32C/BLAKE3；
  这套测试不提供 SHA256 engine 覆盖。
- **Windows native：49 项。** 使用 Windows/MFC 类型、实际文件后端和 OpenSSL，
  包含 SHA256 的 17 种文件长度 × 3 种执行策略、路径和文件句柄、取消后恢复、
  并发、算法选择及失败传播。见 [native 测试说明](../native-runtime-tests/README.md)。
- **OpenSSL provider：6 个 CTest 分组。** 验证头文件和运行库均为 3.5.9；
  30 个固定向量覆盖 10 种算法，另有 SHA256 分块写入、11 种真实文件长度、
  失败传播、生命周期和 8 线程共 800 次 SHA256 运算。直接编译产品的 EVP adapter，
  文件组使用 C++ 流读取，不能替代 OS 文件后端测试。
- **Digest queue：独立 CTest。** 编译实际队列和线程池代码，使用文件读取及摘要回调
  替身检查字节顺序、完整消费、队列限制、取消、读取失败、复用和并发。
  末尾短块执行 100,000 次压力循环；压力测试通过不代表证明不存在竞态。
- **macOS CLI：18 个调用用例。** 用 Python `hashlib` 独立对照 MD5/SHA1，
  检查 13 种文件长度、Unicode 文件名、退出码、缺失文件、目录、符号链接及错误后继续。
- **.NET 测试和源码回归检查。** 检查仓库源码、构建配置和发布约束，可以在 Linux
  云环境复现；这些结果不等于执行了 Windows UI 或平台 API。

## 本地复现

所有命令在仓库根目录运行。需要 CMake、C++17 编译器；OpenSSL 构建还需要
Perl 和平台构建工具。使用与目标相同架构的机器执行平台测试。

Windows core 命令见 [Core Build](CORE_BUILD.md)。带真实 OpenSSL 的 Windows
engine 测试见 [native 测试说明](../native-runtime-tests/README.md)，其中包含
VS2022/x64 和 VS2026/ARM64 的 MSBuild 命令。

macOS engine 和 CLI：

```sh
cmake -S . -B build/runtime -DCMAKE_BUILD_TYPE=RelWithDebInfo \
  -DLHASH_BUILD_M2_SMOKE=ON -DLHASH_BUILD_RUNTIME_TESTS=ON -DLHASH_BUILD_CLI=ON
cmake --build build/runtime --parallel 3
ctest --test-dir build/runtime -V --output-on-failure --no-tests=error
python3 tests/cli/test_cli.py build/runtime/bin/lhash
```

macOS/Linux 的独立 provider 和 queue：

```sh
bash tools/build_openssl_vendor_posix.sh "$PWD/build/openssl-install"
cmake -S tests/openssl-provider -B build/provider -DCMAKE_BUILD_TYPE=RelWithDebInfo \
  -DOPENSSL_ROOT_DIR="$PWD/build/openssl-install"
cmake --build build/provider --parallel 3
ctest --test-dir build/provider -V --output-on-failure --no-tests=error
cmake -S tests/digest_queue -B build/queue -DCMAKE_BUILD_TYPE=RelWithDebInfo
cmake --build build/queue --parallel 3
ctest --test-dir build/queue -V --output-on-failure --no-tests=error
```

Windows 的 provider 使用 `trunk/build_openssl_vendor.ps1` 输出的安装目录，
CMake 配置增加对应 VS generator 和 `-A x64` 或 `-A ARM64`，构建及 CTest 均加
`--config Release` / `-C Release`。完整选项见
[provider README](../tests/openssl-provider/README.md) 和
[queue README](../tests/digest_queue/README.md)。

在 macOS 上复现 sanitizer 作业时，先设置以下变量，然后在新的构建目录及
新的 OpenSSL 安装目录中重新执行上面的构建和测试，避免复用未插桩的静态库：

```sh
export CFLAGS='-O1 -g -fsanitize=address,undefined -fno-omit-frame-pointer'
export CXXFLAGS="$CFLAGS"
export LDFLAGS='-fsanitize=address,undefined'
export ASAN_OPTIONS='halt_on_error=1'
export UBSAN_OPTIONS='halt_on_error=1:print_stacktrace=1'
```

Linux 云环境中的源码检查：

```sh
dotnet test unit-tests/LHash.UnitTests/LHash.UnitTests.csproj --configuration Release
dotnet run --project security-tests/SecurityRegression/SecurityRegression.csproj --configuration Release
```

## 结果与限制

Linux 云环境可以执行 67 项 .NET 源码约束测试；这项本地结果不代表
Windows/macOS 运行时测试已经通过。平台运行结果以对应提交的原生 CI 为准。

检查 Actions 对应提交的每个矩阵作业，确认测试步骤实际执行且未被跳过。
Core、provider 和 queue 使用 `--no-tests=error`，避免空测试集被当作通过；
运行时测试设置超时，失败返回非零状态。日志及可用的 JUnit/TRX 记录作为
Actions artifacts 上传，失败时也保留。

当前测试不包含完整桌面 UI 自动化、资源管理器交互、所有 Windows/macOS 版本，
也不包含 OpenSSL 上游完整测试套件。Linux 只验证可移植组件和源码约束；
Windows 和 Darwin 的文件行为以对应原生 runner 的执行结果为依据。
