const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/calendar/AppointmentEditorModal.vue', 'utf8');

const t1 = '<span class="text-xs text-text-muted font-bold">mennyiség</span>';
const r1 = '<span class="text-xs text-text-muted font-bold">{{ newExtraProduct ? (allProducts.find(p => p.id === newExtraProduct)?.unit || \\'db\\') : \\'mennyiség\\' }}</span>';

if (content.includes(t1)) {
    content = content.replace(t1, r1);
    fs.writeFileSync('soluvion-web/src/components/admin/calendar/AppointmentEditorModal.vue', content, 'utf8');
    console.log('Fixed unit label!');
} else {
    console.log('Target string not found.');
}