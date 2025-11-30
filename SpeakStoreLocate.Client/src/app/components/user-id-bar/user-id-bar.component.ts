import {
    Component,
    ElementRef,
    OnDestroy,
    OnInit,
    ViewChild,
} from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { UserIdService, USER_ID_REGEX } from '../../services/user-id.service';

@Component({
    selector: 'app-user-id-bar',
    templateUrl: './user-id-bar.component.html',
    styleUrls: ['./user-id-bar.component.scss'],
})
export class UserIdBarComponent implements OnInit, OnDestroy {
    readonly pattern = '[A-Za-z0-9_\\-]{1,64}';
    readonly userIdControl = new FormControl('', [
        Validators.required,
        Validators.pattern(USER_ID_REGEX),
    ]);

    currentUserId: string | null = null;
    editMode = false;
    showPrompt = false;

    private readonly destroy$ = new Subject<void>();

    @ViewChild('userIdInput') userIdInput?: ElementRef<HTMLInputElement>;

    constructor(private readonly userIdService: UserIdService) { }

    ngOnInit(): void {
        const initialUserId = this.userIdService.getUserId();
        this.currentUserId = initialUserId;
        this.userIdControl.setValue(initialUserId ?? '');
        this.editMode = !initialUserId;
        this.showPrompt = !initialUserId;

        this.userIdService.changes$
            .pipe(takeUntil(this.destroy$))
            .subscribe((userId) => {
                this.currentUserId = userId;
                if (!this.editMode) {
                    this.userIdControl.setValue(userId ?? '', { emitEvent: false });
                }
                this.showPrompt = !userId;
            });

        this.userIdService.focusRequests$
            .pipe(takeUntil(this.destroy$))
            .subscribe(() => {
                this.startEditing(true);
            });
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    startEditing(force = false): void {
        if (!this.editMode) {
            this.editMode = true;
            this.userIdControl.setValue(this.currentUserId ?? '');
        }

        if (force) {
            this.userIdControl.markAsTouched();
            this.userIdControl.markAsDirty();
            this.showPrompt = true;
        }

        this.focusInput();
    }

    saveUserId(): void {
        
        this.userIdControl.markAsTouched();
        const rawValue = this.userIdControl.value ?? '';
        const trimmed = rawValue.trim();

        this.userIdControl.setValue(trimmed, { emitEvent: false });
        this.userIdControl.updateValueAndValidity({ emitEvent: false });

        if (this.userIdControl.invalid || !this.userIdService.isValidUserId(trimmed)) {
            return;
        }

        try {
            this.userIdService.setUserId(trimmed);
            this.editMode = false;
            this.userIdControl.markAsPristine();
            this.showPrompt = false;
        } catch {
            this.userIdControl.setErrors({ pattern: true });
        }
    }

    cancel(): void {
        this.userIdControl.setValue(this.currentUserId ?? '');
        this.userIdControl.markAsPristine();
        this.userIdControl.markAsUntouched();
        this.editMode = !this.currentUserId;
        this.showPrompt = !this.currentUserId;
    }

    private focusInput(): void {
        setTimeout(() => this.userIdInput?.nativeElement?.focus(), 0);
    }
}
