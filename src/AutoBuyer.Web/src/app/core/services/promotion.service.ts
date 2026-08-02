import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  PromotionCandidate,
  PromotionCandidateStatus
} from '../models/promotion-candidate.model';
import { CreateProductTargetFromPromotionRequest } from
  '../models/create-product-target-from-promotion-request.model';
import { CreateProductTargetFromPromotionResult } from
  '../models/create-product-target-from-promotion-result.model';

@Injectable({
  providedIn: 'root'
})
export class PromotionService {
  private readonly http = inject(HttpClient);

  private readonly baseUrl =
    `${environment.apiUrl}/promotions`;

  getAll(
    status?: PromotionCandidateStatus | null
  ): Observable<PromotionCandidate[]> {
    let params = new HttpParams();

    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<PromotionCandidate[]>(
      this.baseUrl,
      { params }
    );
  }

  createProductTarget(
    promotionId: string,
    request: CreateProductTargetFromPromotionRequest
  ): Observable<CreateProductTargetFromPromotionResult> {
    return this.http.post<CreateProductTargetFromPromotionResult>(
      `${this.baseUrl}/${promotionId}/product-target`,
      request
    );
  }

  ignore(promotionId: string): Observable<void> {
    return this.http.patch<void>(
      `${this.baseUrl}/${promotionId}/ignore`,
      {}
    );
  }
}