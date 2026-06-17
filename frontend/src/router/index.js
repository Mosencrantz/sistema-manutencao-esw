import { createRouter, createWebHistory } from 'vue-router'

// MVP: sem guards de autenticação — todas as rotas abertas
const routes = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue')
  },
  {
    path: '/consulta',
    name: 'consulta',
    component: () => import('@/views/ConsultaPublicaView.vue')
  },
  {
    path: '/',
    component: () => import('@/components/layout/AppLayout.vue'),
    children: [
      { path: '', redirect: '/login' },
      { path: 'dashboard', name: 'dashboard',    component: () => import('@/views/DashboardView.vue') },
      { path: 'clientes',  name: 'clientes',     component: () => import('@/views/ClientesView.vue') },
      { path: 'equipamentos', name: 'equipamentos', component: () => import('@/views/EquipamentosView.vue') },
      { path: 'ordens',    name: 'ordens',        component: () => import('@/views/OrdensServicoView.vue') },
      { path: 'kanban',    name: 'kanban',        component: () => import('@/views/KanbanView.vue') },
      { path: 'usuarios',  name: 'usuarios',      component: () => import('@/views/UsuariosView.vue') }
    ]
  },
  { path: '/:pathMatch(.*)*', redirect: '/login' }
]

export default createRouter({
  history: createWebHistory(),
  routes
})
