#!/usr/bin/env perl
# Verify the generated configuration, not just the requested command line.
# Run in OpenSSL's build directory after Configure; output is saved with the library.
use strict;
use warnings;
no warnings 'once';
use JSON::PP;

my ($mode, $expected_target) = @ARGV;
die "usage: verify_openssl_build_config.pl optimized|portable TARGET\n"
    unless @ARGV == 2 && $mode =~ /\A(?:optimized|portable)\z/;
require './configdata.pm';
my %config = %configdata::config;
my %target = %configdata::target;
my %disabled = %configdata::disabled;
my %unified = %configdata::unified_info;
die "Unexpected OpenSSL target: $config{target}, expected $expected_target\n"
    unless $config{target} eq $expected_target;
my %defines = map { $_ => 1 } @{$unified{defines}{libcrypto} // []};
my $assembly_disabled = exists $disabled{asm};
my $sha256_asm = exists $defines{SHA256_ASM};
my $sha512_asm = exists $defines{SHA512_ASM};
if ($mode eq 'optimized') {
    die "Optimized build did not enable SHA256/SHA512 assembly\n"
        if $assembly_disabled || !$sha256_asm || !$sha512_asm;
} else {
    die "Portable build must disable digest assembly\n"
        unless $assembly_disabled && !$sha256_asm && !$sha512_asm;
}
print JSON::PP->new->canonical->pretty->encode({
    schema_version => 1,
    openssl_version => $config{full_version},
    assembly_mode => $mode,
    target => $config{target},
    asm_arch => $target{asm_arch} // '',
    assembler => $target{AS} // '',
    perlasm_scheme => $target{perlasm_scheme} // '',
    assembly_disabled => $assembly_disabled ? JSON::PP::true : JSON::PP::false,
    sha256_asm => $sha256_asm ? JSON::PP::true : JSON::PP::false,
    sha512_asm => $sha512_asm ? JSON::PP::true : JSON::PP::false,
});
