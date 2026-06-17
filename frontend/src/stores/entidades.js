import { defineStore } from 'pinia'
import { usuariosApi, equipamentosApi } from '@/services/api'

// ─── Clientes ─────────────────────────────────────────────────────────────────
export const useClientesStore = defineStore('clientes', {
  state: () => ({
    clientes: [],
    loading: false,
    error: null
  }),

  getters: {
    soClientes: s => s.clientes.filter(u => u.perfil === 'Cliente')
  },

  actions: {
    async fetchAll() {
      this.loading = true
      try {
        const { data } = await usuariosApi.getAll()
        this.clientes = data
      } catch (e) {
        this.error = e.message
      } finally {
        this.loading = false
      }
    },

    async create(dto) {
      const { data } = await usuariosApi.create({ ...dto, perfil: 'Cliente' })
      this.clientes.push(data)
      return data
    },

    async update(id, dto) {
      const { data } = await usuariosApi.update(id, dto)
      const idx = this.clientes.findIndex(c => c.id === id)
      if (idx !== -1) this.clientes[idx] = data
      return data
    },

    async delete(id) {
      await usuariosApi.delete(id)
      // CORREÇÃO: remove do array visualmente em vez de só marcar inativo
      const idx = this.clientes.findIndex(c => c.id === id)
      if (idx !== -1) this.clientes.splice(idx, 1)
    }
  }
})

// ─── Equipamentos ─────────────────────────────────────────────────────────────
export const useEquipamentosStore = defineStore('equipamentos', {
  state: () => ({
    equipamentos: [],
    loading: false,
    error: null
  }),

  actions: {
    async fetchAll() {
      this.loading = true
      try {
        const { data } = await equipamentosApi.getAll()
        this.equipamentos = data
      } catch (e) {
        this.error = e.message
      } finally {
        this.loading = false
      }
    },

    async fetchByCliente(clienteId) {
      const { data } = await equipamentosApi.getByCliente(clienteId)
      return Array.isArray(data) ? data : []
    },

    async create(dto) {
      const { data } = await equipamentosApi.create(dto)
      this.equipamentos.push(data)
      return data
    },

    async update(id, dto) {
      const { data } = await equipamentosApi.update(id, dto)
      const idx = this.equipamentos.findIndex(e => e.id === id)
      if (idx !== -1) this.equipamentos[idx] = data
      return data
    },

    async delete(id) {
      await equipamentosApi.delete(id)
      // CORREÇÃO: remove do array visualmente
      const idx = this.equipamentos.findIndex(e => e.id === id)
      if (idx !== -1) this.equipamentos.splice(idx, 1)
    }
  }
})

// ─── Usuários (geral) ─────────────────────────────────────────────────────────
export const useUsuariosStore = defineStore('usuarios', {
  state: () => ({
    usuarios: [],
    loading: false,
    error: null
  }),

  getters: {
    tecnicos:     s => s.usuarios.filter(u => u.perfil === 'Tecnico'),
    funcionarios: s => s.usuarios.filter(u => u.perfil === 'Funcionario')
  },

  actions: {
    async fetchAll() {
      this.loading = true
      try {
        const { data } = await usuariosApi.getAll()
        this.usuarios = data
      } catch (e) {
        this.error = e.message
      } finally {
        this.loading = false
      }
    },

    async create(dto) {
      const { data } = await usuariosApi.create(dto)
      this.usuarios.push(data)
      return data
    },

    async update(id, dto) {
      const { data } = await usuariosApi.update(id, dto)
      const idx = this.usuarios.findIndex(u => u.id === id)
      if (idx !== -1) this.usuarios[idx] = data
      return data
    },

    async delete(id) {
      await usuariosApi.delete(id)
      // CORREÇÃO: remove do array visualmente
      const idx = this.usuarios.findIndex(u => u.id === id)
      if (idx !== -1) this.usuarios.splice(idx, 1)
    }
  }
})
