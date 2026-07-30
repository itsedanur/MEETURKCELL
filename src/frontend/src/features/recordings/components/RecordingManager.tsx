import React, { useState, useCallback, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { MeetingRecordingDto, RecordingStatus } from '../types';
import { getRecordings, uploadRecording } from '../api/recordingsApi';
import { useNotification } from '../../../contexts/NotificationContext';

interface RecordingManagerProps {
  meetingId: string;
  isReadOnly?: boolean;
}

const formatBytes = (bytes: number, decimals = 2) => {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const dm = decimals < 0 ? 0 : decimals;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
};

export const RecordingManager: React.FC<RecordingManagerProps> = ({ meetingId, isReadOnly }) => {
  const queryClient = useQueryClient();
  const { showNotification } = useNotification();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [language, setLanguage] = useState<string>('tr');

  // Fetch recordings with polling if any recording is in Queued/Processing state
  const { data: recordings, isLoading } = useQuery({
    queryKey: ['recordings', meetingId],
    queryFn: () => getRecordings(meetingId),
    refetchInterval: (query) => {
      const data = query.state.data as MeetingRecordingDto[] | undefined;
      const shouldPoll = data?.some(r => r.status === RecordingStatus.Queued || r.status === RecordingStatus.Processing);
      return shouldPoll ? 3000 : false;
    }
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => uploadRecording(meetingId, file, language),
    onSuccess: () => {
      showNotification('Ses dosyası yüklendi ve işleme kuyruğuna alındı.', 'success');
      setSelectedFile(null);
      queryClient.invalidateQueries({ queryKey: ['recordings', meetingId] });
      queryClient.invalidateQueries({ queryKey: ['meetings', meetingId] }); // To update meeting status
    },
    onError: (error: any) => {
      const errMsg = error.response?.data?.message || error.message || 'Bilinmeyen hata';
      showNotification(`Yükleme hatası: ${errMsg}`, 'error');
    }
  });

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      setSelectedFile(e.target.files[0]);
    }
  };

  const handleUpload = () => {
    if (selectedFile) {
      uploadMutation.mutate(selectedFile);
    }
  };

  const getStatusText = (status: RecordingStatus) => {
    switch (status) {
      case RecordingStatus.Uploaded: return 'Yüklendi';
      case RecordingStatus.Validating: return 'Doğrulanıyor';
      case RecordingStatus.Queued: return 'Kuyrukta';
      case RecordingStatus.Processing: return 'İşleniyor (AI)';
      case RecordingStatus.Completed: return 'Tamamlandı';
      case RecordingStatus.Failed: return 'Başarısız';
      case RecordingStatus.Cancelled: return 'İptal Edildi';
      default: return 'Bilinmiyor';
    }
  };

  const getStatusColor = (status: RecordingStatus) => {
    switch (status) {
      case RecordingStatus.Completed: return 'text-green-600 bg-green-50';
      case RecordingStatus.Failed: return 'text-red-600 bg-red-50';
      case RecordingStatus.Processing:
      case RecordingStatus.Queued: return 'text-yellow-600 bg-yellow-50';
      default: return 'text-gray-600 bg-gray-50';
    }
  };

  return (
    <div className="space-y-6">
      {!isReadOnly && (
        <div className="bg-white p-6 border rounded-lg shadow-sm">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Yeni Ses/Video Yükle</h3>
          
          <div className="flex items-center space-x-4 mb-4">
            <div className="flex-1">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Dosya Seç (.mp3, .wav, .m4a, .mp4, .webm)
              </label>
              <input
                type="file"
                accept=".mp3,.wav,.m4a,.mp4,.webm,.ogg"
                onChange={handleFileChange}
                disabled={uploadMutation.isPending}
                className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-primary-50 file:text-primary-700 hover:file:bg-primary-100"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Dil
              </label>
              <select
                value={language}
                onChange={(e) => setLanguage(e.target.value)}
                disabled={uploadMutation.isPending}
                className="block w-32 rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500 sm:text-sm"
              >
                <option value="tr">Türkçe</option>
                <option value="en">İngilizce</option>
              </select>
            </div>
          </div>
          
          <div className="flex justify-end">
            <button
              onClick={handleUpload}
              disabled={!selectedFile || uploadMutation.isPending}
              className={`px-4 py-2 text-sm font-medium text-white rounded-md ${
                !selectedFile || uploadMutation.isPending ? 'bg-gray-400 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700'
              }`}
            >
              {uploadMutation.isPending ? 'Yükleniyor...' : 'Yükle ve İşle'}
            </button>
          </div>
        </div>
      )}

      <div className="bg-white border rounded-lg shadow-sm overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Geçmiş Kayıtlar</h3>
        </div>
        
        {isLoading ? (
          <div className="p-6 text-center text-gray-500">Yükleniyor...</div>
        ) : !recordings || recordings.length === 0 ? (
          <div className="p-6 text-center text-gray-500">Henüz ses veya video dosyası yüklenmemiş.</div>
        ) : (
          <ul className="divide-y divide-gray-200">
            {recordings.map((recording) => (
              <li key={recording.id} className="p-6 flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-900">{recording.originalFileName}</p>
                  <p className="text-xs text-gray-500 mt-1">
                    Boyut: {formatBytes(recording.fileSize)} • Yüklenme: {new Date(recording.createdAt).toLocaleString('tr-TR')}
                  </p>
                  {recording.safeErrorMessage && (
                    <p className="text-xs text-red-600 mt-1">Hata: {recording.safeErrorMessage}</p>
                  )}
                </div>
                <div className="flex items-center space-x-4">
                  {(recording.status === RecordingStatus.Processing || recording.status === RecordingStatus.Queued) && (
                    <svg className="animate-spin h-5 w-5 text-primary-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                  )}
                  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(recording.status)}`}>
                    {getStatusText(recording.status)}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
};
