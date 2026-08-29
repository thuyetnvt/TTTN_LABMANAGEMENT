export const TABLE_PAGE_SIZE = 20
export const TABLE_PAGE_SIZE_OPTIONS = ['10', '20', '50', '100']

export const createTablePagination = (overrides = {}) => ({
  defaultPageSize: TABLE_PAGE_SIZE,
  showSizeChanger: true,
  pageSizeOptions: TABLE_PAGE_SIZE_OPTIONS,
  hideOnSinglePage: false,
  position: ['bottomRight'],
  ...overrides
})
