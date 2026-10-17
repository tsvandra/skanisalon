import { defineStore } from 'pinia';
import appointmentApi from '../services/appointmentApi';
import { jwtDecode } from 'jwt-decode';

export const useAppointmentStore = defineStore('appointment', {
  state: () => ({
    appointments: [],
    pendingAppointments: [],
    isLoading: false,
    error: null,
    userRole: 'Worker', // Alapértelmezett biztonsági okokból
    currentEmployeeId: null, // Egyelőre a belépett user
  }),

  getters: {
    canForceOverlap: (state) => ['Owner', 'Manager'].includes(state.userRole),
    pendingCount: (state) => state.pendingAppointments.length,
  },

  actions: {
    initUserPermissions() {
      const token = localStorage.getItem('auth_token') || localStorage.getItem('salon_token') || localStorage.getItem('token');
      if (token) {
        try {
          const decoded = jwtDecode(token);
          // A ClaimTypes.Role alapján:
          this.userRole = decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'Worker';
        } catch (e) {
          console.error('Hibás token formátum', e);
        }
      }
    },

    async fetchAppointments(start, end) {
      this.isLoading = true;
      this.error = null;
      try {
        const response = await appointmentApi.getAppointments(start, end);
        this.appointments = response.data;
        // Frissítjük a függőben lévő online foglalások listáját is
        this.fetchPendingAppointments();
      } catch (err) {
        this.error = err.response?.data || 'Hiba a betöltéskor';
      } finally {
        this.isLoading = false;
      }
    },

    async fetchPendingAppointments() {
      try {
        const response = await appointmentApi.getPendingAppointments();
        this.pendingAppointments = response.data || [];
      } catch (err) {
        console.error('Hiba a függőben lévő foglalások betöltésekor:', err);
      }
    },

    async updateAppointmentStatus(id, status, statusReason = null) {
      try {
        await appointmentApi.updateAppointmentStatus(id, { status, statusReason });
        await this.fetchPendingAppointments();
        return true;
      } catch (err) {
        this.error = err.response?.data || 'Hiba a státusz módosításakor';
        throw err;
      }
    },

    async saveAppointment(data, id = null) {
      this.isLoading = true;
      this.error = null;
      try {
        if (id) {
          await appointmentApi.updateAppointment(id, data);
        } else {
          await appointmentApi.createAppointment(data);
        }
        await this.fetchPendingAppointments();
        return true; // Sikeres mentés
      } catch (err) {
        this.error = err.response?.data || 'Hiba mentéskor';
        throw err; // Továbbdobjuk a komponensnek a hibaüzenetet
      } finally {
        this.isLoading = false;
      }
    },

    // ÚJ: Törlés akció hozzáadva
    async deleteAppointment(id) {
      this.isLoading = true;
      this.error = null;
      try {
        await appointmentApi.deleteAppointment(id);

        // Opcionálisan kivehetjük a lokális state-ből is, bár a CalendarGrid 
        // úgyis újrahívja a fetchAppointments()-t a törlés után.
        this.appointments = this.appointments.filter(app => app.id !== id);
        this.pendingAppointments = this.pendingAppointments.filter(app => app.id !== id);

        return true;
      } catch (err) {
        this.error = err.response?.data || 'Hiba törléskor';
        throw err; // Továbbdobjuk, hogy a UI tudjon hibaüzenetet adni
      } finally {
        this.isLoading = false;
      }
    }
  }
});
