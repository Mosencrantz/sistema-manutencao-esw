import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import { createVuetify } from 'vuetify'
import { pt } from 'vuetify/locale'   // ← locale português

const manutencaoTheme = {
  dark: false,
  colors: {
    primary:      '#1A56DB',
    secondary:    '#3F83F8',
    accent:       '#F05252',
    success:      '#0E9F6E',
    warning:      '#C27803',
    error:        '#E02424',
    background:   '#F9FAFB',
    surface:      '#FFFFFF',
    'on-primary': '#FFFFFF',
    'sidebar-bg': '#1E2937',
  }
}

export default createVuetify({
  // ← tradução de toda a UI para português
  locale: {
    locale: 'pt',
    messages: { pt }
  },
  theme: {
    defaultTheme: 'manutencaoTheme',
    themes: { manutencaoTheme }
  },
  defaults: {
    VBtn: {
      variant: 'flat',
      rounded: 'lg'
    },
    VCard: {
      rounded: 'lg',
      elevation: 1
    },
    VTextField: {
      variant: 'outlined',
      density: 'comfortable',
      rounded: 'lg'
    },
    VSelect: {
      variant: 'outlined',
      density: 'comfortable',
      rounded: 'lg'
    },
    VTextarea: {
      variant: 'outlined',
      density: 'comfortable',
      rounded: 'lg'
    }
  }
})
