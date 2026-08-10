export type StoreMonitoringStatus =
  'Supported' | 'TemporarilyBlocked' | 'RequiresSession' | 'RequiresManualAction' | 'Unsupported';

export interface StoreMonitoringState {
  host: string;
  status: StoreMonitoringStatus;
  consecutiveFailures: number;
  lastHttpStatusCode: number | null;
  lastError: string | null;
  lastSuccessAt: string | null;
  lastFailureAt: string | null;
  nextAllowedAttemptAt: string | null;
  updatedAt: string;
}
