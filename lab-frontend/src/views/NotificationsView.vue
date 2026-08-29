<template>
  <div class="notifications-container">
    <PageHeader title="Thông báo" subtitle="Lịch sử xử lý và các thông tin cần bạn theo dõi.">
      <template #actions>
        <a-button type="primary" :disabled="!notificationStore.hasUnread" :loading="markingAll" @click="markAllRead">
          Đánh dấu tất cả đã đọc
        </a-button>
      </template>
    </PageHeader>

    <a-card :bordered="false" class="notifications-card">
      <div class="notification-toolbar">
        <a-tabs v-model:active-key="activeFilter" @change="changeFilter">
          <a-tab-pane key="all" tab="Tất cả" />
          <a-tab-pane key="unread" tab="Chưa đọc" />
        </a-tabs>
        <span class="unread-summary">{{ notificationStore.unreadCount }} chưa đọc</span>
      </div>

      <ErrorState v-if="notificationStore.error" :message="notificationStore.error" @retry="retry" />
      <div v-else-if="notificationStore.loading" class="notification-page-loading">
        <a-skeleton v-for="index in 5" :key="index" active :paragraph="{ rows: 2 }" />
      </div>
      <a-list v-else-if="notificationStore.items.length" class="notification-page-list" :data-source="notificationStore.items">
        <template #renderItem="{ item }">
          <a-list-item
            :class="['notification-page-item', { 'notification-page-item-unread': !item.isRead }]"
            tabindex="0"
            role="button"
            @click="openNotification(item)"
            @keydown.enter="openNotification(item)"
          >
            <template #extra><span class="notification-page-time">{{ formatRelativeTime(item.createdAt) }}</span></template>
            <a-list-item-meta>
              <template #avatar>
                <span class="notification-page-icon"><component :is="notificationIcon(item.type)" /></span>
              </template>
              <template #title>
                <span class="notification-page-title">{{ item.title }}</span>
                <a-tag class="notification-page-type">{{ notificationTypeLabel(item.type) }}</a-tag>
                <span v-if="!item.isRead" class="notification-page-unread-label">Chưa đọc</span>
              </template>
              <template #description><span class="notification-page-description">{{ item.message }}</span></template>
            </a-list-item-meta>
          </a-list-item>
        </template>
      </a-list>
      <EmptyState v-else description="Không có thông báo phù hợp." />

      <div v-if="!notificationStore.error && notificationStore.total > 0" class="notification-pagination">
        <a-pagination
          :current="notificationStore.page"
          :page-size="notificationStore.pageSize"
          :total="notificationStore.total"
          :show-size-changer="true"
          :page-size-options="TABLE_PAGE_SIZE_OPTIONS"
          :hide-on-single-page="false"
          @change="changePage"
        />
      </div>
    </a-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { useRouter } from 'vue-router'
import PageHeader from '../components/PageHeader.vue'
import EmptyState from '../components/EmptyState.vue'
import ErrorState from '../components/ErrorState.vue'
import { useNotificationStore } from '../stores/notificationStore'
import { formatRelativeTime, notificationIcon, notificationTypeLabel } from '../utils/notificationUtils'
import { TABLE_PAGE_SIZE, TABLE_PAGE_SIZE_OPTIONS } from '../utils/tablePagination'

const router = useRouter()
const notificationStore = useNotificationStore()
const activeFilter = ref('all')
const markingAll = ref(false)

const load = (force = false) => notificationStore.fetchAll({
  page: 1,
  pageSize: TABLE_PAGE_SIZE,
  unreadOnly: activeFilter.value === 'unread',
  force
}).catch(() => {})

const retry = () => load(true)

const changeFilter = () => load(true)

const changePage = (page, pageSize) => notificationStore.fetchAll({
  page,
  pageSize,
  unreadOnly: activeFilter.value === 'unread'
}).catch(() => {})

const openNotification = async item => {
  try {
    await notificationStore.markRead(item.id)
  } catch (error) {
    message.error(error?.message || 'Không thể cập nhật thông báo.')
    return
  }
  if (typeof item.url === 'string' && item.url.startsWith('/')) router.push(item.url)
}

const markAllRead = async () => {
  markingAll.value = true
  try {
    await notificationStore.markAllRead()
  } catch (error) {
    message.error(error?.message || 'Không thể cập nhật thông báo.')
  } finally {
    markingAll.value = false
  }
}

onMounted(() => load())
</script>

<style scoped>
.notifications-container { padding: 0; }
.notifications-card { overflow: hidden; }
.notification-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin-bottom: 4px; }
.notification-toolbar :deep(.ant-tabs-nav) { margin-bottom: 0; }
.unread-summary { color: var(--color-secondary); font-size: 13px; white-space: nowrap; }
.notification-page-list :deep(.ant-list-item) { align-items: flex-start; padding: 16px 12px; border-radius: 12px; cursor: pointer; transition: background-color .15s ease; }
.notification-page-list :deep(.ant-list-item:hover),
.notification-page-list :deep(.ant-list-item:focus-visible) { background: rgba(217, 119, 87, .08); outline: none; }
.notification-page-item-unread { background: rgba(217, 119, 87, .08); }
.notification-page-icon { display: inline-flex; align-items: center; justify-content: center; width: 38px; height: 38px; color: var(--color-primary); background: rgba(217, 119, 87, .12); border-radius: 11px; font-size: 18px; }
.notification-page-title { color: var(--color-ink); font-weight: 650; }
.notification-page-type { margin-inline-start: 8px; color: var(--color-primary); border-color: rgba(217, 119, 87, .25); background: transparent; }
.notification-page-unread-label { margin-inline-start: 8px; color: var(--color-primary); font-size: 12px; }
.notification-page-description { display: block; overflow-wrap: anywhere; color: var(--color-secondary); }
.notification-page-time { color: var(--color-secondary); font-size: 12px; white-space: nowrap; }
.notification-pagination { display: flex; justify-content: center; padding-top: 20px; }
.notification-page-loading { display: grid; gap: 12px; padding: 12px; }
@media (max-width: 640px) {
  .notification-toolbar { align-items: flex-start; flex-direction: column; gap: 0; }
  .unread-summary { margin-bottom: 8px; }
  .notification-page-list :deep(.ant-list-item) { padding: 14px 8px; }
  .notification-page-list :deep(.ant-list-item-extra) { margin-inline-start: 8px; }
  .notification-page-time { display: block; margin-top: 48px; }
  .notification-page-title { display: inline; }
}
</style>
