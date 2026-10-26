#!/usr/bin/env bash
# Static OpenSSL with upstream assembly by default; portable is an explicit fallback.
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
prefix="${1:?usage: build_openssl_vendor_posix.sh ABSOLUTE_INSTALL_ROOT [optimized|portable]}"
assembly_mode="${2:-optimized}"
if [[ $# -gt 2 || ( "$assembly_mode" != optimized && "$assembly_mode" != portable ) ]]; then
  echo 'Assembly mode must be optimized or portable' >&2
  exit 2
fi
case "$prefix" in /*) ;; *) echo 'Install root must be absolute' >&2; exit 2 ;; esac
case "$(uname -s)-$(uname -m)" in
  Darwin-arm64) target=darwin64-arm64-cc ;;
  Linux-x86_64) target=linux-x86_64 ;;
  *) echo 'Unsupported test host' >&2; exit 2 ;;
esac
configure_options=(no-makedepend no-shared no-tests no-module no-ssl)
if [[ "$assembly_mode" == portable ]]; then
  configure_options+=(no-asm)
fi
validator="$repo_root/tools/verify_openssl_build_config.pl"
build_root="$(mktemp -d "${TMPDIR:-/tmp}/lhash-openssl.XXXXXX")"
trap 'rm -rf "$build_root"' EXIT
mkdir -p "$prefix"
# An interrupted rebuild must not leave a completed-build marker from another mode.
rm -f "$prefix/lhash-openssl-build-identity.txt" "$prefix/lhash-openssl-build-config.json"
cd "$build_root"
echo "OpenSSL target=$target assembly_mode=$assembly_mode"
perl "$repo_root/third_party/openssl/3.5.9/Configure" "$target" \
  "--prefix=$prefix" --libdir=lib \
  "${configure_options[@]}"
perl "$validator" "$assembly_mode" "$target" > lhash-openssl-build-config.json
make -j 3 build_libs
make install_dev
cp lhash-openssl-build-config.json "$prefix/lhash-openssl-build-config.json"
script_hash="$(perl -MDigest::SHA -e 'local $/; open my $f, "<", $ARGV[0] or die $!; print Digest::SHA::sha256_hex(<$f>);' "$repo_root/tools/build_openssl_vendor_posix.sh")"
validator_hash="$(perl -MDigest::SHA -e 'local $/; open my $f, "<", $ARGV[0] or die $!; print Digest::SHA::sha256_hex(<$f>);' "$validator")"
printf '%s\n' "openssl-3.5.9|$target|$assembly_mode|$script_hash|$validator_hash|${CC:-}|${CFLAGS:-}|${LDFLAGS:-}" > "$prefix/lhash-openssl-build-identity.txt"
