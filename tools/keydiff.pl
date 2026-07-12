#!/usr/bin/perl
use strict; use warnings;
my ($bk,$fl)=@ARGV;
open(my $a,"<",$bk) or die "bk: $!";
open(my $b,"<",$fl) or die "fl: $!";
my ($ln,$mismatch,$shown)=(0,0,0);
# key = chars up to first UNESCAPED '='  (allow \=)
sub keyof {
  my $s=shift;
  if($s=~/^((?:[^=\\]|\\.)*)=/){ return $1; }
  return undef;
}
while(defined(my $la=<$a>)){
  my $lb=<$b>; $ln++;
  last unless defined $lb;
  chomp $la; chomp $lb;
  next if $la=~m{^//} || $la eq "";
  my $ka=keyof($la); my $kb=keyof($lb);
  next unless defined $ka;
  if(!defined $kb || $ka ne $kb){
    $mismatch++;
    if($shown<12){ $shown++;
      my $sa=substr($ka,0,50);
      my $sb=defined($kb)?substr($kb,0,50):"(none)";
      print "L$ln  EN-key : $sa\n      FILE-key: $sb\n";
    }
  }
}
print "\nTotal lines with mismatched KEY (EN-BACKUP vs FILE): $mismatch\n";
