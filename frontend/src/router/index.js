import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

// AUTENTICAÇÃO RESTAURADA: tela de login volta a ser a porta de entrada do sistema
const routes = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { public: true }
  },
  {
    path: '/consulta',
    name: 'consulta',
    component: () => import('@/views/ConsultaPublicaView.vue'),
    meta: { public: true } // portal de consulta não exige login
  },
  {
    path: '/',
    component: () => import('@/components/layout/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: '/dashboard' },
      {
        path: 'dashboard',
        name: 'dashboard',
        component: () => import('@/views/DashboardView.vue')
      },
      {
        path: 'clientes',
        name: 'clientes',
        component: () => import('@/views/ClientesView.vue'),
        meta: { roles: ['Funcionario', 'Administrador', 'Tecnico'] }
      },
      {
        path: 'equipamentos',
        name: 'equipamentos',
        component: () => import('@/views/EquipamentosView.vue'),
        meta: { roles: ['Funcionario', 'Administrador', 'Tecnico'] }
      },
      {
        path: 'ordens',
        name: 'ordens',
        component: () => import('@/views/OrdensServicoView.vue')
      },
      {
        path: 'kanban',
        name: 'kanban',
        component: () => import('@/views/KanbanView.vue')
      },
      {
        path: 'usuarios',
        name: 'usuarios',
        component: () => import('@/views/UsuariosView.vue'),
        meta: { roles: ['Administrador'] }
      }
    ]
  },
  { path: '/:pathMatch(.*)*', redirect: '/dashboard' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// ─── Navigation guard ─────────────────────────────────────────────────────────
router.beforeEach((to, from, next) => {
  const auth = useAuthStore()

  if (to.meta.public) {
    // Se já estiver logado e tentar acessar /login, manda direto pro dashboard
    if (to.name === 'login' && auth.isAuthenticated) return next('/dashboard')
    return next()
  }

  if (!auth.isAuthenticated) return next('/login')

  if (to.meta.roles && !to.meta.roles.includes(auth.perfil)) {
    return next('/dashboard')
  }

  next()
})

export default router
