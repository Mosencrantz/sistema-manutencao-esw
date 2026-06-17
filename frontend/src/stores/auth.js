import { defineStore } from 'pinia'

// MVP: autenticação desativada — admin logado por padrão
export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: 'mvp-token',
    usuario: {
      id: 'admin',
      nome: 'Administrador',
      email: 'admin@sistema.com',
      perfil: 'Administrador'
    },
    loading: false,
    error: null
  }),

  getters: {
    isAuthenticated: () => true,
    perfil:          () => 'Administrador',
    isAdmin:         () => true,
    isTecnico:       () => true,
    isFuncionario:   () => true
  },

  actions: {
    async login() { return true },
    logout()      {}
  }
})
