#!/usr/bin/perl
# Apply ONLY the gold lines whose KEY itself contains a literal TAB
# (apply_thai.pl can't handle these because it splits key/value on the first tab).
# Key = all fields except the last, rejoined with TAB. Value = last field.
# Usage: perl emb_apply.pl <target_txt> <gold_tsv>
use strict; use warnings;
my ($file, $gold) = @ARGV;
die "usage\n" unless $file && $gold;

open my $gf, "<", $gold or die "open gold: $!";
binmode $gf;
my @rules;
while (my $l = <$gf>) {
    $l =~ s/\r?\n$//;
    next if $l eq "";
    my @f = split(/\t/, $l, -1);
    next unless @f > 2;            # only embedded-tab keys (>=2 internal-or-delim tabs)
    my $v = pop @f;                # value = last field
    my $k = join("\t", @f);        # key = everything before the last tab
    push @rules, [$k, $v];
}
close $gf;
print "embedded-tab rules found: ", scalar(@rules), "\n";

local $/;
open my $fh, "<", $file or die "open target: $!";
binmode $fh;
my $data = <$fh>;
close $fh;

my $total = 0;
for my $r (@rules) {
    my ($k, $v) = @$r;
    my $c = ($data =~ s/^\Q$k\E=[^\n]*/$k=$v/mg);
    $total += ($c || 0);
    my $disp = $k; $disp =~ s/\t/<TAB>/g;
    print "$disp -> ", ($c||0), "\n";
}
open my $out, ">", $file or die "open write: $!";
binmode $out;
print $out $data;
close $out;
print "TOTAL embedded-tab applied: $total\n";
