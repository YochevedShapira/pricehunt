import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('renders the application title', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.app-title')?.textContent).toContain('PriceHunt');
  });

  it('links to both screens', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    const links = Array.from(element.querySelectorAll('a'));
    expect(links.map((a) => a.textContent?.trim())).toEqual(['Search', 'History']);
  });
});
