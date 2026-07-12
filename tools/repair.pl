#!/usr/bin/perl
use strict; use warnings;
# Line-aligned repair: where current FILE's key != EN-BACKUP's key,
# restore the EN-BACKUP line (correct CN key + English). Else keep FILE line (has correct Thai).
my ($bk,$fl,$out)=@ARGV;
open(my $a,"<",$bk) or die "bk: $!";
open(my $b,"<",$fl) or die "fl: $!";
open(my $o,">",$out) or die "out: $!";
sub keyof { my $s=shift; return $1 if $s=~/^((?:[^=\\]|\\.)*)=/; return undef; }
my ($ln,$restored)=(0,0);
while(defined(my $la=<$a>)){
  my $lb=<$b>; $ln++;
  if(!defined $lb){ print $o $la; next; }
  my ($ca,$cb)=($la,$lb); chomp $ca; chomp $cb;
  # comments/blank/section headers: keep FILE's version
  if($ca=~m{^//} || $ca eq ""){ print $o $lb; next; }
  my $ka=keyof($ca); my $kb=keyof($cb);
  if(defined $ka && (!defined $kb || $ka ne $kb)){
    print $o $la;      # restore pristine EN-BACKUP line
    $restored++;
  } else {
    print $o $lb;      # keep current (correct) line
  }
}
close $a; close $b; close $o;
print "lines restored from EN-BACKUP: $restored\n";
