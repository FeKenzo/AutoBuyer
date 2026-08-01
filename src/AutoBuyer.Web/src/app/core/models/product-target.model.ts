export interface ProductTarget {
  id: string;
  storeId: string;
  storeName: string;
  name: string;
  productUrl: string;
  targetPrice: number;
  currentPrice: number | null;
  targetReached: boolean;
  lastCapturedAt: string | null;
  autoBuyEnabled: boolean;
  monitoringEnabled: boolean;
  createdAt: string;
}