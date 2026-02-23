#!/bin/sh

# Pontos dátum és időpont generálása a fájlnévhez
TIMESTAMP=$(date +"%Y-%m-%d_%H-%M-%S")
BACKUP_FILE="skanisalon_backup_${TIMESTAMP}.sql"

echo "Mentés indítása: $BACKUP_FILE"
# Letölti az adatbázist a megadott URL-ről
pg_dump -d $DB_URL > $BACKUP_FILE

echo "Feltöltés a Backblaze B2-re..."
# Átküldi a fájlt a Backblaze tárolóba a megadott kulcsokkal
aws s3 cp $BACKUP_FILE s3://$S3_BUCKET/$BACKUP_FILE --endpoint-url https://$S3_ENDPOINT

echo "Kész! Takarítás..."
# Letörli a fájlt a kis robot memóriájából, hogy ne foglalja a helyet
rm $BACKUP_FILE

echo "A mentés sikeresen befejeződött!"