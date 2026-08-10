export interface CreateProductTargetFromPromotionRequest {
  storeId: string;
  targetPrice: number | null;
  productUrl: string | null;
  autoBuyEnabled: boolean;
}
