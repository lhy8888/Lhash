#!/usr/bin/env python3
"""Build a native Win32 control test from the exact product rendering method.

The thin CEdit adapter calls real Windows APIs; it does not emulate EM_SETSEL.
This verifies native text/selection against the production display cache, not
the complete MFC painter or pixel appearance.
"""
from pathlib import Path
import hashlib
import json
import sys

repo = Path(__file__).resolve().parents[2]
output = Path(sys.argv[1])
output.mkdir(parents=True, exist_ok=True)
relative = 'trunk/source/WinMFC/HyperEditHash.cpp'
source = (repo / relative).read_text(encoding='utf-8-sig')
signature = 'void CHyperEditHash::ShowTextUpdate('
start = source.index(signature)
opening = source.index('{', start)
depth = 0
for end in range(opening, len(source)):
    depth += (source[end] == '{') - (source[end] == '}')
    if depth == 0:
        method = source[start:end + 1]
        break
else:
    raise ValueError('Unterminated production ShowTextUpdate')

template = Path(__file__).with_name('native_control_tests.cpp.in').read_text()
marker = '@@PRODUCTION_SHOW_TEXT_UPDATE@@'
assert template.count(marker) == 1
(output / 'native_control_tests.cpp').write_text(template.replace(marker, method))
(output / 'native-control-extraction.json').write_text(json.dumps({
    'path': relative,
    'signature': signature,
    'first_line': source[:start].count('\n') + 1,
    'method_sha256': hashlib.sha256(method.encode()).hexdigest(),
    'source_sha256': hashlib.sha256((repo / relative).read_bytes()).hexdigest(),
    'scope': 'Production method plus real Win32 EDIT; thin CEdit API adapter. No full MFC painter/pixel assertion.'
}, indent=2) + '\n')
