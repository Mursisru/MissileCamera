# MissileCamera shader AssetBundle

UI shader for RawImage IR path (`MissileCamera/Infrared`).

## Rebuild (Unity 2022.3.6f1)

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.6f1\Editor\Unity.exe"
$proj  = "C:\Users\at747\source\repos\MissileCamera\_unity_shader_build"
# If the editor already has that project open, build from a copy (e.g. _unity_shader_build_vision).
& $unity -batchmode -nographics -quit -projectPath $proj `
  -executeMethod MissileCameraShaderBundleBuilder.Build `
  -logFile "$proj\build_bundle.log"
Copy-Item -Force "$proj\AssetBundles\missilecamera_shaders" `
  "C:\Users\at747\Desktop\GITHUB local\MissileCamera\Assets\missilecamera_shaders.bundle"
```

Includes `Hidden/MissileCamera/InfraredBlit` (WhiteHot / BlackHot / Contour) and UI IR shader. Rebuild the plugin after updating the bundle. `_unity_shader_build/` is gitignored; `Assets/missilecamera_shaders.bundle` is embedded into `MissileCamera.dll`.
