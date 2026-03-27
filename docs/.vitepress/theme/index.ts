import DefaultTheme from 'vitepress/theme'
import './custom.css'
import HomeContent from './HomeContent.vue'
import CustomLayout from './CustomLayout.vue'

export default {
  extends: DefaultTheme,
  Layout: CustomLayout,
  enhanceApp({ app }: { app: any }) {
    app.component('HomeContent', HomeContent)
  },
}
