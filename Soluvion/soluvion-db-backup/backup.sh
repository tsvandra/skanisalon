#!/bin/sh

TIMESTAMP=$(date +"%Y-%m-%d_%H-%M-%S")
BACKUP_FILE="skanisalon_backup_${TIMESTAMP}.sql"

echo "Mentés indítása: $BACKUP_FILE"
# Itt figyeld, hogy a $DB_URL pontosan legyen írva
pg_dump -d "$DB_URL" > "$BACKUP_FILE"

echo "Feltöltés a Backblaze B2-re..."
aws s3 cp "$BACKUP_FILE" "s3://$S3_BUCKET/$BACKUP_FILE" --endpoint-url "https://$S3_ENDPOINT"

echo "Kész! Takarítás..."
rm "$BACKUP_FILE"

echo "A mentés sikeresen befejeződött!"