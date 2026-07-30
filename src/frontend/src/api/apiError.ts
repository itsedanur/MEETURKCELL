import { AxiosError } from 'axios';
import { ApiResponse } from './apiResponse';

export interface ApiErrorDetail {
  code?: string;
  message: string;
}

export class ApiError extends Error {
  public statusCode: number;
  public errors: string[];
  public details?: ApiErrorDetail[];

  constructor(message: string, statusCode: number, errors: string[] = []) {
    super(message);
    this.name = 'ApiError';
    this.statusCode = statusCode;
    this.errors = errors;
  }
}

export const parseApiError = (error: unknown): ApiError => {
  if (error instanceof AxiosError) {
    const statusCode = error.response?.status || 500;
    const responseData = error.response?.data as ApiResponse<any> | undefined;
    
    // Check if the backend returned our standard ApiResponse structure with errors
    if (responseData && responseData.errors && responseData.errors.length > 0) {
      return new ApiError(responseData.message || responseData.errors[0], statusCode, responseData.errors);
    }
    
    // Check if it's a validation problem details object (RFC 7807)
    if (error.response?.data && typeof error.response.data === 'object' && 'errors' in error.response.data) {
       const problemDetails = error.response.data as any;
       if (problemDetails.errors && typeof problemDetails.errors === 'object' && !Array.isArray(problemDetails.errors)) {
          const flatErrors = Object.values(problemDetails.errors).flat() as string[];
          return new ApiError('Doğrulama hatası', statusCode, flatErrors);
       }
    }

    // Default axios error message
    let defaultMsg = error.message;
    if (statusCode === 401) defaultMsg = 'Oturum süreniz doldu. Lütfen tekrar giriş yapın.';
    else if (statusCode === 403) defaultMsg = 'Bu işlem için yetkiniz bulunmuyor.';
    else if (statusCode === 404) defaultMsg = 'Kayıt bulunamadı.';
    else if (statusCode === 413) defaultMsg = 'Dosya boyutu çok büyük.';
    else if (statusCode === 415) defaultMsg = 'Desteklenmeyen dosya türü.';
    else if (statusCode === 409) defaultMsg = 'İşlem çakışması veya veri değiştirilmiş. Lütfen sayfayı yenileyin.';
    else if (statusCode >= 500) defaultMsg = 'Sistem hatası oluştu. Lütfen daha sonra tekrar deneyin.';

    return new ApiError(defaultMsg, statusCode, [defaultMsg]);
  }
  
  // Generic fallback
  return new ApiError(error instanceof Error ? error.message : 'Bilinmeyen bir hata oluştu', 500);
};
