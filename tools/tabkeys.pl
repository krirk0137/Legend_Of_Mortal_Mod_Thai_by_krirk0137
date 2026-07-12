#!/usr/bin/perl
use strict; use warnings;
binmode STDIN, ':utf8'; binmode STDOUT, ':utf8';
while (<STDIN>) {
    next if /^\s*\/\// || /^\s*$/;
    if (/^(.*?)(?<!\\)=/) {
        my $k = $1;
        if ($k =~ /\t/) {
            my $disp = $_;
            $disp =~ s/\t/<TAB>/g;
            print "LINE $.: $disp";
        }
    }
}
