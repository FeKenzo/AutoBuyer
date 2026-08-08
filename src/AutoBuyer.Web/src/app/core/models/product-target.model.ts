export interface ProductTarget {
  id: string;
  storeId: string;
  storeName: string;
  name: string;
  productUrl: string;
  externalProductId: string | null;
  targetPrice: number | null;
  lastObservedPrice: number | null;
  lastSeenAt: string | null;
  currentPrice: number | null;
  targetReached: boolean;
  lastCapturedAt: string | null;
  autoBuyEnabled: boolean;
  monitoringEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}
