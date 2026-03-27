import DefaultTheme from 'vitepress/theme'
import './custom.css'
import HomeContent from './HomeContent.vue'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }: { app: any }) {
    app.component('HomeContent', HomeContent)
  },
}
