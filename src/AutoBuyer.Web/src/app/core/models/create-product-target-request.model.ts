export interface CreateProductTargetRequest {
  storeId: string;
  name: string;
  productUrl: string;
  targetPrice: number;
  autoBuyEnabled: boolean;
}