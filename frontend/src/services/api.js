import axios from 'axios'

// MVP: sem interceptors de autenticação
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
  timeout: 15000
})

api.interceptors.response.use(
  response => response,
  error => {
    const message =
      error.response?.data?.erro ||
      error.response?.data?.title ||
      error.message ||
      'Erro desconhecido'
    console.error('[API Error]', error.config?.url, '→', message)
    return Promise.reject(new Error(message))
  }
)

export default api

export const authApi = {
  login: (email, senha) => api.post('/auth/login', { email, senha }),
  me:    () => api.get('/auth/me')
}

export const usuariosApi = {
  getAll:       ()        => api.get('/usuarios'),
  getById:      id        => api.get(`/usuarios/${id}`),
  create:       dto       => api.post('/usuarios', dto),
  update:       (id, dto) => api.put(`/usuarios/${id}`, dto),
  delete:       id        => api.delete(`/usuarios/${id}`),
  alterarSenha: (id, dto) => api.patch(`/usuarios/${id}/senha`, dto)
}

export const equipamentosApi = {
  getAll:      ()           => api.get('/equipamentos'),
  getById:     id           => api.get(`/equipamentos/${id}`),
  getByCliente: clienteId   => api.get(`/equipamentos/cliente/${clienteId}`),
  create:      dto          => api.post('/equipamentos', dto),
  update:      (id, dto)    => api.put(`/equipamentos/${id}`, dto),
  delete:      id           => api.delete(`/equipamentos/${id}`)
}

export const ordensApi = {
  getAll:          ()           => api.get('/ordens'),
  getById:         id           => api.get(`/ordens/${id}`),
  getByNumero:     numero       => api.get(`/ordens/consulta/${numero}`),
  getByCliente:    clienteId    => api.get(`/ordens/cliente/${clienteId}`),
  getByStatus:     status       => api.get(`/ordens/status/${status}`),
  create:          dto          => api.post('/ordens', dto),
  atualizarStatus: (id, dto)    => api.patch(`/ordens/${id}/status`, dto),
  adicionarPeca:   (id, peca)   => api.post(`/ordens/${id}/pecas`, peca),
  atribuirTecnico: (id, tecnicoId) => api.patch(`/ordens/${id}/tecnico`, { tecnicoId }),
  getHistorico:    id           => api.get(`/ordens/${id}/historico`)
}

export const diagnosticosApi = {
  getByOrdem: osId => api.get(`/diagnosticos/ordem/${osId}`),
  create:     dto  => api.post('/diagnosticos', dto)
}

export const arquivosApi = {
  getByOrdem: osId => api.get(`/arquivos/ordem/${osId}`),
  upload: (osId, file) => {
    const form = new FormData()
    form.append('file', file)
    return api.post(`/arquivos/ordem/${osId}/upload`, form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  }
}
