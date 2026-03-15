import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  label: string;
  route: string;
}

@Component({
  selector: 'app-shell',
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  standalone: false,
})
export class ShellComponent implements OnInit {
  readonly navItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard' },
    { label: 'Categorías', route: '/categories' },
    { label: 'Productos', route: '/products' },
    { label: 'Ingredientes', route: '/ingredients' },
    { label: 'Unidades', route: '/units' },
    { label: 'Proveedores', route: '/suppliers' },
    { label: 'Clientes', route: '/customers' },
    { label: 'Recetas', route: '/recipes' },
    { label: 'Compras', route: '/purchases' },
    { label: 'Inventario', route: '/inventory' },
    { label: 'Producción', route: '/productions' },
    { label: 'Ventas', route: '/sales' },
    { label: 'Reportes', route: '/reports' },
  ];
  readonly adminNavItems: NavItem[] = [{ label: 'Usuarios', route: '/users' }];

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.authService.me().subscribe({
        error: () => this.logout(),
      });
    }
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
