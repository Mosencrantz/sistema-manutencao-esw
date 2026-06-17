import { defineStore } from 'pinia'
import { authApi } from '@/services/api'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('token'),
    usuario: JSON.parse(localStorage.getItem('usuario') || 'null'),
    loading: false,
    error: null
  }),

  getters: {
    isAuthenticated: state => !!state.token,
    perfil: state => state.usuario?.perfil,
    isAdmin: state => state.usuario?.perfil === 'Administrador',
    isTecnico: state => state.usuario?.perfil === 'Tecnico',
    isFuncionario: state => state.usuario?.perfil === 'Funcionario'
  },

  actions: {
    async login(email, senha) {
      this.loading = true
      this.error = null

      try {
        const { data } = await authApi.login(email, senha)
        console.log("LOGIN RESPONSE:", data)

        this.token = data.token

        this.usuario = {
          id: data.id,
          nome: data.nome,
          email: data.email,
          perfil: data.perfil
        }

        localStorage.setItem('token', data.token)
        localStorage.setItem('usuario', JSON.stringify(this.usuario))

        return true
      }
      catch (err) {
        this.error = err.message
        return false
      }
      finally {
        this.loading = false
      }
    },

    logout() {
      this.token = null
      this.usuario = null

      localStorage.removeItem('token')
      localStorage.removeItem('usuario')
    }
  }
})