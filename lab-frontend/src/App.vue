<template>
  <a-config-provider>
    <template #renderEmpty>
      <div style="text-align: center; padding: 40px 0;">
        <folder-open-outlined style="font-size: 56px; color: #1890ff; opacity: 0.3; margin-bottom: 16px;" />
        <p style="color: #8c8c8c; font-size: 15px; margin: 0; font-weight: 500;">Hiện chưa có dữ liệu nào ở đây</p>
      </div>
    </template>
    <router-view />
  </a-config-provider>
</template>

<script setup>
import { FolderOpenOutlined } from '@ant-design/icons-vue'
</script>
