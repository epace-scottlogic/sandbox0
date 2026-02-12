import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

export interface NavItem {
  title: string;
  route: string;
}

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  navItems = input<NavItem[]>([]);
}
