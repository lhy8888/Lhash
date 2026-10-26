# 测试范围与复现命令

以下描述当前配置及测试目标。是否通过必须查看同一提交的运行记录；源码检查、
API替身、原生执行和GUI交互是不同的证据。

## 平台矩阵

| 平台 | Runner | 覆盖 |
| --- | --- | --- |
| Windows x64 | windows-2022 / VS2022 | MFC产品构建、原生engine、OpenSSL、完整CMake回归、真实worker handle及16例真实窗口退出/任务状态/文本测试 |
| Windows ARM64 | windows-11-arm / VS2026 | 原生执行engine/provider/queue/CMake回归；MFC发布目标由x64 runner交叉编译，成品再在原生ARM64执行16例窗口退出/任务状态/文本测试 |
| macOS ARM64 | macos-14 | 真实OpenSSL完整engine、18算法CLI、Darwin文件策略和变化检测 |
| macOS ARM64 ASan/UBSan | macos-14 | 插桩重编OpenSSL、engine、CLI及回归测试 |
| Linux x64与ASan/UBSan | ubuntu-24.04 | 显式POSIX测试后端的完整engine/CLI、故障注入、可移植回归和源码检查 |

工作流：[Core Build Matrix](../.github/workflows/core-m2.yml)、
[Windows Build](../.github/workflows/windows-build.yml)、
[Portable Runtime Tests](../.github/workflows/portable-runtime-tests.yml)、
[macOS CLI](../.github/workflows/macos-cli-build.yml)、
[CodeQL](../.github/workflows/codeql.yml)。CodeQL包含C++产品构建与C#检查。

macOS仅Apple Silicon通用arm64，不绑定某一代M系列CPU。托管CI代表其运行机器，
不能声称已在每个M5/M6型号实机验证。Linux测试后端不替代Darwin原生文件行为。

正式构建使用 OpenSSL 上游汇编优化；Windows x64/ARM64 和 Linux/macOS 另有
隔离的 `portable` 回归。模式、目标和 SHA256/SHA512 汇编状态在构建及缓存恢复后验证。
优化库另在支持的平台屏蔽可选 CPU 特性执行 provider 回归，验证基线分派；这不等同于
在所有旧 CPU 上实测。工具链与命令见 [OPENSSL_OPTIMIZATION.md](OPENSSL_OPTIMIZATION.md)。

## 测试目标

- `lhash_core_runtime`：43个真实文件用例，固定摘要、缓冲边界、Unicode、缺失/目录、
  错误后继续、取消和多请求并发；AUTO/SINGLE/PARALLEL三种策略。
- Windows native suite：49个测试函数，使用真实Windows文件后端和OpenSSL，包含
  SHA256的17种长度×3策略、失败恢复、锁定、并发及完整摘要匹配。
- `lhash_cli_regression`：默认算法与Windows一致，18算法空文件/abc/4MiB+17独立向量，
  逐个选择、全部选择、单/并行、分块边界、Unicode/控制字符文件名、JSON记录、退出码和
  8192文件上限。参考数据来自Python hashlib、独立blake3/xxhash及逐bit CRC32C。
  macOS/Linux 用只读 stdout 描述符注入写入失败，覆盖帮助、列表、单/多文件文本及JSON；
  Linux 另用 `/dev/full` 注入空间不足，要求退出码1及明确错误，含退出前flush才失败的情形。
- `lhash_task_identity_tests`：真实引擎单/并行的重复路径、文件失败、保留历史和新请求，
  验证每个文件事件携带原输入索引；取消与异常不发布部分摘要，也不替未执行任务发完成事件。
- `lhash_file_integrity`：POSIX真实读取与前后metadata/累计字节校验、同尺寸覆盖、
  截短/增长、恢复mtime后的变化、metadata-only、路径类型、并发时间格式化。
  Linux额外使用链接器wrap注入短读/EINTR/EIO和受控检查/open竞争；macOS不冒充执行了Linux wrap。
- `tests/digest_queue`：实际队列与线程池的84个边界、100000次末块交错、1200并发任务、
  取消/失败/复用，新增短读与生产者/消费者异常收尾。
- `tests/engine_failure`：真实入口working状态清理、部分线程池构造失败回收、派发摘要
  任务时失败后等待已启动任务。测试依赖替身隔离这些分支，不等于完整平台集成。
- `tests/ui_lifecycle`：实际session所有权gate、请求快照、退出通知顺序、启动失败与
  完整hex摘要匹配；COM初始化失败、已有初始化引用和异常退出时的客户对象析构顺序；
  Windows另运行真实线程handle，验证活线程不能丢失/重启。
- `lhash_task_state_tests`：直接执行产品使用的队列和任务行模型，验证同路径不同任务、
  延迟消费、终态不回退、取消/异常/未执行、当前会话收尾、历史保留和过期/未知ID拒绝。
  8192条任务及后续会话核对增量计数，覆盖重复进度、清空和非法状态拒绝。
- `lhash_ui_timing_tests`：生产 steady_clock 模型的漏 tick、延迟UI、极短任务、
  异常冻结、reset和并发读取；worker入口与启动生命周期另有集成约束。
- `lhash_text_buffer_tests`：8192条差量恰好交付一次、Unicode、reset/过期批次、
  准备状态快照恢复、可见链接边界以及显示阻塞期间生产者仍可追加。
- `tests/windows-ui`：原10例退出测试加每轮三例：重复小文件完成后必须为2项完成；
  读取中Stop后窗口保留、没有运行中任务，已完成/取消/未执行计数覆盖全部输入。
  128文件场景检查全部SHA256输出恰好一次，再验证大小写重建和Clear。
  从自有进程的普通状态控件读取文字，系统封送，不读取远程ListView结构。
- `tests/windows-inputs`：从当前生产函数生成探针，验证250024个COPYDATA样本、数量
  边界、带空格exe引用、目录预算/取消、剪贴板所有权与失败、版本API返回边界。
  MFC/WinAPI为显式替身，不代表自动运行了原生窗口、Explorer或UIPI。
- `lhash_background_result_tests`：异步扫描结果的取消、晚完成、独立寿命和单次交付；
  Windows另运行 `lhash_folder_scan_native_tests`，实际枚举Unicode临时目录，验证空目录、
  错误、异常以及受控阻塞worker的取消和UI owner释放。受控阻塞不是故障存储实测。
- `lhash_string_safety`：SHA1最终字符串、UTF8/UTF16边界、随机Unicode、嵌入NUL、
  非法编码、ctype高位字节、replace/进制。Windows运行真实字符转换API。
- `tests/openssl-provider`：8个CTest分组，真实3.5.9适配器的向量、文件、生命周期、
  并发、注入失败、长度/模式/NULL参数、专用上下文与外部OpenSSL配置隔离。
- `tests/delivery`：双架构包/版本/提交身份、不可覆盖完整清单、实际解包校验、SBOM
  及vendor完整性。发布门禁包括原生测试和交付回归。
- .NET源码约束与SecurityRegression：检查源码/工程关系，不能当作运行时安全证明。

## macOS完整构建

```sh
bash tools/build_openssl_vendor_posix.sh "$PWD/build/openssl-install"
cmake -S . -B build/runtime -DCMAKE_BUILD_TYPE=RelWithDebInfo \
  -DCMAKE_OSX_ARCHITECTURES=arm64 \
  -DLHASH_ENABLE_OPENSSL_VENDOR=ON -DOPENSSL_ROOT_DIR="$PWD/build/openssl-install" \
  -DLHASH_BUILD_M2_SMOKE=ON -DLHASH_BUILD_RUNTIME_TESTS=ON -DLHASH_BUILD_CLI=ON
cmake --build build/runtime --parallel 3
ctest --test-dir build/runtime -V --output-on-failure --no-tests=error
```

Linux云端使用同一命令，删除`CMAKE_OSX_ARCHITECTURES`并增加
`-DLHASH_ENABLE_POSIX_TEST_BACKEND=ON`。这个选项默认关闭。
Windows参见[Core Build](CORE_BUILD.md)和[native说明](../native-runtime-tests/README.md)，
真实OpenSSL core须给出`OPENSSL_ROOT_DIR`，并使用与vendor一致的Release/静态CRT。

独立套件可以分别运行，例如：

```sh
cmake -S tests/openssl-provider -B build/provider -DCMAKE_BUILD_TYPE=RelWithDebInfo \
  -DOPENSSL_ROOT_DIR="$PWD/build/openssl-install"
cmake --build build/provider --parallel 3
ctest --test-dir build/provider -V --output-on-failure --no-tests=error
cmake -S tests/digest_queue -B build/queue -DCMAKE_BUILD_TYPE=RelWithDebInfo
cmake --build build/queue --parallel 3
ctest --test-dir build/queue -V --output-on-failure --no-tests=error
python3 -m unittest discover -s tests/delivery -v
dotnet test unit-tests/LHash.UnitTests/LHash.UnitTests.csproj --configuration Release
dotnet run --project security-tests/SecurityRegression/SecurityRegression.csproj --configuration Release
```

额外的大文件检查（需要读取两遍以上4GiB逻辑内容；POSIX使用稀疏临时文件）：

```sh
python3 tests/cli/test_large_file.py build/runtime/bin/lhash
```

## Sanitizer与结果解释

在新的build/install目录配置sanitizer，避免复用未插桩的库：

```sh
export CFLAGS='-O1 -g -fsanitize=address,undefined -fno-omit-frame-pointer'
export CXXFLAGS="$CFLAGS"
export LDFLAGS='-fsanitize=address,undefined'
export ASAN_OPTIONS='halt_on_error=1:detect_leaks=1'
export UBSAN_OPTIONS='halt_on_error=1:print_stacktrace=1'
```

然后重建OpenSSL及上面的CMake目标。TSan必须另建目录，用`-fsanitize=thread`，
不能与ASan同时启用。文件/引擎并发测试需要真正完成且无TSan报告；功能摘要正确不能
掩盖数据竞争。仅插桩应用而沿用非插桩libcrypto时，应如实注明范围。

CI采用`--no-tests=error`和测试超时，并保存失败时的日志/JUnit/TRX。仍需人工或专门
GUI自动化验证真实窗口close/ESC/Copy、Explorer注册、提升/非提升交互。未覆盖所有
Windows/macOS版本、所有特殊文件系统、每款M系列硬件或整个OpenSSL上游测试集合。
