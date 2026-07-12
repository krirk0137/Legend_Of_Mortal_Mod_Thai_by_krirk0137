#!/usr/bin/perl
# Reusable: apply Thai values keyed by the Chinese source.
# Usage: perl apply_thai.pl <target_txt> <pairs_tsv>
# pairs_tsv lines: <chinese_key>\t<thai_value>   (blank lines / #comments ignored)
# Matches full line "^KEY=<anything>" and rewrites value to Thai (byte-level, UTF-8/LF).
use strict; use warnings;

my ($file, $pairs) = @ARGV;
die "usage: apply_thai.pl <target> <pairs.tsv>\n" unless $file && $pairs;

open my $pf, "<", $pairs or die "open pairs: $!";
my @rules;
while (my $l = <$pf>) {
  $l =~ s/\r?\n$//;
  next if $l eq "" || $l =~ /^#/;
  my ($k, $v) = split(/\t/, $l, 2);
  next unless defined $k && defined $v && $k ne "";
  push @rules, [$k, $v];
}
close $pf;

local $/;
open my $fh, "<", $file or die "open target: $!";
my $data = <$fh>;
close $fh;

my $total = 0;
for my $r (@rules) {
  my ($k, $v) = @$r;
  # SAFETY GUARD: a key ending in a backslash (e.g. the fragment "<size\") makes
  # the /^\Q$k\E=/ pattern collapse to "^<size\=" and carpet-overwrite EVERY line
  # starting with that prefix. This is exactly what destroyed 682 lines once.
  # Skip such malformed keys instead of applying them.
  if ($k =~ /\\$/) {
    printf("%-24s -> SKIPPED (malformed key ends with backslash)\n", $k);
    next;
  }
  my $count = ($data =~ s/^\Q$k\E=[^\n]*/$k=$v/mg);
  $total += ($count || 0);
  printf("%-24s -> %d\n", $k, ($count||0));
}

open my $out, ">", $file or die "open write: $!";
print $out $data;
close $out;
print "\nTOTAL: $total\n";
