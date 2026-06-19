<template>
  <v-layout>
    <!-- ─── Sidebar ──────────────────────────────────────── -->
    <v-navigation-drawer
      v-model="drawer"
      :rail="rail"
      permanent
      color="#1E2937"
      class="sidebar"
      @click="rail && (rail = false)"
    >
      <!-- Logo -->
      <v-list-item
        prepend-icon="mdi-wrench-cog"
        title="SisManut"
        nav
        class="py-4"
        :subtitle="rail ? '' : 'UFS · Manutenção'"
      >
        <template #append>
          <v-btn
            :icon="rail ? 'mdi-chevron-right' : 'mdi-chevron-left'"
            variant="text"
            color="white"
            @click.stop="rail = !rail"
          />
        </template>
      </v-list-item>

      <v-divider color="rgba(255,255,255,0.12)" class="mb-2" />

      <v-list density="compact" nav>
        <v-list-item
          v-for="item in navItems"
          :key="item.to"
          :prepend-icon="item.icon"
          :title="item.title"
          :to="item.to"
          :active="route.path.startsWith(item.to)"
          active-color="primary"
          color="white"
          base-color="white"
          rounded="lg"
          class="mb-1"
          v-show="!item.roles || item.roles.includes(auth.perfil)"
        />
      </v-list>

      <!-- Logout no rodapé -->
      <template #append>
        <v-divider color="rgba(255,255,255,0.12)" />
        <v-list density="compact" nav class="py-2">
          <v-list-item
            prepend-icon="mdi-logout"
            title="Sair"
            color="white"
            base-color="white"
            rounded="lg"
            @click="logout"
          />
        </v-list>
      </template>
    </v-navigation-drawer>

    <!-- ─── Main content ─────────────────────────────────── -->
    <v-main>
      <!-- Top bar -->
      <v-app-bar elevation="0" border="b" color="surface" height="64">
        <v-app-bar-title>
          <span class="text-h6 font-weight-medium">{{ pageTitle }}</span>
        </v-app-bar-title>

        <template #append>
          <div class="d-flex align-center gap-2 mr-4">
            <v-chip
              :color="perfilColor"
              size="small"
              variant="tonal"
              class="mr-2"
            >
              {{ auth.usuario?.perfil }}
            </v-chip>
            <v-avatar color="primary" size="36">
              <span class="text-caption font-weight-bold text-white">
                {{ initials }}
              </span>
            </v-avatar>
            <div v-if="!rail">
              <div class="text-body-2 font-weight-medium">{{ auth.usuario?.nome }}</div>
              <div class="text-caption text-medium-emphasis">{{ auth.usuario?.email }}</div>
            </div>
          </div>
        </template>
      </v-app-bar>

      <!-- Conteúdo da página -->
      <v-container fluid class="pa-6">
        <RouterView />
      </v-container>
    </v-main>
  </v-layout>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route  = useRoute()
const router = useRouter()
const auth   = useAuthStore()
const drawer = ref(true)
const rail   = ref(false)

const navItems = [
  { to: '/dashboard',    icon: 'mdi-view-dashboard-outline', title: 'Dashboard' },
  { to: '/kanban',       icon: 'mdi-view-column-outline',    title: 'Kanban' },
  { to: '/ordens',       icon: 'mdi-clipboard-list-outline', title: 'Ordens de Serviço' },
  { to: '/clientes',     icon: 'mdi-account-group-outline',  title: 'Clientes',
    roles: ['Funcionario', 'Administrador', 'Tecnico'] },
  { to: '/equipamentos', icon: 'mdi-desktop-classic',        title: 'Equipamentos',
    roles: ['Funcionario', 'Administrador', 'Tecnico'] },
  { to: '/usuarios',     icon: 'mdi-account-cog-outline',    title: 'Usuários',
    roles: ['Administrador'] },
]

const pageTitles = {
  '/dashboard':    'Dashboard',
  '/kanban':       'Quadro Kanban',
  '/ordens':       'Ordens de Serviço',
  '/clientes':     'Clientes',
  '/equipamentos': 'Equipamentos',
  '/usuarios':     'Usuários'
}

const pageTitle = computed(() => {
  for (const [path, title] of Object.entries(pageTitles)) {
    if (route.path.startsWith(path)) return title
  }
  return 'Sistema de Manutenção'
})

const initials = computed(() => {
  const nome = auth.usuario?.nome || ''
  return nome.split(' ').slice(0, 2).map(n => n[0]).join('').toUpperCase()
})

const perfilColor = computed(() => ({
  Administrador: 'error',
  Tecnico:       'purple',
  Funcionario:   'primary',
  Cliente:       'success'
}[auth.perfil] || 'grey'))

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<style scoped>
.sidebar :deep(.v-list-item--active) {
  background: rgba(255, 255, 255, 0.1) !important;
}
.sidebar :deep(.v-navigation-drawer__content) {
  display: flex;
  flex-direction: column;
}
</style>
