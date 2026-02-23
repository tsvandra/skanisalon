# Használjuk a hivatalos Postgres képet, amiben benne van a pg_dump eszköz
FROM postgres:16-alpine

# Feltelepítjük az AWS CLI-t (ezzel tudunk a Backblaze S3-ra másolni)
RUN apk add --no-cache aws-cli

# Bemásoljuk a mi kis mentő scriptünket
COPY backup.sh /backup.sh
RUN chmod +x /backup.sh

# Amikor elindul a robot, ezt a scriptet fogja lefuttatni
CMD ["/backup.sh"]