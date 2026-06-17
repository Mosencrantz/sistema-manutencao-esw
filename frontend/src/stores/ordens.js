import { defineStore } from 'pinia'
import { ordensApi } from '@/services/api'

export const STATUS_LABELS = {
  AguardandoEquipamento: 'Aguardando Equipamento',
  EquipamentoRecebido:   'Equipamento Recebido',
  EmAnalise:             'Em Análise',
  AguardandoAprovacao:   'Aguardando Aprovação',
  EmManutencao:          'Em Manutenção',
  AguardandoPecas:       'Aguardando Peças',
  Finalizado:            'Finalizado',
  EntregueAoCliente:     'Entregue ao Cliente'
}

export const STATUS_COLORS = {
  AguardandoEquipamento: 'blue-grey',
  EquipamentoRecebido:   'indigo',
  EmAnalise:             'blue',
  AguardandoAprovacao:   'orange',
  EmManutencao:          'purple',
  AguardandoPecas:       'amber',
  Finalizado:            'green',
  EntregueAoCliente:     'teal'
}

export const STATUS_SEQUENCE = [
  'AguardandoEquipamento',
  'EquipamentoRecebido',
  'EmAnalise',
  'AguardandoAprovacao',
  'EmManutencao',
  'AguardandoPecas',
  'Finalizado',
  'EntregueAoCliente'
]

export const useOrdensStore = defineStore('ordens', {
  state: () => ({
    ordens: [],
    ordemAtual: null,
    historico: [],
    loading: false,
    error: null
  }),

  getters: {
    ordensPorStatus: s => status =>
      s.ordens.filter(o => o.status === status),

    kanbanColunas: s => STATUS_SEQUENCE.map(status => ({
      status,
      label: STATUS_LABELS[status],
      color: STATUS_COLORS[status],
      ordens: s.ordens.filter(o => o.status === status)
    }))
  },

  actions: {
    async fetchAll() {
      this.loading = true
      try {
        const { data } = await ordensApi.getAll()
        this.ordens = data
      } catch (e) {
        this.error = e.message
      } finally {
        this.loading = false
      }
    },

    async fetchById(id) {
      this.loading = true
      try {
        const { data } = await ordensApi.getById(id)
        this.ordemAtual = data
        return data
      } catch (e) {
        this.error = e.message
      } finally {
        this.loading = false
      }
    },

    async create(dto) {
      const { data } = await ordensApi.create(dto)
      this.ordens.push(data)
      return data
    },

    async atualizarStatus(id, novoStatus, observacao) {
      const { data } = await ordensApi.atualizarStatus(id, {
        novoStatus,
        observacao
      })
      const idx = this.ordens.findIndex(o => o.id === id)
      if (idx !== -1) this.ordens[idx] = data
      if (this.ordemAtual?.id === id) this.ordemAtual = data
      return data
    },

    async adicionarPeca(id, peca) {
      const { data } = await ordensApi.adicionarPeca(id, peca)
      const idx = this.ordens.findIndex(o => o.id === id)
      if (idx !== -1) this.ordens[idx] = data
      return data
    },

    async atribuirTecnico(id, tecnicoId) {
      const { data } = await ordensApi.atribuirTecnico(id, tecnicoId)
      const idx = this.ordens.findIndex(o => o.id === id)
      if (idx !== -1) this.ordens[idx] = data
      return data
    },

    async fetchHistorico(id) {
      const { data } = await ordensApi.getHistorico(id)
      this.historico = data
      return data
    }
  }
})
