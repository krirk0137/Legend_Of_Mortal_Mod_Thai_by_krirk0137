#!/usr/bin/perl
use strict; use warnings;
binmode STDIN, ':utf8'; binmode STDOUT, ':utf8';
my $n = 0;
while (<STDIN>) {
    next if /^\s*\/\// || /^\s*$/;
    if (/^(.*?)(?<!\\)=(.*)$/s) {
        my $v = $2;
        next if $v =~ /[\x{0E00}-\x{0E7F}]/;   # has Thai -> fine
        $n++;
        print "NO-THAI: $_";
    }
}
print "=== total no-Thai-value lines: $n ===\n";
