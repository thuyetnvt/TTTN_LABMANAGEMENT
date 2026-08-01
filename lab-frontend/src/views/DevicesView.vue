<template>
  <div class="devices-container">
    <div class="toolbar">
      <h2>Thiết bị & Tài sản</h2>
    </div>

    <a-card :bordered="false" class="asset-card">
      <a-tabs v-model:activeKey="activeTab" :tabBarGutter="32" class="asset-tabs">
        <template #rightExtra>
          <a-button
            v-if="['Admin', 'Trưởng lab', 'Phó lab'].includes(role)"
            @click="categoryModalVisible = true"
          >
            <template #icon><setting-outlined /></template>
            Quản lý danh mục
          </a-button>
        </template>

        <a-tab-pane key="1" tab="Thiết bị">
          <DeviceTable />
        </a-tab-pane>
        <a-tab-pane key="2" tab="Vật tư tiêu hao">
          <ConsumablesTable />
        </a-tab-pane>
      </a-tabs>
    </a-card>

    <a-modal
      v-model:open="categoryModalVisible"
      title="Quản lý danh mục"
      :footer="null"
      width="860px"
      :destroyOnClose="true"
    >
      <AssetCategoriesTable />
    </a-modal>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { SettingOutlined } from '@ant-design/icons-vue'
import { useAuthStore } from '../stores/authStore'
import DeviceTable from '../components/DeviceTable.vue'
import ConsumablesTable from '../components/ConsumablesTable.vue'
import AssetCategoriesTable from '../components/AssetCategoriesTable.vue'

const authStore = useAuthStore()
const role = computed(() => authStore.role)
const activeTab = ref('1')
const categoryModalVisible = ref(false)
</script>

<style scoped>
.devices-container {
  padding: 0;
}

.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}

.asset-card {
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.05);
}

.asset-card :deep(.ant-card-body) {
  padding: 24px;
}

.asset-tabs :deep(.ant-tabs-nav) {
  margin-bottom: 20px;
}

.asset-tabs :deep(.ant-tabs-tab) {
  padding-top: 0;
}

.asset-tabs :deep(.ant-tabs-tab-btn) {
  font-weight: 600;
}

.asset-tabs :deep(.ant-tabs-extra-content) {
  display: flex;
  align-items: flex-start;
}
</style>
