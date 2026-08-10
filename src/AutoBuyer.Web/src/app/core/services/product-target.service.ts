import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CreateProductTargetRequest } from '../models/create-product-target-request.model';
import { ProductTarget } from '../models/product-target.model';
import { UpdateProductTargetRequest } from '../models/update-product-target-request.model';

@Injectable({ providedIn: 'root' })
export class ProductTargetService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + '/product-targets';

  getAll(): Observable<ProductTarget[]> {
    return this.http.get<ProductTarget[]>(this.baseUrl);
  }

  create(request: CreateProductTargetRequest): Observable<ProductTarget> {
    return this.http.post<ProductTarget>(this.baseUrl, request);
  }

  update(id: string, request: UpdateProductTargetRequest): Observable<ProductTarget> {
    return this.http.put<ProductTarget>(this.baseUrl + '/' + id, request);
  }

  changeMonitoringStatus(id: string, enabled: boolean): Observable<void> {
    return this.http.patch<void>(this.baseUrl + '/' + id + '/monitoring', {
      enabled,
    });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(this.baseUrl + '/' + id);
  }
}
