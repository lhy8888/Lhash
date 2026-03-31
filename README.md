# LHash

![LHash Logo](trunk/source/WinUI/Assets/StoreLogo.scale-400.png)

![Windows Build](https://github.com/lhy8888/fhash/actions/workflows/windows-build.yml/badge.svg)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)

LHash is a maintained Windows-focused hash utility forked from [fHash](https://github.com/sunjw/fhash).
The current maintained release line starts at `v1.10.0`.

## 涓枃璇存槑

LHash 鏄熀浜?[fHash](https://github.com/sunjw/fhash) 鎸佺画缁存姢鐨?Windows 鏂囦欢 Hash 宸ュ叿鍒嗘敮銆?褰撳墠杩欎竴鐗堜笉鍐嶆妸閲嶇偣鏀惧湪鈥滃巻鍙插爢鍙犲吋瀹光€濓紝鑰屾槸鍥寸粫涓変釜鏂瑰悜鎸佺画鏀跺彛锛?
- 瀹夊叏鍔熻兘
- 鏋舵瀯浼樺寲
- 鍙獙璇併€佸彲鍙戝竷銆佸彲鎸佺画缁存姢

## 褰撳墠鐗堟湰

- 褰撳墠姝ｅ紡鐗堟湰锛歔`v1.10.0`](https://github.com/lhy8888/fhash/releases/tag/v1.10.0)
- 褰撳墠姝ｅ紡涓嬭浇鍏ュ彛锛歔`GitHub Releases`](https://github.com/lhy8888/fhash/releases)
- 褰撳墠涓诲垎鏀細`future-winui-was2`
- 褰撳墠 CI锛歔`Windows Build workflow`](https://github.com/lhy8888/fhash/actions/workflows/windows-build.yml)

## 瀹夊叏鍔熻兘

LHash 褰撳墠缁存姢绾块噸鐐瑰仛鐨勬槸鈥滄湰鍦板伐鍏峰畨鍏ㄥ寲鈥濓紝涓昏鍖呮嫭锛?
- 鍔犲浐鍛戒护琛岃緭鍏ュ鐞嗭紝鍑忓皯寮傚父鍙傛暟瀵艰嚧鐨勫穿婧冩垨閿欒琛屼负
- 鍔犲浐 `WM_COPYDATA` 杈撳叆鏍￠獙锛岄伩鍏嶇暩褰㈡秷鎭薄鏌撴闈㈣繘绋嬭矾寰?- 鍔犲浐鎷栨嫿銆乻hell 鎷夎捣鍜屽彸閿彍鍗曢摼璺?- 淇 Windows 鏂囦欢璇诲彇澶辫触璺緞锛岄伩鍏嶉敊璇湴鎶婂紓甯稿綋鎴?EOF 骞朵骇鍑洪敊璇?Hash
- 绉婚櫎缁存姢鐗?Windows 鍙戣绾块噷鑷姩鎶?Hash 鍙戦€佸埌绗笁鏂圭珯鐐圭殑鍏ュ彛
- 鏀剁揣鎵撳寘銆佹瀯寤恒€佸彂甯冩祦绋嬶紝璁╁彂甯冪墿鍙鐜般€佸彲鍥炴函

褰撳墠缁存姢鐗堥粯璁ゅ畾浣嶆槸锛?
- 鏈湴杩愯
- 涓嶈姹傝处鍙?- 涓嶅仛閬ユ祴
- 涓嶉粯璁や笂浼犳枃浠跺唴瀹?- 涓嶉粯璁や笂浼?Hash 鍒扮涓夋柟鏈嶅姟

## 鏋舵瀯浼樺寲

杩欎竴杞粨搴撳凡缁忎笉鍐嶆槸鏃╂湡閭ｇ鈥滄墍鏈夐€昏緫娣峰湪 UI 鍜岃€佸伐绋嬮噷鈥濈殑鐘舵€併€?鏍稿績涓荤嚎宸茬粡鍋氫簡杩欎簺鏋舵瀯鏀跺彛锛?
- 寮曞叆 `HashRequest`銆乣HashResult`銆乣ProgressEvent`
- 寮曞叆 `HashProgressSink` 鍜?`HashExecutionContext`
- 灏嗘牳蹇冩墽琛岄摼閫愭浠?legacy `ResultData` 涓昏鍒囨崲鍒?`HashResult`
- 灏?WinUI native 鏋勫缓缁熶竴鏀跺彛鍒?`fHashNativeCore`
- 灏嗗ぇ閲忔棫鐨?projection / search / compatibility 灞傞檷绾т负鍏煎澹?- 涓?MFC銆乄inUI銆丆LR bridge銆乁WP bridge 淇濈暀缂栬瘧鍋ュ悍锛屼絾鎶婂叡浜牳蹇冨線涓€?contract 鎺ㄨ繘

鐜板湪鏇存帴杩戠殑涓荤嚎鏄細

`HashRequest -> HashExecutionContext -> HashResult -> Progress/Bridge/Projection`

鑰屼笉鏄繃鍘婚偅绉嶁€淯I銆佺粨鏋滅粨鏋勩€佹墽琛屼笂涓嬫枃褰兼寮鸿€﹀悎鈥濈殑褰㈡€併€?
## 娴嬭瘯涓庨獙璇?
褰撳墠浠撳簱涓嶆槸鈥滆兘缂栬繃灏辩畻瀹屾垚鈥濓紝鑰屾槸甯﹂獙璇侀棬妲涚殑銆?
姣忎釜姝ｅ紡鍙戝竷鍊欓€夊簲閫氳繃锛?
- .NET 鍗曞厓娴嬭瘯
- 鍘熺敓 C++ 杩愯鏃舵祴璇?- 鏋舵瀯鍩虹嚎妫€鏌?- 瀹夊叏鍥炲綊妫€鏌?- Windows 鍘熺敓鏋勫缓鐭╅樀
- 鍙戝竷閾炬紨缁?
鐩墠 GitHub Actions 宸茶鐩栵細

- legacy 妗岄潰绋嬪簭鎵撳寘
- WinUI / CLR bridge 缂栬瘧
- UWP / WinRT bridge 缂栬瘧
- shell extension 缂栬瘧
- publish-release 婕旂粌鍜屾寮?tag 鍙戝竷

## 鍙戝竷璇存槑

- 鍒嗘敮鏋勫缓浼氱敓鎴愬彂甯冩紨缁冨寘
- `v*` 鏍囩浼氳Е鍙戞寮?GitHub Release
- 褰撳墠姝ｅ紡鍙戝竷鐩爣鏄?Windows legacy 妗岄潰绋嬪簭
- 鍙€変唬鐮佺鍚嶆帴鍏ュ凡缁忓湪 CI 閲岀暀濂戒綅缃紝浣嗙湡姝ｆ秷闄も€滃彂甯冭€呮湭鐭モ€濅粛闇€瑕佺湡瀹炵鍚嶈瘉涔?
## 浠撳簱鏂囨。

- 鑻辨枃鏇存柊鏃ュ織锛歔CHANGELOG.md](CHANGELOG.md)
- 涓枃鏇存柊鏃ュ織锛歔CHANGELOG.zh-CN.md](CHANGELOG.zh-CN.md)
- 闅愮璇存槑锛歔PRIVACY.md](PRIVACY.md)
- 寮€婧愬崗璁細[LICENSE](LICENSE)

## Upstream Credit

LHash is built on top of the original [fHash](https://github.com/sunjw/fhash) project by Sun Junwen.
This fork keeps upstream credit and GPL-2.0 licensing intact while maintaining its own tested Windows release line.
