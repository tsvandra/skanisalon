const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/calendar/CalendarGrid.vue', 'utf8');

const target = Buffer.from('Y29uc3Qgcm91dGUgPSB1c2VSb3V0ZSgpOyBjb25zdCBjdXJyZW50VmlldyA9IHJlZihyb3V0ZS5xdWVyeS5kYXRlID8gImRheSIgOiAibW9udGgiKTs=', 'base64').toString('utf8');
const replacement = Buffer.from('Y29uc3Qgcm91dGUgPSB1c2VSb3V0ZSgpOyBjb25zdCBjdXJyZW50VmlldyA9IHJlZihyb3V0ZS5xdWVyeS52aWV3ID8gcm91dGUucXVlcnkudmlldyA6IChyb3V0ZS5xdWVyeS5kYXRlID8gImRheSIgOiAibW9udGgiKSk7', 'base64').toString('utf8');

if(content.includes(target)) {
    content = content.replace(target, replacement);
    fs.writeFileSync('soluvion-web/src/components/admin/calendar/CalendarGrid.vue', content, 'utf8');
    console.log('CalendarGrid updated successfully.');
} else {
    console.log('Target not found in CalendarGrid.vue');
}