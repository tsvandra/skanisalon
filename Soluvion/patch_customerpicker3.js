const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/calendar/CustomerPicker.vue', 'utf8');
const targetRegex = /<label class="block text-\[10px\] md:text-xs font-bold text-text-muted mb-1\.5 uppercase flex items-center gap-1">[\s\S]*?<\/label>/;
const replacement = Buffer.from('PGxhYmVsIGNsYXNzPSJibG9jayB0ZXh0LVsxMHB4XSBtZDp0ZXh0LXhzIGZvbnQtYm9sZCB0ZXh0LXRleHQtbXV0ZWQgbWItMS41IHVwcGVyY2FzZSBmbGV4IGl0ZW1zLWNlbnRlciBqdXN0aWZ5LWJldHdlZW4iPgogICAgICA8ZGl2IGNsYXNzPSJmbGV4IGl0ZW1zLWNlbnRlciBnYXAtMSI+PGkgY2xhc3M9InBpIHBpLXVzZXIiPjwvaT4ge3sgJHQoJ2NhbGVuZGFyLmVkaXRvci5jbGllbnQnKSB9fTwvZGl2PgogICAgICA8YnV0dG9uIHYtaWY9Im1vZGVsVmFsdWUgJiYgbW9kZWxWYWx1ZSAhPT0gJ25ldyciIEBjbGljay5zdG9wPSJnb1RvQ3VzdG9tZXIiIGNsYXNzPSJ0ZXh0LXByaW1hcnkgaG92ZXI6dGV4dC1wcmltYXJ5LzcwIHRyYW5zaXRpb24tY29sb3JzIGZsZXggaXRlbXMtY2VudGVyIGdhcC0xIiB0aXRsZT0iVWdyw6FzIGF6IMO8Z3lmw6lsIGthcnRvbmrDoXJhIj4KICAgICAgICA8aSBjbGFzcz0icGkgcGktZXh0ZXJuYWwtbGluayI+PC9pPiBLYXJ0b24KICAgICAgPC9idXR0b24+CiAgICA8L2xhYmVsPg==', 'base64').toString('utf8');

content = content.replace(targetRegex, replacement);
fs.writeFileSync('soluvion-web/src/components/admin/calendar/CustomerPicker.vue', content, 'utf8');