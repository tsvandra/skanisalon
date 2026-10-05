const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/views/CustomersView.vue', 'utf8');

const t1 = '<script setup>\n  import { ref, computed, onMounted } from \\'vue\\';\n  import bookingApi from \\'@/services/bookingApi\\';\n  import attributesApi from \\'@/services/companyAttributesApi\\'; // <- Fontos: Itt hívjuk meg az új API-t!\n  import { getCustomerColor } from \\'@/utils/colorUtils\\';\n  import CustomerHistoryModal from \\'@/components/admin/customers/CustomerHistoryModal.vue\\';\n\n  const customers = ref([]);';
const t2 = '<script setup>\r\n  import { ref, computed, onMounted } from \'vue\';\r\n  import bookingApi from \'@/services/bookingApi\';\r\n  import attributesApi from \'@/services/companyAttributesApi\'; // <- Fontos: Itt hívjuk meg az új API-t!\r\n  import { getCustomerColor } from \'@/utils/colorUtils\';\r\n  import CustomerHistoryModal from \'@/components/admin/customers/CustomerHistoryModal.vue\';\r\n\r\n  const customers = ref([]);';

const r1 = Buffer.from('PHNjcmlwdCBzZXR1cD4KICBpbXBvcnQgeyByZWYsIGNvbXB1dGVkLCBvbk1vdW50ZWQsIHdhdGNoIH0gZnJvbSAndnVlJzsKICBpbXBvcnQgeyB1c2VSb3V0ZSwgdXNlUm91dGVyIH0gZnJvbSAndnVlLXJvdXRlcic7CiAgaW1wb3J0IGJvb2tpbmdBcGkgZnJvbSAnQC9zZXJ2aWNlcy9ib29raW5nQXBpJzsKICBpbXBvcnQgYXR0cmlidXRlc0FwaSBmcm9tICdAL3NlcnZpY2VzL2NvbXBhbnlBdHRyaWJ1dGVzQXBpJzsgLy8gPC0gRm9udG9zOiBJdHQgaMOtdmp1ayBtZWcgYXogw7pqIEFQSS10IQogIGltcG9ydCB7IGdldEN1c3RvbWVyQ29sb3IgfSBmcm9tICdAL3V0aWxzL2NvbG9yVXRpbHMnOwogIGltcG9ydCBDdXN0b21lckhpc3RvcnlNb2RhbCBmcm9tICdAL2NvbXBvbmVudHMvYWRtaW4vY3VzdG9tZXJzL0N1c3RvbWVySGlzdG9yeU1vZGFsLnZ1ZSc7CgogIGNvbnN0IHJvdXRlID0gdXNlUm91dGUoKTsKICBjb25zdCByb3V0ZXIgPSB1c2VSb3V0ZXIoKTsKCiAgY29uc3QgY3VzdG9tZXJzID0gcmVmKFtdKTs=', 'base64').toString('utf8');

if (content.includes(t1)) content = content.replace(t1, r1);
else if (content.includes(t2)) content = content.replace(t2, r1);
else {
    // regex fallback
    content = content.replace(/<script setup>[\s\S]*?import CustomerHistoryModal[\s\S]*?const customers = ref\(\[\]\);/, r1);
}

const tClose1 = 'const closeHistoryModal = () => {\n    isHistoryModalOpen.value = false;\n    selectedCustomerHistory.value = null;\n  };';
const tClose2 = 'const closeHistoryModal = () => {\r\n    isHistoryModalOpen.value = false;\r\n    selectedCustomerHistory.value = null;\r\n  };';
const rClose = 'const closeHistoryModal = () => {\n    isHistoryModalOpen.value = false;\n    selectedCustomerHistory.value = null;\n    if (route.query.customerId) {\n      const q = { ...route.query };\n      delete q.customerId;\n      router.replace({ query: q });\n    }\n  };';

if (content.includes(tClose1)) content = content.replace(tClose1, rClose);
else if (content.includes(tClose2)) content = content.replace(tClose2, rClose);

const tFetchEnd1 = 'companyAttributes.value = allAttrs.filter(a => a.isActive);\n\n    } catch (error) {';
const tFetchEnd2 = 'companyAttributes.value = allAttrs.filter(a => a.isActive);\r\n\r\n    } catch (error) {';
const rFetchEnd = 'companyAttributes.value = allAttrs.filter(a => a.isActive);\n\n      if (route.query.customerId) {\n        const c = customers.value.find(x => x.id == route.query.customerId);\n        if (c) openHistoryModal(c);\n      }\n    } catch (error) {';

if (content.includes(tFetchEnd1)) content = content.replace(tFetchEnd1, rFetchEnd);
else if (content.includes(tFetchEnd2)) content = content.replace(tFetchEnd2, rFetchEnd);

fs.writeFileSync('soluvion-web/src/views/CustomersView.vue', content, 'utf8');