import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  label: string;
  route: string;
  icon: string;
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

@Component({
  selector: 'app-shell',
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  standalone: false,
})
export class ShellComponent implements OnInit {
  readonly navGroups: NavGroup[] = [
    {
      label: 'Resumen',
      items: [
        { label: 'Dashboard', route: '/dashboard', icon: 'space_dashboard' },
        { label: 'Reportes', route: '/reports', icon: 'analytics' },
      ],
    },
    {
      label: 'Operación',
      items: [
        { label: 'Ventas', route: '/sales', icon: 'point_of_sale' },
        { label: 'Compras', route: '/purchases', icon: 'shopping_bag' },
        { label: 'Producción', route: '/productions', icon: 'bakery_dining' },
        { label: 'Inventario', route: '/inventory', icon: 'inventory_2' },
        { label: 'Recetas', route: '/recipes', icon: 'menu_book' },
      ],
    },
    {
      label: 'Catálogos',
      items: [
        { label: 'Productos', route: '/products', icon: 'cake' },
        { label: 'Ingredientes', route: '/ingredients', icon: 'egg_alt' },
        { label: 'Categorías', route: '/categories', icon: 'category' },
        { label: 'Unidades', route: '/units', icon: 'straighten' },
        { label: 'Proveedores', route: '/suppliers', icon: 'local_shipping' },
        { label: 'Clientes', route: '/customers', icon: 'groups' },
      ],
    },
  ];
  readonly adminNavItems: NavItem[] = [{ label: 'Usuarios', route: '/users', icon: 'manage_accounts' }];
  isMobile = false;
  mobileNavOpen = false;
  isNavCollapsed = false;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly breakpointObserver: BreakpointObserver,
  ) {}

  ngOnInit(): void {
    this.breakpointObserver
      .observe('(max-width: 980px)')
      .pipe(takeUntilDestroyed())
      .subscribe((state) => {
        this.isMobile = state.matches;
        this.mobileNavOpen = false;
      });

    if (this.authService.isAuthenticated()) {
      this.authService.me().subscribe({
        error: () => this.logout(),
      });
    }
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  get currentUserName(): string {
    const user = this.authService.getCurrentUserSnapshot();
    return user ? `${user.firstName} ${user.lastName}`.trim() : 'Equipo BakeryFlow';
  }

  toggleNav(): void {
    if (this.isMobile) {
      this.mobileNavOpen = !this.mobileNavOpen;
      return;
    }

    this.isNavCollapsed = !this.isNavCollapsed;
  }

  handleNavigation(): void {
    if (this.isMobile) {
      this.mobileNavOpen = false;
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
