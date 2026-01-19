<script setup>
import { ref } from "vue";
import Image from 'primevue/image';

// Ideiglenes képek listája (ezeket később kicseréljük a sajátjaidra)
const photos = ref([
    {
        src: 'https://images.unsplash.com/photo-1562322140-8baeececf3df?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60',
        alt: 'Hajvágás folyamatban'
    },
    {
        src: 'https://images.unsplash.com/photo-1595476108010-b4d1f102b1b1?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60',
        alt: 'Modern szalon belső'
    },
    {
        src: 'https://images.unsplash.com/photo-1522337660859-02fbefca4702?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60',
        alt: 'Festés és styling'
    },
    {
        src: 'https://images.unsplash.com/photo-1605497788044-5a32c7078486?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60',
        alt: 'Elegáns frizura'
    },
    {
        src: 'https://images.unsplash.com/photo-1582095133179-bfd08d2fc6b2?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60',
        alt: 'Fodrász eszközök'
    },
    {
        src: 'https://images.unsplash.com/photo-1519699047748-40ba5266f226?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60',
        alt: 'Hosszú haj hátulról'
    }
]);
</script>

<template>
  <div class="gallery-container">
    <h1 class="gallery-title">Munkáink</h1>
    <p class="gallery-subtitle">Betekintés a mindennapjainkba és legszebb alkotásainkba.</p>

    <div class="photo-grid">
      <div v-for="(photo, index) in photos" :key="index" class="photo-item">
        <Image :src="photo.src" :alt="photo.alt" preview width="100%" />
      </div>
    </div>
  </div>
</template>

<style scoped>
  .gallery-container {
    max-width: 1200px; /* Kicsit szélesebb, hogy jobban elférjenek */
    margin: 0 auto;
    padding: 20px;
    text-align: center;
  }

  .gallery-title {
    font-size: 2.5rem;
    color: #333;
    margin-bottom: 10px;
  }

  .gallery-subtitle {
    color: #666;
    margin-bottom: 40px;
  }

  /* RÁCS ELRENDEZÉS */
  .photo-grid {
    display: grid;
    /* Reszponzív: Minimum 300px széles legyen egy kép, de ha van hely, nyúljon */
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 24px; /* Nagyobb térköz */
  }

  .photo-item {
    border-radius: 12px;
    overflow: hidden; /* Ez vágja le a kép sarkait */
    box-shadow: 0 8px 15px rgba(0,0,0,0.1); /* Erősebb árnyék */
    transition: transform 0.3s ease, box-shadow 0.3s ease;
    background: white; /* Ha a kép kisebb, legyen fehér háttere */
  }

    .photo-item:hover {
      transform: translateY(-5px);
      box-shadow: 0 12px 20px rgba(0,0,0,0.15);
    }

  /* --- A TRÜKKÖS RÉSZ: PRIME VUE IMAGE JAVÍTÁS --- */

  /* A PrimeVue "span" keretét kényszerítjük, hogy töltse ki a dobozt */
  :deep(.p-image) {
    width: 100%;
    height: 100%;
    display: block; /* Fontos, hogy ne inline elem legyen */
  }

  /* Magát a képet is formázzuk */
  :deep(.p-image img) {
    width: 100%;
    height: 250px; /* Fix magasság, hogy minden kép egyforma magas legyen! */
    object-fit: cover; /* Ha a kép aránya más, levágja a szélét, de nem torzít */
    display: block;
  }

  /* A "preview" (szem) ikon stílusa, ha esetleg nem látszana jól */
  :deep(.p-image-preview-indicator) {
    background-color: rgba(0, 0, 0, 0.5); /* Sötétebb áttetsző réteg */
  }
</style>
