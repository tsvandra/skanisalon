const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/dashboard/LowStockPlannerModal.vue', 'utf8');

const target = Buffer.from('cm91dGVyLnB1c2goeyBwYXRoOiAnL21lZ3JlbmRlbGVzZWsnLCBxdWVyeTogeyBkYXRlOiBkYXRlU3RyIH0gfSk7', 'base64').toString('utf8');
const replacement = Buffer.from('cm91dGVyLnB1c2goeyBwYXRoOiAnL21lZ3JlbmRlbGVzZWsnLCBxdWVyeTogeyBkYXRlOiBkYXRlU3RyLCB2aWV3OiAnbW9udGgnIH0gfSk7', 'base64').toString('utf8');

if(content.includes(target)) {
    content = content.replace(target, replacement);
    fs.writeFileSync('soluvion-web/src/components/admin/dashboard/LowStockPlannerModal.vue', content, 'utf8');
    console.log('LowStockPlannerModal updated successfully.');
} else {
    console.log('Target not found in LowStockPlannerModal.vue');
}