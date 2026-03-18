import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
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
  @ViewChild('userDialogTemplate') userDialogTemplate?: TemplateRef<unknown>;
  @ViewChild('passwordDialogTemplate') passwordDialogTemplate?: TemplateRef<unknown>;
  users: UserItem[] = [];
  loading = true;
  submitting = false;
  error = '';
  editingId: string | null = null;
  passwordUserId: string | null = null;
  private userDialogRef: MatDialogRef<unknown> | null = null;
  private passwordDialogRef: MatDialogRef<unknown> | null = null;

  readonly form;
  readonly passwordForm;

  constructor(
    private readonly fb: FormBuilder,
    private readonly apiService: ApiService,
    private readonly dialog: MatDialog,
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
    this.loading = true;
    this.apiService.getPaged<UserItem>('users', { page: 1, pageSize: 50 }).subscribe({
      next: (result: PagedResult<UserItem>) => {
        this.users = result.items;
        this.error = '';
        this.loading = false;
        this.submitting = false;
      },
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudieron cargar los usuarios.';
        this.users = [];
        this.loading = false;
        this.submitting = false;
      },
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.error = '';

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
        this.closeUserDialog();
        this.resetForm();
        this.loadUsers();
      },
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudo guardar el usuario.';
        this.submitting = false;
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
    this.openUserDialog();
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
    this.submitting = false;
  }

  openCreateDialog(): void {
    this.resetForm();
    this.openUserDialog();
  }

  toggleStatus(user: UserItem): void {
    this.submitting = true;
    this.apiService.patch(`users/${user.id}/toggle-status`).subscribe({
      next: () => this.loadUsers(),
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudo actualizar el estado.';
        this.submitting = false;
      },
    });
  }

  openPasswordDialog(userId: string): void {
    this.passwordUserId = userId;
    this.passwordForm.reset({ password: '' });
    if (!this.passwordDialogTemplate) {
      return;
    }

    this.passwordDialogRef = this.dialog.open(this.passwordDialogTemplate, {
      width: 'min(520px, calc(100vw - 2rem))',
      maxWidth: 'calc(100vw - 2rem)',
      panelClass: 'bf-dialog-panel',
      autoFocus: false,
    });

    this.passwordDialogRef.afterClosed().subscribe(() => {
      this.passwordDialogRef = null;
      this.passwordUserId = null;
      this.passwordForm.reset({ password: '' });
    });
  }

  changePassword(): void {
    if (!this.passwordUserId || this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.apiService
      .post(`users/${this.passwordUserId}/change-password`, this.passwordForm.getRawValue())
      .subscribe({
        next: () => {
          this.closePasswordDialog();
          this.submitting = false;
        },
        error: (error: { error?: { message?: string } }) => {
          this.error = error.error?.message ?? 'No se pudo cambiar la contraseña.';
          this.submitting = false;
        },
      });
  }

  get isEmpty(): boolean {
    return !this.loading && !this.error && this.users.length === 0;
  }

  closePasswordDialog(): void {
    this.passwordDialogRef?.close();
    this.passwordDialogRef = null;
    this.passwordUserId = null;
    this.passwordForm.reset({ password: '' });
  }

  closeUserDialog(): void {
    this.userDialogRef?.close();
    this.userDialogRef = null;
  }

  private openUserDialog(): void {
    if (!this.userDialogTemplate) {
      return;
    }

    this.userDialogRef = this.dialog.open(this.userDialogTemplate, {
      width: 'min(760px, calc(100vw - 2rem))',
      maxWidth: 'calc(100vw - 2rem)',
      panelClass: 'bf-dialog-panel',
      autoFocus: false,
    });

    this.userDialogRef.afterClosed().subscribe(() => {
      this.userDialogRef = null;
      this.resetForm();
    });
  }
}
