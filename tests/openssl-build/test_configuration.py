#!/usr/bin/env python3
"""Generate actual OpenSSL configurations and exercise the production verifier.

This is a configuration regression, not a cross-platform execution/throughput test.
Native builds and known-answer tests run separately in the platform CI jobs.
"""
import json
import importlib.util
from pathlib import Path
import platform
import subprocess
import tempfile

repo = Path(__file__).resolve().parents[2]
configure = repo / 'third_party/openssl/3.5.9/Configure'
verify = repo / 'tools/verify_openssl_build_config.pl'
host = (platform.system(), platform.machine().lower())
if host == ('Linux', 'x86_64'):
    targets = ['linux-x86_64', 'darwin64-arm64-cc']
elif host == ('Darwin', 'arm64'):
    targets = ['darwin64-arm64-cc']
else:
    raise SystemExit(f'Unsupported configuration test host: {host}')

cases = 0
with tempfile.TemporaryDirectory(prefix='lhash-openssl-config-') as temporary:
    for target in targets:
        for mode in ('optimized', 'portable'):
            build = Path(temporary) / f'{target}-{mode}'
            build.mkdir()
            options = ['no-makedepend', 'no-shared', 'no-tests', 'no-module', 'no-ssl']
            if mode == 'portable':
                options.append('no-asm')
            configured = subprocess.run(['perl', str(configure), target, *options],
                                        cwd=build, text=True, capture_output=True)
            if configured.returncode:
                raise RuntimeError(configured.stdout + configured.stderr)
            verified = subprocess.run(['perl', str(verify), mode, target],
                                      cwd=build, text=True, capture_output=True)
            if verified.returncode:
                raise RuntimeError(verified.stdout + verified.stderr)
            facts = json.loads(verified.stdout)
            assert facts['openssl_version'] == '3.5.9'
            assert facts['target'] == target
            assert facts['sha256_asm'] == (mode == 'optimized')
            assert facts['sha512_asm'] == (mode == 'optimized')
            assert facts['assembly_disabled'] == (mode == 'portable')
            # A cached library/configuration from the opposite mode must be rejected.
            opposite = 'portable' if mode == 'optimized' else 'optimized'
            wrong_mode = subprocess.run(['perl', str(verify), opposite, target],
                                        cwd=build, text=True, capture_output=True)
            assert wrong_mode.returncode != 0, wrong_mode.stdout
            wrong_target = subprocess.run(['perl', str(verify), mode, 'wrong-target'],
                                          cwd=build, text=True, capture_output=True)
            assert wrong_target.returncode != 0, wrong_target.stdout
            print(f'PASS {target} {mode}: digest assembly selection, wrong-mode and wrong-target rejection')
            cases += 3
print(f'PASS OpenSSL configuration checks={cases}')

# Synthetic installations exercise cache rejection only, not library validity.
# Native CI separately links/runs against the verified real installations.
spec = importlib.util.spec_from_file_location('verify_install', repo / 'tools/verify_openssl_install.py')
install_verifier = importlib.util.module_from_spec(spec)
spec.loader.exec_module(install_verifier)
with tempfile.TemporaryDirectory(prefix='lhash-openssl-cache-') as temporary:
    install = Path(temporary)
    (install / 'include/openssl').mkdir(parents=True)
    (install / 'lib').mkdir()
    for header in ('configuration.h', 'opensslv.h'):
        (install / 'include/openssl' / header).write_text('/* Cache-contract fixture, not a real header. */\n')
    identity = install / 'lhash-openssl-build-identity.txt'
    identity.write_text('openssl-3.5.9|linux-x86_64|optimized|' + 'a' * 64 + '|' + 'b' * 64 + '|||\n')
    library = install / 'lib/libcrypto.a'
    library.write_bytes(b'cache-contract-fixture')
    metadata_path = install / 'lhash-openssl-build-config.json'
    metadata = dict(schema_version=1, openssl_version='3.5.9', assembly_mode='optimized',
                    target='linux-x86_64', assembly_disabled=False, sha256_asm=True, sha512_asm=True)
    def save_metadata(value):
        metadata_path.write_text(json.dumps(value))
    def reject(label, mode='optimized', target='linux-x86_64'):
        try:
            install_verifier.verify_install(install, mode, target)
        except (ValueError, OSError):
            print(f'PASS cache rejection: {label}')
        else:
            raise AssertionError(f'Invalid cache accepted: {label}')
    save_metadata(metadata)
    install_verifier.verify_install(install, 'optimized', 'linux-x86_64')
    reject('opposite mode', 'portable')
    reject('wrong target', target='darwin64-arm64-cc')
    for field, invalid in [('sha256_asm', 1), ('sha512_asm', 'true'),
                           ('assembly_disabled', 0), ('schema_version', True),
                           ('openssl_version', '3.5.8')]:
        save_metadata({**metadata, field: invalid})
        reject(f'invalid {field}')
    save_metadata(metadata)
    library.unlink()
    reject('missing library')
    library.touch()
    reject('empty library')
    library.write_bytes(b'cache-contract-fixture')
    identity.unlink()
    reject('unfinished installation without identity')
print('PASS OpenSSL cache contract checks=11')
