# LHash

![LHash Logo](trunk/source/WinUI/Assets/StoreLogo.scale-400.png)

![Windows Build](https://github.com/lhy8888/fhash/actions/workflows/windows-build.yml/badge.svg)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)

LHash is a maintained Windows-focused hash utility forked from [fHash](https://github.com/sunjw/fhash).
The current release line starts at `1.10.0` and focuses on secure desktop delivery, testable core architecture, and reproducible GitHub-based releases.

- English changelog: [CHANGELOG.md](CHANGELOG.md)
- 涓枃鏇存柊鏃ュ織: [CHANGELOG.zh-CN.md](CHANGELOG.zh-CN.md)
- Privacy: [PRIVACY.md](PRIVACY.md)
- License: [LICENSE](LICENSE)
- Releases: [GitHub Releases](https://github.com/lhy8888/fhash/releases)
- CI: [Windows Build workflow](https://github.com/lhy8888/fhash/actions/workflows/windows-build.yml)

## What LHash Provides

- MD5, SHA1, SHA256, and SHA512
- Drag and drop hashing
- Explorer context menu integration
- English and Simplified Chinese UI
- Packaged legacy desktop build from CI
- Native runtime tests, .NET unit tests, security regression tests, and release-chain rehearsal in GitHub Actions

## Current Product Shape

The repository contains multiple Windows-era stacks, but the current maintained release target is the packaged legacy desktop executable.
WinUI, CLR bridge, UWP bridge, and shell-extension projects are still built in CI so the shared native core stays healthy.

## Security Position

LHash is designed as a local hash tool first.
The Windows release line in this repository:

- does not require an account
- does not include telemetry
- does not automatically upload file content or hashes to third-party services
- hardens command-line, `WM_COPYDATA`, and shell-entry validation
- keeps release packaging reproducible in GitHub Actions

## Validation In This Repository

Every maintained release candidate is expected to pass:

- .NET unit tests
- native C++ runtime tests
- refactor baseline checks
- security regression checks
- the Windows build matrix
- the release-chain rehearsal job

## Build And Release Notes

- The current maintained release line starts at `v1.10.0`.
- Branch builds produce rehearsal release bundles.
- Version tags named `v*` trigger the formal GitHub release publishing step.
- Authenticode signing is supported by CI, but requires a real code-signing certificate configured in repository secrets.

## Upstream Credit

LHash is built on top of the original [fHash](https://github.com/sunjw/fhash) project by Sun Junwen.
This fork keeps upstream credit and GPL-2.0 licensing intact while maintaining a separate release line and delivery process.

---

## 涓枃璇存槑

LHash 鏄熀浜?[fHash](https://github.com/sunjw/fhash) 鎸佺画缁存姢鐨?Windows 鏂囦欢 Hash 宸ュ叿鍒嗘敮銆?褰撳墠缁存姢鐨勫彂琛岀嚎浠?`1.10.0` 閲嶆柊寮€濮嬶紝閲嶇偣鏀惧湪锛?
- Windows 鏈湴瀹夊叏鍔犲浐
- 鍙祴璇曠殑鏍稿績鏋舵瀯
- GitHub Actions 鍙鐜版瀯寤轰笌鍙戝竷

### 褰撳墠鑳藉姏

- 鏀寔 `MD5`銆乣SHA1`銆乣SHA256`銆乣SHA512`
- 鏀寔鎷栨嫿鏂囦欢璁＄畻
- 鏀寔璧勬簮绠＄悊鍣ㄥ彸閿彍鍗?- 鏀寔鑻辨枃鍜岀畝浣撲腑鏂囩晫闈?- CI 鎻愪緵鍙笅杞界殑妗岄潰鏋勫缓浜х墿

### 褰撳墠鍙戝竷绛栫暐

- 鍒嗘敮鏋勫缓浼氱敓鎴愬彂甯冩紨缁冨寘
- `v*` 鐗堟湰鏍囩浼氳Е鍙戞寮?GitHub Release
- 鐩墠姝ｅ紡鍙戝竷鐩爣鏄?legacy 妗岄潰绋嬪簭
- 鍏朵粬 WinUI / bridge / shell extension 宸ョ▼缁х画鍦?CI 涓繚娲伙紝鐢ㄤ簬淇濊瘉鍏变韩鍘熺敓鏍稿績涓嶆紓绉?
### 闅愮涓庡畨鍏?
LHash 鏄湰鍦板伐鍏凤紝涓嶈姹傝处鍙凤紝涔熶笉榛樿涓婁紶鏂囦欢鍐呭鎴栧搱甯屽€笺€?褰撳墠浠撳簱涓殑 Windows 鐗堟湰宸茬粡鍘绘帀鑷姩鎶婂搱甯屾彁浜ゅ埌绗笁鏂圭珯鐐圭殑鍏ュ彛锛屽苟鎸佺画瀵瑰懡浠よ銆乣WM_COPYDATA`銆乻hell 鍚姩閾惧仛杈撳叆鏍￠獙鍔犲浐銆?
### 浠撳簱楠岃瘉鍩虹嚎

褰撳墠浠撳簱涓殑姝ｅ紡鍙戝竷鍊欓€夊簲閫氳繃锛?
- .NET 鍗曞厓娴嬭瘯
- 鍘熺敓 C++ 杩愯鏃舵祴璇?- 鏋舵瀯鍩虹嚎妫€鏌?- 瀹夊叏鍥炲綊妫€鏌?- Windows 鏋勫缓鐭╅樀
- 鍙戝竷閾炬紨缁?
濡傞渶鏌ョ湅鐗堟湰鍙樺寲锛岃鍒嗗埆鍙傝€冿細

- 鑻辨枃鏇存柊鏃ュ織锛歔CHANGELOG.md](CHANGELOG.md)
- 涓枃鏇存柊鏃ュ織锛歔CHANGELOG.zh-CN.md](CHANGELOG.zh-CN.md)
