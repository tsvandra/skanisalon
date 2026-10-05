const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/views/CustomersView.vue', 'utf8');

const t = 'onMounted(() => {';
const r = 'watch(() => route.query.customerId, (newId) => {\n    if (newId) {\n      const c = customers.value.find(x => x.id == newId);\n      if (c) openHistoryModal(c);\n    } else {\n      isHistoryModalOpen.value = false;\n    }\n  });\n\n  onMounted(() => {';

content = content.replace(t, r);
fs.writeFileSync('soluvion-web/src/views/CustomersView.vue', content, 'utf8');