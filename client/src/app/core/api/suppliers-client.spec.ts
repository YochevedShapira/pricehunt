import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Supplier } from './supplier';
import { SuppliersClient } from './suppliers-client';

describe('SuppliersClient', () => {
  let client: SuppliersClient;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    client = TestBed.inject(SuppliersClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requests the supplier catalog', () => {
    client.list().subscribe();

    const request = http.expectOne('/api/suppliers');
    expect(request.request.method).toBe('GET');
  });

  it('returns the suppliers from the response', () => {
    const suppliers: Supplier[] = [{ name: 'Oceanic' }, { name: 'BlueWave' }];
    let received: Supplier[] | undefined;
    client.list().subscribe((s) => (received = s));

    http.expectOne('/api/suppliers').flush(suppliers);

    expect(received).toEqual(suppliers);
  });
});
