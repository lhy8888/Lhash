#!/usr/bin/env bash
# Build the same static, no-assembly OpenSSL backend used by Windows tests.
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
prefix="${1:?usage: build_openssl_vendor_posix.sh ABSOLUTE_INSTALL_ROOT}"
case "$prefix" in /*) ;; *) echo 'Install root must be absolute' >&2; exit 2 ;; esac
case "$(uname -s)-$(uname -m)" in
  Darwin-arm64) target=darwin64-arm64-cc ;;
  Darwin-x86_64) target=darwin64-x86_64-cc ;;
  Linux-x86_64) target=linux-x86_64 ;;
  *) echo 'Unsupported test host' >&2; exit 2 ;;
esac
build_root="$(mktemp -d "${TMPDIR:-/tmp}/lhash-openssl.XXXXXX")"
trap 'rm -rf "$build_root"' EXIT
cd "$build_root"
perl "$repo_root/third_party/openssl/3.5.9/Configure" "$target" \
  "--prefix=$prefix" --libdir=lib \
  no-makedepend no-shared no-tests no-module no-ssl no-asm
make -j 3 build_libs
make install_dev
