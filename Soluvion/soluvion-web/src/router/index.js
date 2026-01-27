import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import ServicesView from '../views/ServicesView.vue'
import GalleryView from '../views/GalleryView.vue'
import ContactView from '../views/ContactView.vue'
import LoginView from '../views/LoginView.vue'
import SettingsView from '../views/SettingsView.vue'


const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/szolgaltatasok', name: 'services', component: ServicesView },
    { path: '/galeria', name: 'gallery', component: GalleryView },
    { path: '/kapcsolat', name: 'contact', component: ContactView },
    { path: '/login', name: 'login', component: LoginView },
    { path: '/beallitasok', name: 'settings', component: SettingsView }
  ],
})

export default router
