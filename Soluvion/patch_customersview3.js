const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/views/CustomersView.vue', 'utf8');

const watchReplacement =   watch(() => route.query.customerId, (newId) => {
    if (newId) {
      const c = customers.value.find(x => x.id == newId);
      if (c) openHistoryModal(c);
    } else {
      isHistoryModalOpen.value = false;
    }
  });

  onMounted(() => {;

content = content.replace("onMounted(() => {", watchReplacement);

fs.writeFileSync('soluvion-web/src/views/CustomersView.vue', content, 'utf8');