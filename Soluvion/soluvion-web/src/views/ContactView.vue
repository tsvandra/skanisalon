<script setup>
import { ref } from 'vue';
import Card from 'primevue/card';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Button from 'primevue/button';
import Message from 'primevue/message';

const name = ref('');
const email = ref('');
const messageText = ref('');
const submitted = ref(false);

const sendMessage = () => {
    // Itt később beköthetjük a valódi email küldést az API-n keresztül
    if (name.value && email.value && messageText.value) {
        submitted.value = true;
        // Űrlap törlése
        name.value = '';
        email.value = '';
        messageText.value = '';

        // 3 másodperc múlva eltűnik a sikerüzenet
        setTimeout(() => {
            submitted.value = false;
        }, 3000);
    } else {
        alert("Kérlek tölts ki minden mezőt!");
    }
}
</script>

<template>
  <div class="contact-container">
    <div class="header-section">
      <h1>Kapcsolat</h1>
      <p>Kérdése van? Időpontot foglalna? Keressen minket bizalommal!</p>
    </div>

    <div class="grid-layout">

      <div class="info-column">

        <Card style="margin-bottom: 20px;">
          <template #title>
            Elérhetőségeink
          </template>
          <template #content>
            <ul class="contact-list">
              <li>
                <i class="pi pi-map-marker icon"></i>
                <div>
                  <strong>Cím:</strong><br>
                  930 36 Horná Potôň<br>
                  Záhradnícka 593.
                </div>
              </li>
              <li>
                <i class="pi pi-phone icon"></i>
                <div>
                  <strong>Telefon:</strong><br>
                  <a href="tel:+421905105691">+421 905 105 691</a>
                </div>
              </li>
              <li>
                <i class="pi pi-envelope icon"></i>
                <div>
                  <strong>Email:</strong><br>
                  <a href="mailto:szkaniszalon@gmail.com">szkaniszalon@gmail.com</a>
                </div>
              </li>
            </ul>
          </template>
        </Card>

        <Card>
          <template #title>
            Nyitvatartás
          </template>
          <template #content>
            <div class="opening-info">
              <div style="margin-bottom: 20px;">
                <i class="pi pi-calendar-clock" style="font-size: 2rem; color: var(--p-primary-color); margin-bottom: 10px;"></i>
                <h3 style="margin: 0; color: #888;">Bejelentkezés alapján</h3>
              </div>

              <p style="line-height: 1.6; color: #888;">
                Jelenleg kizárólag előre egyeztetett időpontban fogadok vendégeket.
              </p>

              <div style="background: #f9f9f9; color: #555; padding: 15px; border-radius: 8px; border-left: 4px solid var(--p-primary-color); margin: 15px 0;">
                <strong>Főként elérhető időpontok:</strong><br>
                Hétköznap: 16:00 - 18:00
              </div>

              <p style="font-size: 0.9rem; margin-top: 20px;">
                A hirtelen felszabaduló időpontokat Facebook oldalunkon tesszük közzé:
              </p>

              <a href="https://www.facebook.com/profile.php?id=100089712034134&sk=about" target="_blank" style="text-decoration: none;">
                <Button label="Facebook oldalunk" icon="pi pi-facebook" class="p-button-outlined" style="width: 100%;" />
              </a>
            </div>
          </template>
        </Card>

      </div>

      <div class="form-column">

        <Card style="margin-bottom: 20px;">
          <template #title>
            Írjon nekünk!
          </template>
          <template #content>
            <div v-if="submitted" style="margin-bottom: 15px;">
              <Message severity="success" :closable="false">Köszönjük! Üzenetét megkaptuk.</Message>
            </div>

            <div class="form-group">
              <label>Név</label>
              <InputText v-model="name" placeholder="Az Ön neve" style="width: 100%" />
            </div>
            <div class="form-group">
              <label>Email cím</label>
              <InputText v-model="email" placeholder="pelda@email.com" style="width: 100%" />
            </div>
            <div class="form-group">
              <label>Üzenet</label>
              <Textarea v-model="messageText" rows="4" placeholder="Miben segíthetek?" style="width: 100%" />
            </div>
            <Button label="Üzenet küldése" icon="pi pi-send" @click="sendMessage" />
          </template>
        </Card>

        <div class="map-container">
          <iframe src="https://maps.google.com/maps?q=48.03571820467702,17.498286445088155&t=&z=15&ie=UTF8&iwloc=&output=embed"
                  width="100%"
                  height="300"
                  style="border:0; border-radius: 12px;"
                  allowfullscreen=""
                  loading="lazy">
          </iframe>
        </div>

      </div>
    </div>
  </div>
</template>

<style scoped>
  .contact-container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 20px;
  }

  .header-section {
    text-align: center;
    margin-bottom: 40px;
  }

    .header-section h1 {
      font-size: 2.5rem;
      color: #999;
      margin-bottom: 10px;
    }
  .opening-info {
      text-align: center;
  }
  /* Kétoszlopos elrendezés (Grid) */
  .grid-layout {
    display: grid;
    grid-template-columns: 1fr 1.5fr; /* A jobb oldal kicsit szélesebb */
    gap: 30px;
  }

  /* Mobilon egymás alá kerüljenek */
  @media (max-width: 768px) {
    .grid-layout {
      grid-template-columns: 1fr;
    }
  }

  .contact-list {
    list-style: none;
    padding: 0;
    margin: 0;
  }

    .contact-list li {
      display: flex;
      align-items: flex-start;
      margin-bottom: 20px;
      font-size: 1.1rem;
    }

    .contact-list .icon {
      font-size: 1.5rem;
      color: var(--p-primary-color);
      margin-right: 15px;
      margin-top: 3px;
    }

    .contact-list a {
      color: #888;
      text-decoration: none;
      transition: color 0.2s;
    }

      .contact-list a:hover {
        color: var(--p-primary-color);
      }

  /* Nyitvatartás táblázat stílus */
  .hours-table {
    display: flex;
    flex-direction: column;
    gap: 10px;
  }

  .hour-row {
    display: flex;
    justify-content: space-between;
    padding-bottom: 8px;
    border-bottom: 1px dashed #ddd;
  }

    .hour-row:last-child {
      border-bottom: none;
    }

    .hour-row.closed {
      color: #999;
    }

  .form-group {
    margin-bottom: 20px;
  }

    .form-group label {
      display: block;
      margin-bottom: 8px;
      font-weight: bold;
      color: #888;
    }

  .map-container {
    box-shadow: 0 4px 10px rgba(0,0,0,0.1);
    border-radius: 12px;
  }
</style>
