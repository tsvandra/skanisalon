const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/calendar/AppointmentEditorModal.vue', 'utf8');

const t = 'const basePayload = {';
const r =       // Update customer default formulas if requested
      const defaultsToAdd = form.value.extraMaterials.filter(em => em.saveAsDefault);
      if (defaultsToAdd.length > 0 && finalCustId && finalCustId !== 'new') {
        try {
          const custRes = await bookingApi.getCustomerById(finalCustId);
          const customer = custRes.data;
          let formula = [];
          if (customer.attributes && customer.attributes.FormulaList) {
             try { formula = JSON.parse(customer.attributes.FormulaList); } catch(e){}
          }
          defaultsToAdd.forEach(d => {
             const existing = formula.find(f => f.productId === d.productId);
             if (existing) { existing.quantity += d.quantity; }
             else { formula.push({ productId: d.productId, quantity: d.quantity, notes: 'Hozzáadva foglalás szerkesztésből' }); }
          });
          if (!customer.attributes) customer.attributes = {};
          customer.attributes.FormulaList = JSON.stringify(formula);
          await apiClient.put('/api/CompanyAttributes/customer-attributes/' + customer.id, customer.attributes);
        } catch(err) {
          console.error("Nem sikerült menteni az ügyfél alapértelmezett anyagait", err);
        }
      }

      const basePayload = {
        extraMaterials: JSON.stringify(form.value.extraMaterials.map(em => ({ productId: em.productId, quantity: em.quantity, name: em.name }))),;

if (!content.includes('extraMaterials: JSON.stringify')) {
    content = content.replace(t, r);
    fs.writeFileSync('soluvion-web/src/components/admin/calendar/AppointmentEditorModal.vue', content, 'utf8');
}