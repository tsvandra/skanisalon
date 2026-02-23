#!/bin/sh

echo "KÜLDETÉS INDÍTÁSA: Éles adatbázis klónozása a Játszótérre (PROD -> DEV)..."

# Ez a sor letölti az éles adatokat, és azonnal betölti a fejlesztőibe
pg_dump -d "postgresql://postgres:uelIqQhiDpmmdFoUNVzEitQMbFbEVYsM@yamabiko.proxy.rlwy.net:12946/railway" --clean | psql -d "postgresql://postgres:wIUNlrNDUERNcnKcjSAENsYpEioDqaQW@nozomi.proxy.rlwy.net:36230/railway"

echo "KÜLDETÉS TELJESÍTVE! Minden adat átmásolva!"