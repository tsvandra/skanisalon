import { defineStore } from 'pinia';
import appointmentApi from '../services/appointmentApi';
import { jwtDecode } from 'jwt-decode';

export const useAppointmentStore = defineStore('appointment', {
  state: () => ({
    appointments: [],
    isLoading: false,
    error: null,
    userRole: 'Worker', // Alapértelmezett biztonsági okokból
    currentEmployeeId: null, // Egyelőre a belépett user
  }),

  getters: {
    canForceOverlap: (state) => ['Owner', 'Manager'].includes(state.userRole),
  },

  actions: {
    initUserPermissions() {
      const token = localStorage.getItem('token');
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
      } catch (err) {
        this.error = err.response?.data || 'Hiba a betöltéskor';
      } finally {
        this.isLoading = false;
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
