# 147VR Cue Station

Project-facing assets for the stage-side cue display.

## Source and rights
The source files were copied from the user's local APK extraction workspace:
C:\Users\mongo\Tools\pulled_apks\_export\

The user confirmed on 2026-11-05 that they have authorization to use these assets in the 147 VR project. Exact original paths, file sizes, and SHA256 hashes are preserved in AssetManifest.json for internal provenance and license traceability.

Project-facing names use the 147VR prefix. This is naming normalization only; it does not remove or falsify source attribution.

## Imported assets
- Models/147VR_CueRack_HighPoly.obj — high-poly cue rack; source local Z is vertical.
- Models/147VR_SnookerCue_HighPoly.obj — high-poly snooker cue; source local X is its length.
- Textures/ — rack albedo/normal and cue albedo variants, normal, metallic, and AO maps.

## Unity import notes
- Keep model scale at 1.0.
- Convert rack Z-up to Unity Y-up with a parent rotation of -90 degrees around X.
- Orient the cue model's local X axis along the rack's local Z axis.
- Cue display objects are static visual props; no Rigidbody or gameplay collider is added by this asset set.
