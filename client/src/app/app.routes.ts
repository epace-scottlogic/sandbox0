import { Routes } from '@angular/router';
import { Home } from './home/home';
import { Blockchain } from '../blockchain/blockchain';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'blockchain', component: Blockchain },
];
