import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import Components from 'unplugin-vue-components/vite'
import { AntDesignVueResolver } from 'unplugin-vue-components/resolvers'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const proxyTarget = env.VITE_DEV_API_PROXY_TARGET || 'http://localhost:8080'

  return {
    build: {
      // ApexCharts được tách thành chunk tải lười; kích thước gzip thực tế khoảng 230 KB.
      chunkSizeWarningLimit: 850
    },
    plugins: [
      vue(),
      Components({
        resolvers: [AntDesignVueResolver({ importStyle: false })]
      })
    ],
    server: {
      port: 5173,
      strictPort: true,
      proxy: {
        '/api': {
          target: proxyTarget,
          changeOrigin: true,
          timeout: 15000,
          proxyTimeout: 15000
        },
        '/notificationHub': {
          target: proxyTarget,
          ws: true,
          changeOrigin: true,
          timeout: 15000,
          proxyTimeout: 15000
        }
      }
    }
  }
})
