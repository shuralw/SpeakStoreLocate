import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AudioRecorderComponent } from './audio-recorder/audio-recorder.component';
import { FeatureFlagsComponent } from './feature-flags/feature-flags.component';

const routes: Routes = [
  { path: '', component: AudioRecorderComponent },
  // Hidden route: only reachable if you know it (no navigation link)
  { path: '__flags', component: FeatureFlagsComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
