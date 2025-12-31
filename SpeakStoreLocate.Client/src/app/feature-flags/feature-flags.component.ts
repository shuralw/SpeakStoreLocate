import { Component } from '@angular/core';
import { FeatureFlagsService } from '../services/feature-flags.service';

@Component({
  selector: 'app-feature-flags',
  templateUrl: './feature-flags.component.html',
  styleUrls: ['./feature-flags.component.scss'],
  standalone: false,
})
export class FeatureFlagsComponent {
  constructor(public flags: FeatureFlagsService) {}

  onLocalSaveChanged(checked: boolean): void {
    this.flags.setLocalSaveInsteadOfBackend(checked);
  }
}
