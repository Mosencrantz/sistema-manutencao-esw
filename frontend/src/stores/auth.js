import { defineStore } from 'pinia'
import { authApi } from '@/services/api'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('token') || null,
    usuario: JSON.parse(localStorage.getItem('usuario') || 'null'),
    loading: false,
    error: null
  }),

  getters: {
    isAuthenticated: s => !!s.token,
    perfil:          s => s.usuario?.perfil || '',
    isAdmin:         s => s.usuario?.perfil === 'Administrador',
    isTecnico:       s => ['Tecnico', 'Administrador'].includes(s.usuario?.perfil),
    isFuncionario:   s => ['Funcionario', 'Administrador'].includes(s.usuario?.perfil)
  },

  actions: {
    async login(email, senha) {
      this.loading = true
      this.error = null
      try {
        const { data } = await authApi.login(email, senha)

        // DIAGNÓSTICO: mostra no console exatamente o que veio do servidor
        console.log('[LOGIN] resposta recebida:', data)

        // CORREÇÃO CRÍTICA: antes, se data.token viesse undefined (ex: por
        // causa de "Token" vs "token" no JSON), o sistema salvava a string
        // "undefined" como se fosse um token válido e seguia para o dashboard
        // normalmente — só falhando 1 segundo depois, sem explicação, na
        // primeira chamada autenticada. Agora isso é detectado IMEDIATAMENTE
        // no momento do login, com uma mensagem de erro clara na tela.
        if (!data?.token || typeof data.token !== 'string' || !data.token.includes('.')) {
          console.error(
            '[LOGIN] O servidor respondeu, mas sem um token JWT válido. Valor recebido:',
            data?.token
          )
          this.error =
            'O servidor não retornou um token de acesso válido. ' +
            'Isso geralmente indica um problema de configuração no backend — ' +
            'veja o console (F12) para detalhes técnicos.'
          return false
        }

        this.token = data.token
        this.usuario = {
          id: data.userId,
          nome: data.nome,
          email: data.email,
          perfil: data.perfil,
          expiracao: data.expiracao
        }
        localStorage.setItem('token', data.token)
        localStorage.setItem('usuario', JSON.stringify(this.usuario))
        return true
      } catch (e) {
        this.error = e.message
        return false
      } finally {
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
