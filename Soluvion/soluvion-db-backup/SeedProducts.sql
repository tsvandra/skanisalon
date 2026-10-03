-- Fodrászati termékek betöltése minden céghez
DO $$
DECLARE
    comp_id INT;
BEGIN
    FOR comp_id IN SELECT "Id" FROM "Companies"
    LOOP
        -- L'Oréal termékek
        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('L''Oréal Professionnel Dialight (Hajszínező)', '3474630441459', 0, 50, 2500, 4500, 2, 10, true, false, false, comp_id, NOW());

        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('L''Oréal Professionnel Metal Detox Sampon', '3474636974235', 0, 1500, 9500, 15000, 1, 3, true, true, false, comp_id, NOW());

        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('L''Oréal Professionnel Metal Detox Maszk', '3474636974280', 0, 500, 12000, 18500, 1, 2, true, true, false, comp_id, NOW());

        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('L''Oréal INOA Ammóniamentes Hajfesték', '3474630453308', 0, 60, 3100, 5500, 5, 20, true, false, false, comp_id, NOW());
        
        -- Mon Platin termékek
        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('Mon Platin 12 in 1 Classic Hajápoló Spray', '7290011500123', 0, 250, 3500, 6500, 3, 15, true, true, false, comp_id, NOW());

        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('Mon Platin Anti Break Super Mask', '7290011500451', 0, 500, 7500, 12500, 2, 5, true, true, false, comp_id, NOW());

        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('Mon Platin Hyloren Színvédő Sampon', '7290011500789', 0, 500, 4200, 8000, 3, 8, true, true, false, comp_id, NOW());

        -- Egyéb segédeszközök és alapanyagok
        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('Olaplex No.1 Bond Multiplier', '896364002345', 0, 525, 35000, 0, 1, 2, true, false, false, comp_id, NOW());

        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('Olaplex No.2 Bond Perfector', '896364002451', 0, 525, 35000, 0, 1, 2, true, false, false, comp_id, NOW());

        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('Eldobható gumikesztyű (Fekete, M)', '5999887766554', 2, 100, 2500, 0, 20, 150, true, false, false, comp_id, NOW());
        
        INSERT INTO "Products" ("Name", "EAN", "Unit", "PackageSize", "CostPrice", "RetailPrice", "LowStockThreshold", "CurrentStock", "IsProfessional", "IsRetail", "IsDeleted", "CompanyId", "CreationDate")
        VALUES ('Fólia melírozáshoz (100m)', '5999881122334', 2, 1, 1800, 0, 2, 5, true, false, false, comp_id, NOW());
    END LOOP;
END $$;

