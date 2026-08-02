import { ProductTarget } from './product-target.model';
import { PromotionCandidate } from './promotion-candidate.model';

export interface CreateProductTargetFromPromotionResult {
  success: boolean;
  notFound: boolean;
  alreadyImported: boolean;
  productTarget: ProductTarget | null;
  promotion: PromotionCandidate | null;
  error: string | null;
}