<script setup>
  import { ref, onMounted } from "vue";
  import Image from 'primevue/image';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import InputText from 'primevue/inputtext';
  // A FileUpload komponenst kivettük, mert lassú volt
  // import FileUpload from 'primevue/fileupload';

  const categories = ref([]);
  const selectedCategory = ref(null);
  const images = ref([]);
  const loading = ref(false);

  // Feltöltés ablak változói
  const uploadDialogVisible = ref(false);
  const newImageTitle = ref("");
  const selectedFile = ref(null);
  const uploadStatus = ref("");
  const fileInput = ref(null); // Ez a hivatkozás a rejtett fájlválasztóra

  const API_URL = "https://localhost:7113";

  // --- 1. KATEGÓRIÁK ---
  onMounted(async () => {
    await loadCategories();
  });

  const loadCategories = async () => {
    try {
      const response = await fetch(`${API_URL}/api/Gallery/categories?companyId=1`);
      if (response.ok) {
        categories.value = await response.json();
        if (categories.value.length > 0) {
          selectedCategory.value = categories.value[0];
          loadImages(categories.value[0].id);
        }
      }
    } catch (error) {
      console.error("Hiba a kategóriák betöltésekor:", error);
    }
  };

  // --- 2. KÉPEK ---
  const loadImages = async (categoryId) => {
    if (!categoryId) return;
    loading.value = true;
    images.value = [];
    try {
      const response = await fetch(`${API_URL}/api/Gallery/images?categoryId=${categoryId}`);
      if (response.ok) {
        images.value = await response.json();
      }
    } catch (error) {
      console.error("Hiba a képek betöltésekor:", error);
    } finally {
      loading.value = false;
    }
  };

  const onCategoryChange = (category) => {
    selectedCategory.value = category;
    loadImages(category.id);
  };

  // --- 3. KÉP VÁLASZTÁS ÉS FELTÖLTÉS ---

  // Ez a függvény nyitja meg a rejtett fájlválasztót
  const triggerFileInput = () => {
    fileInput.value.click();
  };

  // Ez fut le, amikor a felhasználó kiválasztotta a fájlt a Windows ablakban
  const onFileChange = (event) => {
    const file = event.target.files[0];
    if (file) {
      selectedFile.value = file;
      uploadStatus.value = "";
    }
  };

  const uploadImage = async () => {
    if (!selectedFile.value) {
      uploadStatus.value = "Kérlek válassz ki egy képet!";
      return;
    }
    if (!selectedCategory.value) {
      uploadStatus.value = "Nincs kiválasztva kategória!";
      return;
    }

    // Ha nincs cím, a fájlnév lesz (kiterjesztés nélkül)
    let titleToSend = newImageTitle.value;
    if (!titleToSend || titleToSend.trim() === "") {
      titleToSend = selectedFile.value.name.split('.')[0];
    }

    const formData = new FormData();
    formData.append("file", selectedFile.value);
    formData.append("title", titleToSend);
    formData.append("categoryId", selectedCategory.value.id);

    try {
      const response = await fetch(`${API_URL}/api/Gallery/upload`, {
        method: "POST",
        body: formData
      });

      if (response.ok) {
        uploadDialogVisible.value = false;

        // Tiszta lappal indulunk legközelebb
        newImageTitle.value = "";
        selectedFile.value = null;
        uploadStatus.value = "";
        if (fileInput.value) fileInput.value.value = ""; // Input ürítése

        loadImages(selectedCategory.value.id);
      } else {
        uploadStatus.value = "Hiba történt a feltöltéskor.";
      }
    } catch (error) {
      console.error("Feltöltési hiba:", error);
      uploadStatus.value = "Szerver hiba.";
    }
  };

  const deleteImage = async (id) => {
    if (!confirm("Biztosan törölni szeretnéd ezt a képet?")) return;

    try {
      const response = await fetch(`${API_URL}/api/Gallery/images/${id}`, {
        method: "DELETE"
      });

      if (response.ok) {
        images.value = images.value.filter(img => img.id !== id);
      }
    } catch (error) {
      console.error("Törlési hiba:", error);
    }
  };

  const getImageUrl = (imagePath) => {
    return `${API_URL}${imagePath}`;
  }
</script>

<template>
  <div class="gallery-container">
    <div class="header-actions">
      <h1>Galéria</h1>
      <Button label="Új Kép Feltöltése" icon="pi pi-plus" @click="uploadDialogVisible = true" />
    </div>

    <div class="category-tabs" v-if="categories.length > 0">
      <Button v-for="cat in categories"
              :key="cat.id"
              :label="cat.name"
              :class="{'p-button-outlined': selectedCategory?.id !== cat.id}"
              class="category-btn"
              @click="onCategoryChange(cat)" />
    </div>
    <div v-else class="no-data">
      <p>Még nincsenek kategóriák.</p>
    </div>

    <hr class="divider" />

    <div v-if="loading">Betöltés...</div>

    <div class="photo-grid" v-else>
      <div v-for="img in images" :key="img.id" class="photo-item-wrapper">
        <div class="photo-item">
          <Image :src="getImageUrl(img.imagePath)" :alt="img.title" preview />
        </div>
        <div class="photo-info">
          <span>{{ img.title }}</span>
          <Button icon="pi pi-trash" class="p-button-danger p-button-text p-button-sm" @click="deleteImage(img.id)" />
        </div>
      </div>
    </div>

    <div v-if="!loading && images.length === 0 && selectedCategory" class="no-data">
      <p>Ebben a kategóriában még nincsenek képek.</p>
    </div>

    <Dialog v-model:visible="uploadDialogVisible" modal header="Új kép feltöltése" :style="{ width: '450px' }">
      <div class="upload-form">

        <div style="background-color: #f0f8ff; color: #333; padding: 10px; border-radius: 5px; margin-bottom: 15px; border-left: 4px solid #007ad9;">
          <i class="pi pi-folder-open" style="margin-right: 5px;"></i>
          Cél mappa: <strong>{{ selectedCategory ? selectedCategory.name : 'Nincs kiválasztva' }}</strong>
        </div>

        <label style="display:block; margin-bottom: 5px;">Kép címe (Opcionális)</label>
        <InputText v-model="newImageTitle" placeholder="Kép címe" style="width: 100%; margin-bottom: 20px;" />

        <label style="display:block; margin-bottom: 5px;">Kép kiválasztása</label>

        <input type="file"
               ref="fileInput"
               @change="onFileChange"
               accept="image/*"
               style="display: none;" />

        <Button label="Kép tallózása..."
                icon="pi pi-images"
                class="p-button-secondary p-button-outlined"
                style="width: 100%"
                @click="triggerFileInput" />

        <div v-if="selectedFile" style="margin-top: 15px; color: #007ad9; font-weight: bold; text-align: center;">
          <i class="pi pi-file"></i> {{ selectedFile.name }}
        </div>

        <div v-if="uploadStatus" style="color: red; margin-top: 10px;">{{ uploadStatus }}</div>
      </div>

      <template #footer>
        <Button label="Mégse" icon="pi pi-times" @click="uploadDialogVisible = false" class="p-button-text" />
        <Button label="Feltöltés" icon="pi pi-check" @click="uploadImage" :disabled="!selectedFile" autofocus />
      </template>
    </Dialog>

  </div>
</template>

<style scoped>
  .gallery-container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 20px;
  }

  .header-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
  }

  .category-tabs {
    display: flex;
    gap: 10px;
    flex-wrap: wrap;
    margin-bottom: 20px;
  }

  .category-btn {
    border-radius: 20px;
  }

  .photo-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
    gap: 20px;
  }

  .photo-item-wrapper {
    background: white;
    border-radius: 12px;
    box-shadow: 0 4px 10px rgba(0,0,0,0.1);
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }

  .photo-item {
    height: 200px;
    overflow: hidden;
  }

  :deep(.p-image), :deep(.p-image img) {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
  }

  .photo-info {
    padding: 10px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 0.9rem;
    font-weight: bold;
    color: #555;
  }

  .no-data {
    text-align: center;
    color: #999;
    padding: 40px;
  }

  .divider {
    border: 0;
    height: 1px;
    background: #eee;
    margin-bottom: 30px;
  }
</style>
