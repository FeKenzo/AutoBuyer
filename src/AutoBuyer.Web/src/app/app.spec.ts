import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('creates the application shell', () => {
    const fixture = TestBed.createComponent(AppComponent);

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders the three primary navigation areas', () => {
    const fixture = TestBed.createComponent(AppComponent);

    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const links = element.querySelectorAll('.side-nav a');

    expect(links).toHaveLength(3);
    expect(element.textContent).toContain('Monitoramentos');
    expect(element.textContent).toContain('Promoções');
  });
});
