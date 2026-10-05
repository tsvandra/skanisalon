const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/customers/CustomerHistoryModal.vue', 'utf8');

// Fix clici
content = content.replace(/@clici=/g, '@click=');

// Fix getUnit
content = content.replace('p.unitOfMeasure', 'p.unit');

fs.writeFileSync('soluvion-web/src/components/admin/customers/CustomerHistoryModal.vue', content, 'utf8');