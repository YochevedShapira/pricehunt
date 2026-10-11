import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Supplier } from './supplier';

/** Reads the supplier catalog. */
@Injectable({ providedIn: 'root' })
export class SuppliersClient {
  private readonly http = inject(HttpClient);

  list(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>('/api/suppliers');
  }
}
