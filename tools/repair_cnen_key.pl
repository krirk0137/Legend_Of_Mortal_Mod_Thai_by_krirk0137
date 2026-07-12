#!/usr/bin/perl
# Repair a gold TSV where the worker's "key" field is actually "CN=EN" (the
# full source line) instead of just CN. Extract the true CN key using the
# same negative-lookbehind-for-backslash rule as _progress/extract_keys.pl,
# then re-emit CN<TAB>THAI.
use strict; use warnings;
binmode STDIN, ':utf8'; binmode STDOUT, ':utf8';
my ($bad, $total) = (0, 0);
while (<STDIN>) {
    chomp;
    my ($left, $thai) = split(/\t/, $_, 2);
    if (!defined $thai) { $bad++; print STDERR "NO-TAB-LINE: $_\n"; next; }
    if ($left =~ /^(.*?)(?<!\\)=(.*)$/s) {
        my $cn = $1;
        print "$cn\t$thai\n";
        $total++;
    } else {
        $bad++;
        print STDERR "NO-MATCH: $left\n";
    }
}
print STDERR "repaired: $total, bad: $bad\n";
