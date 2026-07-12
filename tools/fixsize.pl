#!/usr/bin/perl
use strict; use warnings;
# Reconstruct malformed "<size\<TAB>24>...=<size\=20>THAI" rows into proper CN<TAB>THAI.
# Key uses <size\=24> (display), value uses <size\=20> (translated); separator is =<size\=20>.
my ($in,$out)=@ARGV;
open(my $i,"<",$in) or die "in: $!";
open(my $o,">",$out) or die "out: $!";
my $n=0;
while(my $l=<$i>){
  chomp $l;
  my ($f1,$rest)=split(/\t/,$l,2);
  next unless defined $rest;
  if($f1 eq "<size\\"){                 # malformed fragment key
    my $sep="=<size\\=20>";
    my $idx=index($rest,$sep);
    next if $idx < 0;                   # can't locate separator -> skip
    my $keyc=substr($rest,0,$idx);      # "24>...</size>[...]"
    my $val =substr($rest,$idx+1);      # "<size\=20>THAI..."
    my $key ="<size\\=".$keyc;          # "<size\=24>...</size>"
    print $o "$key\t$val\n";
    $n++;
  }
}
close $i; close $o;
print "reconstructed rows: $n\n";
