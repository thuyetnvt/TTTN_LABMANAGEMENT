<template>
  <div class="not-found">
    <a-result
      status="404"
      title="404"
      sub-title="Trang bạn tìm kiếm không tồn tại hoặc đã được di chuyển."
    >
      <template #extra>
        <a-button type="primary" @click="goHome">Về trang chính</a-button>
      </template>
    </a-result>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'

const router = useRouter()

const goHome = () => {
  const token = localStorage.getItem('token') || sessionStorage.getItem('token')
  router.push(token ? '/dashboard' : '/')
}
</script>

<style scoped>
.not-found {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 24px;
  background: #f4f5f7;
}
</style>
