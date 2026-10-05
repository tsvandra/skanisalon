const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/calendar/AppointmentCard.vue', 'utf8');

const target = Buffer.from('PGRpdiBjbGFzcz0iZmxleCBpdGVtcy1jZW50ZXIgZ2FwLTIgbWQ6Z2FwLTMiPgogICAgICAgIDxkaXYgY2xhc3M9ImZsZXggaXRlbXMtY2VudGVyIGp1c3RpZnktY2VudGVyIHJvdW5kZWQtZnVsbCBmb250LWJvbGQgdGV4dC13aGl0ZSBkcm9wLXNoYWRvdy1zbSBzaGFkb3ctc20i', 'base64').toString('utf8');
const replacement = Buffer.from('PGRpdiBjbGFzcz0iZmxleCBpdGVtcy1jZW50ZXIgZ2FwLTIgbWQ6Z2FwLTMgaG92ZXI6b3BhY2l0eS04MCB0cmFuc2l0aW9uLW9wYWNpdHkiIEBjbGljay5zdG9wPSJnb1RvQ3VzdG9tZXIoYXBwLmN1c3RvbWVySWQpIiB0aXRsZT0iVWdyw6FzIGF6IMO8Z3lmw6lsaGV6Ij4KICAgICAgICA8ZGl2IGNsYXNzPSJmbGV4IGl0ZW1zLWNlbnRlciBqdXN0aWZ5LWNlbnRlciByb3VuZGVkLWZ1bGwgZm9udC1ib2xkIHRleHQtd2hpdGUgZHJvcC1zaGFkb3ctc20gc2hhZG93LXNtIg==', 'base64').toString('utf8');

if(content.includes(target)) {
    content = content.replace(target, replacement);
}

if(!content.includes('useRouter')) {
    content = content.replace('import { computed } from ''vue'';', 'import { computed } from ''vue'';\nimport { useRouter } from ''vue-router'';');
    content = content.replace('const props = defineProps', 'const router = useRouter();\nconst goToCustomer = (id) => {\n  if(id) router.push({ path: ''/ugyfelek'', query: { customerId: id } });\n};\n\nconst props = defineProps');
}

fs.writeFileSync('soluvion-web/src/components/admin/calendar/AppointmentCard.vue', content, 'utf8');