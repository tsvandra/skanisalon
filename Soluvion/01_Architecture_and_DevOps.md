# 01. Architektúra és DevOps (Architecture & DevOps)

[cite_start]**Dátum:** 2026.02.25. [cite: 1]
[cite_start]**Projekt:** Soluvion (Skani Salon) [cite: 1]
[cite_start]**Státusz:** MVP Stabilizálva, Staging infrastruktúra kiépítve. [cite: 149, 158]

[cite_start]Ez a dokumentum a rendszer technikai alapjait, a felhõs infrastruktúrát, a telepítési folyamatokat (CI/CD) és a biztonsági protokollokat írja le. [cite: 2]

---

## 1. Technológiai Stack (Tech Stack)

[cite_start]A rendszer egy modern, szétválasztott architektúrára (Backend API + SPA Frontend) épül. [cite: 50]

### 1.1. Backend Ecosystem
* [cite_start]**Keretrendszer:** .NET 10 (ASP.NET Core Web API). [cite: 3, 53]
* [cite_start]**Nyelv:** C# 14. [cite: 3]
* [cite_start]**Adatelérés (ORM):** Entity Framework Core (Code-First megközelítés). [cite: 3, 53]
* **Külsõ integrációk (NuGet):**
    * [cite_start]`Npgsql.EntityFrameworkCore.PostgreSQL`: Adatbázis meghajtó. [cite: 3]
    * [cite_start]`BCrypt.Net-Next`: Kriptográfiai jelszókezelés. [cite: 4]
    * [cite_start]`CloudinaryDotNet`: Felhõ alapú képtároláshoz és manipulációhoz. [cite: 6]
    * [cite_start]Hivatalos `OpenAI` .NET kliens a dinamikus SaaS lokalizációhoz (gpt-5.2 modell). [cite: 37, 38]

### 1.2. Frontend Ecosystem
* [cite_start]**Keretrendszer:** Vue 3 (Composition API, `<script setup>` szintaxis). [cite: 22, 53]
* [cite_start]**Build Tool:** Vite. [cite: 22, 53]
* [cite_start]**UI Komponensek:** PrimeVue (v4 szabványok) és PrimeIcons. [cite: 22, 23, 45, 53]
* [cite_start]**Állapotkezelés & Routing:** Lokális/Globális State (provide/inject), Vue Router. [cite: 25, 26, 181]
* [cite_start]**API Kliens:** Axios (Környezeti változókból vezérelt BaseURL-lel). [cite: 26, 165]
* [cite_start]**Extra könyvtárak:** `vuedraggable` (Drag & Drop funkciókhoz). [cite: 24, 189]

---

## 2. Infrastruktúra és Hosting

[cite_start]A rendszer felhõalapú megoldásokat használ, különválasztva az éles (Production) és a teszt (Staging) környezeteket. [cite: 28, 52]

* **Backend Hosting:** Railway felhõ. [cite_start]Külön 'production' és 'staging' dockerizált környezet. [cite: 8, 29, 53]
* [cite_start]**Frontend Hosting:** Netlify (Static site hosting SPA konfigurációval). [cite: 53]
* [cite_start]**Környezeti Változók:** A Netlify UI-n a "Different value for each deploy context" beállítás biztosítja, hogy a megfelelõ `VITE_API_URL` mutasson a PROD, illetve a STAGING backendre. [cite: 35, 152]

---

## 3. CI/CD Pipeline (Élesítés és Tesztelés)

[cite_start]Szigorúan tilos új fejlesztést vagy kísérletezést egyenesen a `main` ágra tolni. [cite: 228] [cite_start]A telepítési folyamat a Git ágak (branches) alapján automatizált. [cite: 59]

* **Éles Környezet (Production):**
    * [cite_start]**Trigger:** Git Push a `main` ágra. [cite: 30]
    * [cite_start]**Folyamat:** A Railway frissíti a fõ konténert, a Netlify pedig frissíti a fõ domaint (pl. `skanisalon.sk`). [cite: 31, 33]
* **Teszt Környezet (Staging):**
    * [cite_start]**Trigger:** Git Push a `develop` ágra. [cite: 30, 229]
    * [cite_start]**Folyamat:** A Netlify automatikusan egy dedikált teszt linket generál (Branch Deploy: `develop--[app-neve].netlify.app`). [cite: 34, 151, 229] [cite_start]Ez a verzió a Railway elkülönített Staging adatbázisával és API-jával kommunikál. [cite: 150, 229]

---

## 4. Adatbázis és Biztonsági Mentés (Database & Backup)

[cite_start]A rendszer relációs adatbázist használ. [cite: 7] [cite_start]A Railway beépített mentéseinek fizetõs korlátai miatt egyedi mentési stratégiát alkalmazunk. [cite: 230]

* [cite_start]**Motor:** PostgreSQL 16+. [cite: 8]
* **Adatbázis Kapcsolat:** A külvilág felé TCP Proxy-n keresztül kommunikál. [cite_start]DBeaver esetén kötelezõ a JDBC URL formátum és a kikényszerített SSL: `jdbc:postgresql://[HOST]:[PORT]/railway?sslmode=require`. [cite: 8, 155, 234]
* [cite_start]**Biztonsági Mentés (Backup):** Automatizált napi mentések futnak a **Backblaze B2 Cloud Storage** tárhelyre, tömörített `.sql.gz` formátumban (pg_dump). [cite: 8, 230]
* **Klónozás és Helyreállítás (PROD -> STAGING):**
    1.  [cite_start]A Railway felhõs adatbázisára lokális parancssorból (CLI) ráereszteni a `dotnet ef database update` parancsot tilos a szigorú TCP proxy szabályok miatt. [cite: 153, 231]
    2.  [cite_start]A Backblaze B2-bõl letöltött `.sql.gz` fájlt ki kell csomagolni (nem a webes UI zip letöltésével, hanem pl. Cyberduck segítségével). [cite: 233]
    3.  [cite_start]A fájlból a terminál-specifikus `\restrict` és `\unrestrict` sorokat törölni kell. [cite: 234]
    4.  [cite_start]A klónozást DBeaver-ben a "Tools -> Execute Script" (psql háttérmotor) funkcióval kell futtatni, mert a sima SQL Editor nem tudja kezelni a PostgreSQL `COPY` parancsait. [cite: 155, 232, 235]

---

## 5. Biztonság és Autentikáció (Security Stack)

[cite_start]A rendszer nem tárol nyílt szöveges jelszavakat és szigorú hálózati házirendeket alkalmaz. [cite: 13]

* [cite_start]**Jelszó Hashing:** A rendszer a **BCrypt** algoritmust (Blowfish cipher) használja. [cite: 14, 15] [cite_start]Regisztrációkor a rendszer egy véletlenszerû "Salt"-ot generál, a jelszót ezzel összefûzi, többszörösen hash-eli, és csak ezt a hasht menti az adatbázisba. [cite: 15, 16] [cite_start]Ez védelmet nyújt a Rainbow Table és Brute Force támadások ellen. [cite: 17]
* [cite_start]**Autentikáció:** Stateless JWT (JSON Web Token) Bearer Token alapokon. [cite: 18, 53] [cite_start]A token HMACSHA512 algoritmussal van aláírva, és payloadként tartalmazza a felhasználó azonosítóját (User ID) és a `CompanyId`-t (Multi-tenancy kulcs). [cite: 19]
* [cite_start]**Hálózati Biztonság (CORS & HTTPS):** A CORS házirend szigorúan szabályozott, csak a Netlify domain és a Localhost engedélyezett a kérésekhez. [cite: 19, 20, 71] [cite_start]A kommunikáció kizárólag kötelezõ HTTPS titkosított csatornán történik (amit a Railway és a Netlify automatikusan kezel). [cite: 20]