const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/calendar/AppointmentCard.vue', 'utf8');

const t1 = '<div class="flex items-center gap-2 md:gap-3">\n        <div class="flex items-center justify-center rounded-full font-bold text-white drop-shadow-sm shadow-sm"';
const r1 = '<div class="flex items-center gap-2 md:gap-3 hover:opacity-80 transition-opacity" @click.stop="goToCustomer(app.customerId)" title="Ugrás az ügyfélhez">\n        <div class="flex items-center justify-center rounded-full font-bold text-white drop-shadow-sm shadow-sm"';
const t2 = '<div class="flex items-center gap-2 md:gap-3">\r\n        <div class="flex items-center justify-center rounded-full font-bold text-white drop-shadow-sm shadow-sm"';
const r2 = '<div class="flex items-center gap-2 md:gap-3 hover:opacity-80 transition-opacity" @click.stop="goToCustomer(app.customerId)" title="Ugrás az ügyfélhez">\r\n        <div class="flex items-center justify-center rounded-full font-bold text-white drop-shadow-sm shadow-sm"';

if(content.includes(t1)) content = content.replace(t1, r1);
else if(content.includes(t2)) content = content.replace(t2, r2);

if(!content.includes('useRouter')) {
    content = content.replace(/import { computed } from 'vue';/g, "import { computed } from 'vue';\nimport { useRouter } from 'vue-router';");
    content = content.replace(/const props = defineProps/g, "const router = useRouter();\nconst goToCustomer = (id) => {\n  if(id) router.push({ path: '/ugyfelek', query: { customerId: id } });\n};\n\nconst props = defineProps");
}

fs.writeFileSync('soluvion-web/src/components/admin/calendar/AppointmentCard.vue', content, 'utf8');