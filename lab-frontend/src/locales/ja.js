export default {
  menu: {
    overview: '概要',
    devices: '機器と資材',
    borrowHistory: '貸出/返却履歴',
    teacherApproval: '教員承認',
    maintenanceHistory: 'メンテナンス履歴',
    penalty: '賠償管理',
    userManagement: 'ユーザー管理',
    borrowRequests: '貸出承認',
    consumableRequests: '消耗品承認',
    studentBorrowHistory: '貸出履歴',
    studentConsumableHistory: '消耗品履歴',
    logout: 'ログアウト'
  },
  header: {
    search: '検索',
    workspace: '中央実験室'
  },
  common: {
    add: '追加',
    edit: '編集',
    delete: '削除',
    save: '保存',
    cancel: 'キャンセル',
    detail: '詳細',
    view: '表示',
    actions: '操作',
    confirm: '確認',
    close: '閉じる'
  },
  device: {
    name: '機器名',
    model: 'モデル',
    serial: 'シリアル番号',
    location: '場所',
    status: 'ステータス',
    qrScan: 'QRスキャン',
    exportExcel: 'Excelエクスポート',
    borrow: '借りる',
    addDevice: '機器追加',
    editDevice: '機器編集',
    deviceList: '機器リスト',
    qrcode: 'QRコード',
    scanToBorrow: 'スキャンして借りる',
    statusReady: '利用可能',
    statusBorrowing: '貸出中',
    statusMaintenance: 'メンテナンス中'
  },
  borrow: {
    requestBorrow: '貸出申請',
    expectedReturnDate: '返却予定日',
    teacherGuarantee: '保証教員',
    optional: '任意',
    teacherGuaranteeHint: 'ヒント：必要に応じて教員のメールを入力してください',
    purpose: '目的',
    purposePlaceholder: '利用目的を入力してください',
    sendRequest: 'リクエスト送信',
    selectPlaceholder: '選択してください'
  },
  message: {
    fillAllInfo: '必要な情報をすべて入力してください',
    addSuccess: '追加しました',
    updateSuccess: '更新しました',
    saveError: '保存に失敗しました',
    deleteSuccess: '削除しました',
    borrowSuccess: '貸出に成功しました',
    loadTeacherError: '教員の読み込みに失敗しました',
    loadDeviceError: '機器の読み込みに失敗しました',
    invalidQR: '無効なQRコード',
    deviceNotFound: '機器が見つかりません'
  },
  consumable: {
    addConsumable: '消耗品追加',
    consumableName: '消耗品名',
    unit: '単位',
    currentQuantity: '現在の数量',
    minQuantity: '最小数量',
    status: 'ステータス',
    requestSupply: '供給リクエスト',
    editQuantity: '数量編集',
    consumablePlaceholder: '消耗品名を入力',
    unitPlaceholder: '単位を入力'
  }
}
