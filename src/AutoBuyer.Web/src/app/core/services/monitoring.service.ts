import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { StoreMonitoringState } from '../models/store-monitoring-state.model';

@Injectable({ providedIn: 'root' })
export class MonitoringService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + '/monitoring';

  getStoreStates(): Observable<StoreMonitoringState[]> {
    return this.http.get<StoreMonitoringState[]>(this.baseUrl + '/stores');
  }
}
