FROM alpine:3

RUN apk add vsftpd ncftp

EXPOSE 2020-2021
EXPOSE 65500-65515

RUN mkdir /var/ftp
WORKDIR /var/ftp

RUN echo "hello, world">test.txt

#RUN ncftpget -C ftp://ftp.bom.gov.au/anon/gen/fwo/IDQ60920.xml /var/ftp/IDQ60920.xml

COPY loader.sh /usr/sbin/

RUN chmod +x /usr/sbin/loader.sh && /usr/sbin/loader.sh

COPY vsftpd.conf /etc/vsftpd/

RUN chown root.root /etc/vsftpd/vsftpd.conf \
    && mkdir -p /var/run/vsftpd/empty

CMD ["/usr/sbin/vsftpd", "/etc/vsftpd/vsftpd.conf"]

# Build: docker build -t ftp-test .
# Run: docker run -d -p 2020-2021:2020-2021 -p 65500-65515:65500-65515 ftp-test
