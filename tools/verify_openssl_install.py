#!/usr/bin/env python3
"""Reject a restored vendor install made for a different assembly mode/target."""
import argparse
import json
from pathlib import Path


def verify_install(root: Path, mode: str, target: str) -> dict:
    metadata = json.loads((root / 'lhash-openssl-build-config.json').read_text(encoding='utf-8-sig'))
    expected = {
        'schema_version': 1,
        'openssl_version': '3.5.9',
        'assembly_mode': mode,
        'target': target,
        'assembly_disabled': mode == 'portable',
        'sha256_asm': mode == 'optimized',
        'sha512_asm': mode == 'optimized',
    }
    for key, value in expected.items():
        if type(metadata.get(key)) is not type(value) or metadata.get(key) != value:
            raise ValueError(f'OpenSSL cache/config mismatch: {key}={metadata.get(key)!r}; expected {value!r}')
    for relative in ('include/openssl/configuration.h', 'include/openssl/opensslv.h',
                     'lhash-openssl-build-identity.txt'):
        if not (root / relative).is_file() or (root / relative).stat().st_size == 0:
            raise ValueError(f'Incomplete OpenSSL install: {relative}')
    identity = (root / 'lhash-openssl-build-identity.txt').read_text(encoding='utf-8-sig').strip().split('|')
    if not identity or identity[0] != 'openssl-3.5.9' or mode not in identity or target not in identity:
        raise ValueError('OpenSSL build identity does not match the expected version, target and assembly mode')
    library = 'lib/libcrypto.lib' if target.startswith('VC-') else 'lib/libcrypto.a'
    if not (root / library).is_file() or (root / library).stat().st_size == 0:
        raise ValueError(f'Missing static library: {library}')
    return metadata


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=Path)
    parser.add_argument('mode', choices=('optimized', 'portable'))
    parser.add_argument('target')
    args = parser.parse_args()
    verify_install(args.root, args.mode, args.target)
    print(f'Verified OpenSSL 3.5.9 {args.mode}: {args.target}')
