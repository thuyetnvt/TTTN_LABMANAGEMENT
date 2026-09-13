import { defineStore } from 'pinia'
import { notificationApi } from '../api/notificationApi.js'

let listRequest = null
let countRequest = null

const unwrapList = (payload) => {
  if (Array.isArray(payload)) return { items: payload, page: 1, pageSize: payload.length, total: payload.length, hasNextPage: false }
  return {
    items: payload?.items || payload?.Items || [],
    page: payload?.page || payload?.Page || 1,
    pageSize: payload?.pageSize || payload?.PageSize || 20,
    total: payload?.total || payload?.Total || 0,
    hasNextPage: payload?.hasNextPage ?? payload?.HasNextPage ?? false
  }
}

const getErrorMessage = error => error?.response?.data?.message || error?.message || 'Không tải được thông báo.'

export const useNotificationStore = defineStore('notifications', {
  state: () => ({
    items: [],
    unreadCount: 0,
    loading: false,
    error: '',
    initialized: false,
    page: 1,
    pageSize: 20,
    total: 0,
    hasNextPage: false,
    filterUnreadOnly: false,
    realtimeKeys: []
  }),

  getters: {
    recentItems: state => state.items.slice(0, 7),
    hasUnread: state => state.unreadCount > 0
  },

  actions: {
    async fetchRecent(force = false) {
      if (this.initialized && !force && this.page === 1 && !this.filterUnreadOnly) return this.items
      return this.fetchAll({ page: 1, pageSize: 20, force })
    },

    async fetchAll({ page = 1, pageSize = 20, unreadOnly = false, force = false } = {}) {
      const isFirstPage = page === 1
      if (isFirstPage && this.initialized && !force && this.filterUnreadOnly === unreadOnly) {
        return this.items
      }
      if (listRequest) return listRequest

      this.loading = true
      this.error = ''
      listRequest = (async () => {
        try {
          const listPromise = notificationApi.getAll({ page, pageSize, unreadOnly })
          const shouldRefreshCount = isFirstPage || !this.initialized
          const countPromise = shouldRefreshCount ? this.fetchUnreadCount() : Promise.resolve(this.unreadCount)
          const [payload] = await Promise.all([listPromise, countPromise])
          const result = unwrapList(payload)
          this.items = result.items
          if (isFirstPage) {
            this.initialized = true
            this.filterUnreadOnly = unreadOnly
          }
          this.page = result.page
          this.pageSize = result.pageSize
          this.total = result.total
          this.hasNextPage = result.hasNextPage
          return this.items
        } catch (error) {
          this.error = getErrorMessage(error)
          throw error
        } finally {
          this.loading = false
          listRequest = null
        }
      })()
      return listRequest
    },

    async fetchUnreadCount() {
      if (countRequest) return countRequest
      countRequest = notificationApi.getUnreadCount()
        .then(result => {
          this.unreadCount = result?.count || 0
          return this.unreadCount
        })
        .finally(() => { countRequest = null })
      return countRequest
    },

    async markRead(id) {
      const item = this.items.find(notification => notification.id === id)
      const wasRead = item?.isRead ?? true
      if (item && !wasRead) {
        item.isRead = true
        this.unreadCount = Math.max(0, this.unreadCount - 1)
      }
      try {
        await notificationApi.markRead(id)
        return true
      } catch (error) {
        if (item && !wasRead) {
          item.isRead = false
          this.unreadCount += 1
        }
        this.error = getErrorMessage(error)
        throw error
      }
    },

    async markAllRead() {
      if (this.unreadCount === 0) return true
      const unreadItems = this.items.filter(item => !item.isRead)
      unreadItems.forEach(item => { item.isRead = true })
      const previousCount = this.unreadCount
      this.unreadCount = 0
      try {
        await notificationApi.markAllRead()
        return true
      } catch (error) {
        unreadItems.forEach(item => { item.isRead = false })
        this.unreadCount = previousCount
        this.error = getErrorMessage(error)
        throw error
      }
    },

    handleRealtimeNotification(payload) {
      const normalized = typeof payload === 'string'
        ? { title: 'Thông báo mới', message: payload, url: '' }
        : payload || {}
      const key = normalized.id
        ? `id:${normalized.id}`
        : `${normalized.title || ''}|${normalized.message || ''}|${normalized.url || ''}`
      if (this.realtimeKeys.includes(key)) return false
      this.realtimeKeys = [key, ...this.realtimeKeys].slice(0, 100)

      const existing = normalized.id && this.items.find(item => item.id === normalized.id)
      if (existing) {
        Object.assign(existing, normalized)
        return false
      }

      this.items.unshift({
        id: normalized.id,
        type: normalized.type || 'GENERAL',
        title: normalized.title || 'Thông báo mới',
        message: normalized.message || '',
        url: normalized.url || '',
        isRead: false,
        createdAt: normalized.createdAt || new Date().toISOString(),
        readAt: null
      })
      this.items = this.items.slice(0, 100)
      this.total += 1
      this.unreadCount += 1
      return true
    }
  }
})
