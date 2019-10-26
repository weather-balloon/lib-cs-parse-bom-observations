#!/bin/sh

obsFiles="IDV60920 IDQ60920 IDN60920 IDT60920 IDS60920 IDW60920 IDD60920"

for f in $obsFiles ; do
    echo Loading ${f}
    ncftpget -C ftp://ftp.bom.gov.au/anon/gen/fwo/${f}.xml /var/ftp/${f}.xml
done
