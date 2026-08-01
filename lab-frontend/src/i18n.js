import { createI18n } from 'vue-i18n'
import vi from './locales/vi'
import en from './locales/en'
import zh from './locales/zh'
import ja from './locales/ja'
import ko from './locales/ko'
import de from './locales/de'

const i18n = createI18n({
  legacy: false, // Sử dụng Composition API
  locale: 'vi', // Ngôn ngữ mặc định
  fallbackLocale: 'en',
  messages: {
    vi,
    en,
    zh,
    ja,
    ko,
    de
  }
})

export default i18n
