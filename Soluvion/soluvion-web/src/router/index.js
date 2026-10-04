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
    { path: '/foglalas', name: 'booking', component: () => import('../views/BookingView.vue') },
    { path: '/login', name: 'login', component: LoginView },
    { path: '/beallitasok', name: 'settings', component: SettingsView },
    { path: '/vezerlopult', name: 'dashboard', component: () => import('../views/DashboardView.vue') },
    { path: '/megrendelesek', name: 'orders', component: () => import('../components/admin/calendar/CalendarGrid.vue') },
    { path: '/ugyfelek', name: 'customers', component: () => import('../views/CustomersView.vue') },
    { 
      path: '/raktar', 
      name: 'inventory', 
      component: () => import('../views/Inventory/InventoryView.vue'),
      beforeEnter: (to, from, next) => {
        const token = localStorage.getItem('salon_token');
        if (token) {
          next();
        } else {
          next('/login');
        }
      }
    }
  ],
})

export default router
