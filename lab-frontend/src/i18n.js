import { createI18n } from 'vue-i18n'
import vi from './locales/vi'

const i18n = createI18n({
  legacy: false, // Sử dụng Composition API
  locale: 'vi', // Ngôn ngữ mặc định
  fallbackLocale: 'vi',
  messages: {
    vi
  }
})

export default i18n
