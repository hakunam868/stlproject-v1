# ParseSTL.Viewer — standalone 3D viewer

The 3D CAD viewer from the MAK3R AI app (`/viewer`), lifted out on its own so it can be shared without the rest of the Maker code. It is a small .NET 10 Blazor WebAssembly app and behaves the same as the in-app viewer.

## Run

Requires the .NET 10 SDK.

```powershell
dotnet run --project ParseSTL.Viewer
```

Then open http://localhost:5317.

## What it does

- **Open** (folder icon) loads a model, replacing the current one; **Import** (cloud icon) adds a model to the scene.
- Formats: STL, 3MF, STEP/STP, IGES/IGS, BREP/BRP. STEP/IGES/BREP use OpenCascade compiled to WebAssembly (`occt-import-js`).
- **Export STL** downloads the visible scene as a binary STL file.
- Controls: left-drag orbits around the model center (yaw/pitch); focus the viewer and press Q/E to roll around the view axis; right-drag pans; the wheel zooms (middle-drag also zooms).
- Right-hand buttons: top / front / right / left / back / bottom views.
- Context menu on a part: colour, isolate, opacity, comment.
- **Capture** (camera icon) — same as the Maker app: the PNG is passed to C# `saveCapture`.

## Publish as a static site

```powershell
dotnet publish ParseSTL.Viewer -c Release -o publish
```

Serve `publish/wwwroot` from any static host. The app expects to be served from the site root (`<base href="/" />` in `wwwroot/index.html`).

## Layout

| Path | Purpose |
| --- | --- |
| `Viewer3D/ViewerPage.razor` | The viewer page (markup copied from Maker's `ViewerPage.razor`) |
| `Viewer3D/ViewerViewModel.cs` | C# methods the JS calls: `jsFetch` (serves the embedded OCCT wasm), `saveCapture`, `loadFile` |
| `Viewer3D/occt-import-js.wasm` | OpenCascade importer, embedded in the assembly |
| `wwwroot/js/viewer/` | Viewer JavaScript (three.js, loaders, controls, assembly tree) — unchanged from Maker |
| `wwwroot/js/viewmodelsMapper.js` | Maker helpers for script loading and JS→C# wiring — unchanged |
| `wwwroot/css/viewer/viewer.css`, `wwwroot/images/` | Viewer styling and icons — unchanged |

## Differences from the Maker copy

- No login, nav menu or plugin system; the page is the whole app.
- Assets are served from `./` instead of `/_content/Maker.Assets.Razor/` (`Viewer3D/BaseUrlService.cs` + the `MakerBaseUrl` meta tag).
- `saveCapture` writes the PNG bytes it receives directly rather than re-encoding them through ImageSharp (the bytes are already PNG). This avoids a dependency with known vulnerabilities; the saved file is the same.

See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for the bundled open-source libraries.
