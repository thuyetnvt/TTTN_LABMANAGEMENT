<template>
  <div class="notifications-container">
    <PageHeader title="Thông báo" subtitle="Lịch sử xử lý và các thông tin cần bạn theo dõi.">
      <template #actions><a-button @click="markAllRead" :disabled="!hasUnread">Đánh dấu tất cả đã đọc</a-button></template>
    </PageHeader>
    <ErrorState v-if="errorMessage" :message="errorMessage" @retry="fetchItems" />
    <a-card :bordered="false">
      <a-list :data-source="items" :loading="loading" item-layout="horizontal">
        <template #renderItem="{ item }">
          <a-list-item :class="{ unread: !item.isRead }" @click="openNotification(item)">
            <a-list-item-meta :title="item.title" :description="item.message">
              <template #avatar><a-avatar :style="{ backgroundColor: item.isRead ? '#94a3b8' : 'var(--color-primary)' }"><bell-outlined /></a-avatar></template>
            </a-list-item-meta>
            <span class="time">{{ formatDate(item.createdAt) }}</span>
          </a-list-item>
        </template>
      </a-list>
      <EmptyState v-if="!errorMessage && !loading && !items.length" description="Chưa có thông báo" />
    </a-card>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { BellOutlined } from '@ant-design/icons-vue'
import { useRouter } from 'vue-router'
import { notificationApi } from '../api/notificationApi'
import PageHeader from '../components/PageHeader.vue'
import EmptyState from '../components/EmptyState.vue'
import ErrorState from '../components/ErrorState.vue'

const router = useRouter()
const items = ref([])
const loading = ref(false)
const errorMessage = ref('')
const hasUnread = computed(() => items.value.some(item => !item.isRead))
const formatDate = value => value ? new Date(value).toLocaleString('vi-VN') : '—'

const fetchItems = async () => {
  loading.value = true
  errorMessage.value = ''
  try { items.value = await notificationApi.getAll() || [] }
  catch (error) { errorMessage.value = error.response?.data?.message || 'Không tải được thông báo.'; message.error(errorMessage.value) }
  finally { loading.value = false }
}
const openNotification = async item => {
  if (!item.isRead) {
    await notificationApi.markRead(item.id)
    item.isRead = true
  }
  if (item.url) router.push(item.url)
}
const markAllRead = async () => {
  await notificationApi.markAllRead()
  items.value.forEach(item => { item.isRead = true })
}
onMounted(fetchItems)
</script>

<style scoped>
.notifications-container { padding: 0; }
.toolbar { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; gap: 16px; }
.toolbar h2 { margin: 0; font-weight: 600; }
.toolbar p { color: #64748b; margin: 6px 0 0; }
.unread { background: #eff6ff; cursor: pointer; }
.time { color: #64748b; font-size: 12px; }
</style>
