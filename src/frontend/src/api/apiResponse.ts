export interface ApiResponse<T> {
  data: T | null;
  message: string | null;
  success: boolean;
  errors: string[] | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
