import { TestBed } from '@angular/core/testing';
import { RouterModule } from '@angular/router';
import { Navbar, NavItem } from './navbar';

describe('Navbar', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Navbar, RouterModule.forRoot([])],
    }).compileComponents();
  });

  it('should create the component', () => {
    const fixture = TestBed.createComponent(Navbar);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should always render the Home link', async () => {
    const fixture = TestBed.createComponent(Navbar);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    const links = compiled.querySelectorAll('a');
    expect(links.length).toBe(1);
    expect(links[0].textContent).toContain('Home');
  });

  it('should render nav items passed via input', async () => {
    const fixture = TestBed.createComponent(Navbar);
    const items: NavItem[] = [
      { title: 'Blockchain', route: '/blockchain' },
      { title: 'Settings', route: '/settings' },
    ];
    fixture.componentRef.setInput('navItems', items);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    const links = compiled.querySelectorAll('a');
    expect(links.length).toBe(3);
    expect(links[0].textContent).toContain('Home');
    expect(links[1].textContent).toContain('Blockchain');
    expect(links[2].textContent).toContain('Settings');
  });
});
