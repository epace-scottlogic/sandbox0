import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar, NavItem } from './navbar/navbar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  navItems: NavItem[] = [{ title: 'Blockchain', route: '/blockchain' }];
}
