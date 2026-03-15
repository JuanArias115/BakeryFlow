import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { PagedResult } from '../../core/models/api.models';

interface UserItem {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: 'Admin' | 'Operator';
  isActive: boolean;
}

@Component({
  selector: 'app-users-page',
  templateUrl: './users-page.component.html',
  styleUrl: './users-page.component.scss',
  standalone: false,
})
export class UsersPageComponent implements OnInit {
  users: UserItem[] = [];
  error = '';
  editingId: string | null = null;
  passwordUserId: string | null = null;

  readonly form;
  readonly passwordForm;

  constructor(
    private readonly fb: FormBuilder,
    private readonly apiService: ApiService,
  ) {
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: [''],
      email: ['', [Validators.required, Validators.email]],
      role: ['Operator', Validators.required],
      isActive: [true, Validators.required],
      password: ['', [Validators.required, Validators.minLength(8)]],
    });

    this.passwordForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.apiService.getPaged<UserItem>('users', { page: 1, pageSize: 50 }).subscribe({
      next: (result: PagedResult<UserItem>) => {
        this.users = result.items;
      },
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudieron cargar los usuarios.';
      },
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload = this.editingId
      ? {
          firstName: raw.firstName,
          lastName: raw.lastName ?? '',
          email: raw.email,
          role: raw.role,
          isActive: !!raw.isActive,
        }
      : {
          firstName: raw.firstName,
          lastName: raw.lastName ?? '',
          email: raw.email,
          role: raw.role,
          isActive: !!raw.isActive,
          password: raw.password || '',
        };

    const request$ = this.editingId
      ? this.apiService.put(`users/${this.editingId}`, payload)
      : this.apiService.post('users', payload);

    request$.subscribe({
      next: () => {
        this.resetForm();
        this.loadUsers();
      },
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudo guardar el usuario.';
      },
    });
  }

  edit(user: UserItem): void {
    this.editingId = user.id;
    this.form.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      role: user.role,
      isActive: user.isActive,
      password: '',
    });
    this.form.get('password')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();
  }

  resetForm(): void {
    this.editingId = null;
    this.form.reset({
      firstName: '',
      lastName: '',
      email: '',
      role: 'Operator',
      isActive: true,
      password: '',
    });
    this.form.get('password')?.setValidators([Validators.required, Validators.minLength(8)]);
    this.form.get('password')?.updateValueAndValidity();
  }

  toggleStatus(user: UserItem): void {
    this.apiService.patch(`users/${user.id}/toggle-status`).subscribe({
      next: () => this.loadUsers(),
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudo actualizar el estado.';
      },
    });
  }

  openPasswordDialog(userId: string): void {
    this.passwordUserId = userId;
    this.passwordForm.reset({ password: '' });
  }

  changePassword(): void {
    if (!this.passwordUserId || this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.apiService
      .post(`users/${this.passwordUserId}/change-password`, this.passwordForm.getRawValue())
      .subscribe({
        next: () => {
          this.passwordUserId = null;
          this.passwordForm.reset({ password: '' });
        },
        error: (error: { error?: { message?: string } }) => {
          this.error = error.error?.message ?? 'No se pudo cambiar la contraseña.';
        },
      });
  }
}
