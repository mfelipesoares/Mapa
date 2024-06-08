# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [3.39.0-exp.2] - 2024-06-03
### Needle Engine
- Fix: vite build pipeline plugin 

### Unity Integration 
- Fix: run npm install with include optional sharp for build pipeline

## [3.39.0-exp] - 2024-06-03
### Needle Engine
- Fix: Issue where OrbitControls setTarget not working as expected sometimes due to first frame matrices not being updated yet
- Fix: Gizmos sometimes rendering for 2 frames instead of just 1
- Change: WebXRImageTracking now applies some smoothing based on jitter amount
- Change: Bump gltf-build-pipeline package to 2.1

### Unity Integration 
- Fix: Issue where context menu export would not clear internal cache that prevented re-exporting the same file multiple times
- Fix: Export context menu handle case where directories can not be deleted because they're locked by a CLI tool
- Change: Dont write progressive texture max size if no specific settings are found because we then want it to be handled by the build pipeline package (depends on the usecase etc)

## [3.38.0-exp.1] - 2024-05-29
### Needle Engine
- Fix: needle menu CSS blur in safari
- Fix: USDZ - move Animation component animations into correct sequence depending on whether it should loop or not
- Fix: QuickLook button being created by WebXR despite `usdzExporter.allowCreateQuicklookButton` explicitly being off
- Fix: Issue in vite plugin for Node 16 where fetch wasn't supported yet 
- Fix: AudioListener should remove itself when disabled

### Unity Integration 
- Minor WebXR component tooltip improvements

## [3.38.0-exp] - 2024-05-29
### Needle Engine
- Add: OrbitControls `clickBackgroundToFitScene` property that can be used to allow autofitting when users click on the background. By default it is set to 2 clicks
- Add: Vite plugin option to open browser with network ip address when the server starts by setting `needlePlugins(command, config, { openBrowser: true })` in vite.config.js
- Fix: USDZ image tracking orientation corrects for node world matrix now, resulting in proper placement relative to the image
- Fix: WebARSessionRoot matrix and invertForward were not correctly applied for USDZ export
- Change: WebXR Image Tracking orientation was rotated by 180°. Now it's consistent between iOS USDZ and Android WebXR. If you're using image tracking, you might have to rotate your content 180° to adjust to the new orientation.

### Unity Integration 
- Add: OrbitControls `clickBackgroundToFitScene` property that can be used to allow autofitting when users click on the background. By default it is set to 2 clicks

## [3.37.16-exp] - 2024-05-28
### Needle Engine
- Add: `Gizmos.DrawWireMesh` 
- Fix: use lowpoly raycast mesh again when available
- Fix: USDZ - issue where image tracking orientation was inconsistent between Android and iOS
- Change: USDZ - better logs when unsupported animation tracks are used during export (e.g. .activeSelf or material animations).
- Change: USDZ - show balloon warning when exporting unsupported track types

### Unity Integration 
- Add: ImageTracking now renders gizmo texture on tracked object

## [3.37.15-exp] - 2024-05-27
### Needle Engine
- Add: ScreenCapture `deviceFilter` and `deviceName` properties that simplify camera selection
- Fix: Avatar hands being visible in screenbased AR
- Fix: USDZ workaround for Apple bug FB13808839 - skeletal mesh rest poses must be decomposable in RealityKit
- Fix: Issue where Lightmap LOD textures did not load
- Fix: Issue where Custom Shader LOD textures did not load
- Change: `Powered by Needle` tag can be hidden with indie license

### Unity Integration 
- Add: ScreenCapture `deviceName` field and dropdown for simplified camera selection
- Change: `Powered by Needle` tag can be hidden with indie license using the Needle Menu component

## [3.37.14-exp] - 2024-05-24
### Needle Engine
- Fix: Issue where deactivated SpriteRenderer would not be included in USDZ
- Fix: Minor `@serializable` warning in SpriteRenderer 
- Fix: SpriteRenderer progressive textures
- Change: bump `gltf-progressive` package which includes a vanilla three.js example and fixes issue where texture settings were not re-applied correctly after having loaded the texture LOD (e.g. filter)

## [3.37.13-exp] - 2024-05-23
### Needle Engine
- Add: WebXRImageTracking `hideWhenTrackingIsLost` option to configure if objects should stay visible or hide when tracking is lost
- Add: WebARSessionRoot `autoPlace` option to allow automatically placing the scene content on the first XR hit 
- Fix: WebXR component `createQRCode` options now respects Needle Menu QR code option
- Fix: Bump `gltf-progressive` package to support updating LODs when using postprocessing effects
- Fix: AR placement being prevented by other scripts that caused the event being `used`
- Change: QR code now warns when being used for scanning a `localhost` address

### Unity Integration 
- Add: WebXRImageTracking `hideWhenTrackingIsLost` option to configure if objects should stay visible or hide when tracking is lost
- Add: WebARSessionRoot `autoPlace` option to allow automatically placing the scene content on the first XR hit 

## [3.37.12-exp] - 2024-05-17
### Needle Engine
- Add: `onClear` (invoked e.g. when `<needle-engine src>` changes) and `onDestroy` hooks (invoked when the needle engine context is disposed)

### Unity Integration 
- Fix: Compilation on Linux due to wrong compiler directive
- Fix: Disabling `Allow Progressive Loading` in the ProgressiveLoadingSettings component now prevents progressive textures from being generated
- Change: OrbitControls exposes zoomspeed and zoomToCursor + add some more jsdoc comments
- Change: CodeWatcher now watches `.tsx` files as well

## [3.37.11-exp] - 2024-05-15
### Needle Engine
- Add: OrbitControls `zoomSpeed` and `zoomToCursor` properties
- Add: `screenshot2` method that takes an options object for easier configuration and transparent screenshots support.
- Add: `context.menu.setVisible` method for hiding the Needle Menu from code
- Add: USDZ API now supports playing back audio from custom behavior scripts. Usage from inside `createBehaviours`:
  ```ts
  const audioClip = ext.addAudioClip(clipUrl);
  const behavior = new BehaviorModel("playAudio",
      TriggerBuilder.tapTrigger(this.gameObject),  
      ActionBuilder.playAudioAction(playbackTarget, audioClip, "play", volume, auralMode),
  );
  ```
- Add: USDZ API now supports registering animations from custom behavior scripts (experimental)
better lighting response
- Add: animator state speed support in USDZ
- Fix: USDZ export had multiple identical textures for the same cloned image
- Fix: USDZ sprites had wrong lighting because of QuickLook bug with non-specified normals, now emitting (0,0,1) and not (0,0,0) as fallback normals
- Fix: HideOnStart should not hide objects in USDZ that were manually enabled in the scene before
- Fix: issue with texture tiling being 0 in any direction leading to invalid USDZ files
- Fix: Contact Shadows now have consistent blur independent of scene scale
- Fix: Regression with audio not playing at start of USDZ scene
- Fix: Contact Shadows now properly correct for different scene aspect ratios 
- Fix: SceneSwitcher `scenes` array not immediately when creating from code 
- Fix: `onStart` hook not being called for all `<needle-engine>` elements
- Fix: Script registration to correct context when loading multiple `<needle-engine>` components on one page
- Fix: Renderer allow changing the ReflectionProbe anchor at runtime
- Fix: Reflection probes with lightmaps causing memory leak
- Fix: Contact Shadows now have consistent blur
- Fix: Contact Shadows auto fit box is now correctly setup to include the whole scene
- Fix: Balloon Messages overflowing for very long words
- Fix: Issue with OrbitControls `zoomToCursor` enabled in cases where the loaded glTF didn't contain any camera
- Change: USDZ now exports disabled objects as well, sets their visibility to false (for regular USD) and hides them on start (for QuickLook)
- Change: `clearOverlayMessages` has been renamed to `clearBalloonMessages`
- Change: Allow hiding the needle menu for local development 

## [3.37.10-exp.1] - 2024-05-07
### Needle Engine
- Add: expose `clearOverlayMessages` method
- Fix: SyncedTransform should not set kinematic if `overridePhysics` is false
- Fix: CustomShader Screenspace support for shaders exported from Unity 2022
- Change: enable preload on audio sources dynamically created by PlayAudioOnClick
- Change: improve URL name parsing for loading screen for blob URLs

### Unity Integration 
- Fix: DeployToItch needs to disable gzip temporarely

## [3.37.10-exp] - 2024-05-06
### Needle Engine
- Add: static `ContactShadows.auto` 
- Add: ContactShadows `autoFit` option to automatically fit the contact shadows at startup and `fitShadows()` method for applying autofit manually
- Change: Improve PWA logging and rename Vite `pwaOptions` to `pwa` in needlePlugin
- Change: Improve loading screen rendering

### Unity Integration 
- Change: bump UnityGLTF dependency to 2.12.0

## [3.37.9-exp] - 2024-05-03
### Needle Engine
- Add: `INeedleGLTFExtensionPlugin.onLoaded` hook providing access to the loaded glTF when registering custom extensions
- Add: PWA ability to specify updateInterval (number in ms or undefined) for auto-updating apps while running
- Fix: WebXRImageTracking now restores tracked objects to their previous state after exit AR
- Fix: WebXRImageTracking extra check in session enabled features if image tracking is even enabled. Otherwise I did get tons of errors in mobile VR
- Fix: Input system now handles mouse wheel during pointer lock
- Fix: Simplify and improve PWA generation and passing workbox config to vite-pwa
- Fix: AnimatorController with multiple layers: don't select start state in another layer
- Fix: AnimatorController handle empty state to stay in last animated previous pose
- Bump gltf-progressive dependency for fixes regarding transparent materials as well as VRM materials
- Bump gltf-build-pipeline dependency to 1.5 alpha for VRM support

### Unity Integration 
Change: improve better skybox shader by using a global texture instead of per-material

## [3.37.8-exp.1] - 2024-05-02
### Needle Engine
- Fix: Camera should not set skybox from scene again automatically if there's a background skybox already
- Fix: Multi-material LOD meshes
- Change: Only set GLTFLoaders if none others are already set
- Change: vite userconfig expose "allowHotReload" in jsdoc types (third argument in `needlePlugins`)

### Unity Integration
- Fix: Prevent export of top-level GltfObject components in disabled hierarchy

## [3.37.8-exp] - 2024-04-30
### Needle Engine
- Add: `@needle-tools/gltf-progressive` dependency that handles loading progressive meshes and textures.

## [3.37.7-pre.1] - 2024-04-29
### Needle Engine
- Fix: USDZ regression in writing timeSamples > 1000

## [3.37.7-pre] - 2024-04-29
### Needle Engine
- Fix: USDZ animation loops didnt work in some cases
- Fix: Properly apply and revert arScale on USDZ export
- Fix: Correctly apply WebXR arSceneScale on USDZ export even when no USDZExporter is present
- Fix: Hand models not being displayed on VisionPro – invalid data passed into registerExtensions leading to exception
- Fix: `time.timescale` set to 0 now fully pauses physics simulation
- Fix: `@syncField` now properly applies room state once on connection
- Fix: Timeline reset previously active animation actions then being disabled (e.g. when switching to another active timeline) 

### Unity Integration
- Change: Improve warnings when not connected to the internet

## [3.37.6-pre] - 2024-04-26
### Needle Engine
- Fix: USDZExporter bug where geometry was getting duplicated on export when the same mesh was used multiple times
- Fix: USDZExporter duplicate export of scene start triggers
- Change: Improve USDZExporter animation export validation and improve handling of empty TransformData slots
- Change: Improve USDZ animation export allowing `RegisteredAnimationInfo` to also register a null clip for targeting the rest pose (e.g. empty state)
- Change: Improve USDZ time formatting
- Change: Invoke engine lifecycle hooks in the order in which they were registered (e.g. `onStart(ctx => ...)`)

### Unity Integration
- Add: DeployToFTP Server asset now has a `port` option that can be configured if necessary
- Add: DeployToNetlify does now render a info label while uploading
- Change: log error if trying to perform a distribution build without having a web project setup

## [3.37.6-exp] - 2024-04-24
### Needle Engine
- Add: More API documentation
- Add: VideoPlayer can now play `m3u` livestream links
- Fix: WebARBackground now checks if camera-access is granted
- Change: Progressively loaded assets now postfix urls with the content hash of the assets if available to make sure the correct version is loaded and not a old version from cache
- Change: VideoPlayer setting `url` now immediately updates the videoplayback

## [3.37.5-exp] - 2024-04-22
### Needle Engine
- Add: SceneSwitcher support for adding the `ISceneEventListener` on the sceneSwitcher gameObject
- Fix: OrbitControls should not update on user input when the camera is not currently active
- Change: OrbitControls middle click/double click does not change camera position anymore and just set the look target

## [3.37.4-exp] - 2024-04-22
### Needle Engine
- Fix: Collider filtermask bug where it did previously override membership settings in certain cases
- Change: Menu now removes the buttons for very small sizes
- Change: `this.context.physics.engine.raycast` and `raycastWithNormal` api changed to take an options parameter instead of single values. It now also exposes rapier's `queryFilterFlags`, `filterGroups` and the `filterPredicate` options. It can now be called with e.g. `this.context.physics.engine.raycast(origin, direction, { maxDistance: 2 })`

### Unity Integration
- Change: Move all Needle Engine components in `Needle Engine/` addComponent menu and improve searchability

## [3.37.3-exp] - 2024-04-19
### Needle Engine
- Fix: Regression in progressive mesh for multi material objects / multiple primitives per mesh
- Change: Improve LOD level selection based on available mesh density per level
  Level of detail switching now finds a good match for screen and mesh density that results in more consistent on-screen triangle density. This change also improves LOD switching for low-poly meshes considerably.
- Change: ScreenCapture now respects if user is in `viewonly` networked room

### Unity Integration
- Add: New "Better Cubemap" shader that allows to blur the skybox in the editor and change intensity.  
  You can upgrade from skybox materials that currently use `Skybox/Cubemap`.  
  Upgrade options are in the material inspector and on the `Camery Skybox Data` component in a scene.  
- Add: Improved image-based lighting workflow on 2023.x+ when using the new "Better Cubemap" shader

## [3.37.2-exp] - 2024-04-17
### Needle Engine
- Fix: Regression in USDZ export causing behaviours to stop working

### Unity Integration
- Change: WebXR component now exposes methods to start and stop a XRSession (`enterVR`, `enterAR`, `exitXR`)
- Change: WebXR component AR transform touch controls are now enabled by default

## [3.37.1-exp] - 2024-04-16
### Needle Engine
- Fix: Eventlist now handles EventListeners being added or removed during `EventList.invoke`
- Fix: Progressive LOD textures issue where compressed textures would not be loaded in some cases when using tiling
- Change: USDZExporter created by the WebXR component now enables `autoExportAnimation` and `autoExportAudioSources` by default

### Unity Integration
- Change: Improve multi scene workflow with additional loaded scenes that are also referenced. Multiple ExportInfo components are also better handled by using the first ExportInfo component in the currently active scene

## [3.37.0-exp] - 2024-04-15
### Needle Engine
- Add: USDZ physics export for VisionOS
- Add: Sprite `mesh` and `material` properties to simplify creating a new sprite object
- Fix: Loading files that don't have a `.glb` or `.gltf` extension but the correct mime type
- Fix: PostProcessing error when using tonemapping from vanilla threejs
- Change: Bump rapier dependency to ^0.12.0

### Unity Integration
- Fix: DeployToGlitch now using `needle.config.json` for build folder (support for e.g. Sveltekit deployment to glitch) 
- Fix: DeployToGlitch horizontal button layout

## [3.36.6] - 2024-04-12

### ⭐ Highlights

This release comes with numerous new features on our path to providing the best foundations for the spatial web.

We have rewritten our underlying WebXR support with ease of use and flexibility in mind, 
added a new cross-platform Menu component for quickly adding custom functionality to apps, and introduced a novel automatic mesh simplification and level of detail system. 

Needle Engine now supports VisionOS, with improvements to hand tracking, transient pointers, and support
for both Fully Immersive apps using WebXR, and Immersive Volume experiences based on Everywhere Actions and USD. 

We're also introducing the next step on our road to ubiquitous 3D content with automatic mesh simplification and sophisticated level of detail selection at runtime. 
Apps now are smaller, load faster, and run smoother – heavy models and large worlds benefit the most.

#### **New WebXR Foundations and API**
We've rewritten our WebXR API to be more intuitive, easier to use, and better integrated with the rest of the engine. XR controllers, hands, and eye tracking now flow seamlessly through our Event System, so that events like `onPointerClick` just work – no matter the input source.  

We've also made sure controllers are fully accessible for advanced use cases, with low-level access to the underlying WebXR API. 

Our new spatial preloader allows scenes to enter XR sessions more quickly, unlocking immersive navigation using `sessiongranted` (supported on Quest 2/3) for larger scenes.
Drag Controls now have support for multiple hands/controllers/touches and have modes for different interaction types. 

This new release also brings support for `mesh tracking`, `depth sensing`, `offer session`, a spatial debugging console, and more.
Existing scenes will upgrade to the new WebXR API automatically. 

#### **Needle Menu**
The new `Needle Menu` component allows for easy creation of custom menus in your apps. It brings together a number of often-used features like fullscreen, audio and networking settings, and sharing options under one unified user interface. 
Sharing experiences with others is now even easier through automatic QR code generation and Direct-to-Quest links.   

AR, VR and QuickLook buttons are integrated into the menu as well – and Needle Menu is supported in WebXR too. 

#### **Next Level Optimzation**
Needle Engine supported automatic compression and progressive loading for textures for a while, and now we're introducing automatic LOD generation, progressive loading, and runtime switching. 
Detail levels are chosen based on screen density, which means that in complex scenes only load mesh levels that are actually needed are ever downloaded. 
Additionally, automatic compression of meshes and textures paired with our compression cache is now fast enough to be enabled by default while working on projects, not just for production builds. 

#### **Better API documentation**
Besides readable source and documentation, we now have a dedicated API docs page that contains documentation for all previous and future versions: https://engine.needle.tools/api. 

#### **VisionOS support**
VisionOS is now a fully supported platform for Needle Engine.  

We support both Immersive Volume experiences and Fully Immersive (VR) experiences.  
Immersive Volumes are enabled by our Everywhere Actions and on-the-fly USD generation, and can even be shared via SharePlay to other users. Take a look at our collection of interactive USD samples at https://engine.needle.tools/projects/ar-showcase. 

While we had VisionOS support since day 1 due to building on open standards like WebXR, we improved the experience with better hand tracking, transient pointers for eye tracking, and performance improvements. 
A great example is https://engine.needle.tools/samples/bow-&-arrow/ – make sure to enable the WebXR flag in your Safari settings on VisionOS.

#### **Smarter FTP deployments**
When using our built-in FTP deployment, we now keep track of previously uploaded files.  
This makes repeat deployments much faster since it allows us to upload only those files that have changed.  

#### **Numerous Fixes and Improvements**
This release ships with hundreds of bug fixes and improvements in the runtime, build pipeline, and integrations. 
Thanks to all of our customers who send us feedback and reported bugs – we appreciate each and every report, keep them coming!    

Among the improvements are preload support for video and audio, better gizmo rendering and handling for lines, texts, meshes, and many new debug flags for an in-depth look at the engine's inner workings. We also ship experimental support for single-line PWA setup – more on that in a future release. 

### Needle Engine
- Add: Needle Menu can now create QR button
- Change: Needle menu fullscreen button now switches the `needle-engine` element into fullscreen
- Change: LODs are now switched at a slightly larger distance


## [3.36.6-pre] - 2024-04-10
### Needle Engine
- Add: Expose `setAutoFitEnabled` method to remove objects from being included in camera fitting
- Change: Improve WebAR wall and ceiling placement

### Unity Integration
- Fix: Issue where the menu item `Needle Engine/Build Production` creating a development build (if the Build Window `Development Build` checkbox was ticked)

## [3.36.5-pre] - 2024-04-09
### Needle Engine
- Add: lifecycle hooks like `onUpdate(()=>{})` now return method to unsubscribe. For example you can now write it like this `const unsubscribe = onUpdate(()=>{ console.log("One Frame"); unsubscribe(); })` 
- Add: `onAfterRender` hook
- Add: RemoteSkybox can now handle locally dropped files
- Add: `ObjectUtils.createSprite` method
- Fix: `<needle-engine camera-controls="0">` does now not create OrbitControls anymore if the assigned glTF file doesn't contain a camera
- Fix: RemoteSkybox doesn't prevent drop events anymore if the dropped file can not be handled

### Unity Integration
- Fix: Unity 2023 platform selection for embedded assets (Textures, Meshes)

## [3.36.4-pre] - 2024-04-05
### Needle Engine
- Add: More API documentation
- Add: `SceneSwitcher.addScene`
- Add: SceneSwitcher `scene-opened` event. Subscribe with `sceneSwitcher.addEventListener('scene-opened', args => {})`
- Fix: `OrbitControls.fitCamera` handle case where user passes in array with undefined entries
- Fix: Needle Menu not visible in AR overlay
- Fix: Contact Shadows should not render transparent objects
- Fix: API docs warnings (internal)

## [3.36.3-pre] - 2024-04-04
### Needle Engine
- Add: More API documentation for progressive loading, USDZExporter, getComponent methods etc
- Add: Needle Menu CSS for disabled buttons
- Add: Expose `onXRSessionStart` and `onXRSessionEnd` hooks
- Add: `isAndroidDevice` utility method
- Fix: Bounds calculation of SkinnedMeshRenderer with multi-material (multiple three skinned meshes in children)
- Fix: ContactShadows rendering for AR 
- Fix: WebAR touch transform does now ignore touches that start in top 10% of screen on android (e.g. when user is opening the menu by swiping down)
- Change: Needle Menu button height is clamped
- Change: Improve OrbitControls `fitCamera`
- Change: Needle asap now displays custom logo if assigned to `needle-engine` web component (requires PRO license)

### Unity Integration
- Add: `OrbitControls.fitCamera` method exposed

## [3.36.2-pre] - 2024-04-03
### Needle Engine
- Add and improve API documentation
- Add: `onXRSessionEnd` method hook
- Fix: Regression introduced by 3.36.0 causing stencil rendering to not work anymore
- Change: Move QR button method into `ButtonsFactory`

### Unity Integration
- Fix: minor issue where register type codegen would generate types for @example jsdoc comments

## [3.36.0-pre] - 2024-04-02
### Needle Engine
- Add: support for Auto LOD generation and runtime switching based on mesh density
- Add: support for progressive mesh loading which can reduce the initial download size significantly
- Add: BatchedMesh support for instancing
- Add: `Renderer.setInstanced(myMesh, true)` call to enable instancing for any Mesh
- Change: raycasting will now use lowpoly LOD which can reduce intersection checks significantly for high-poly assets

### Unity Integration
- Add: support for Auto LOD generation.  
  - generating LODs can be disabled for a complete project using the `ProgressiveLoadingSettings` component or in the mesh import settings

## [3.35.1-pre] - 2024-03-29
### Needle Engine
- Add: Animator api for `getCurrentStateInfo()` and `currentAction` getter which returns the currently playing three action.
- Internal: use `compileAsync` for prewarming newly loaded objects

### Unity Integration
- Fix exception caused by pnpm folder check

## [3.35.0-pre] - 2024-03-25
### Unity Integration
- Fix: sub asset inspector should not create the data object asset when platform import-options are selected
- fix: AssetSettingsInspector should handle submeshes in PackageCache - the data object asset is then created in `Assets/Needle/ImportSettings`

## [3.35.0-exp] - 2024-03-22
### Needle Engine
- Add: Support for `transient-pointer` input sources for VisionOS
- Add: add metadata and intersections to `NEPointerEvent` type. Intersections are filled in from EventSystem. This information can be access via `this.input.addEventListener` or the input event callbacks
- Change: `ObjectUtils.createPrimitive` now has types strings e.g. `ObjectUtils.createPrimitive("Cube")`
- Change: ChangeMaterialOnClick doesnt require a Renderer component anymore

## [3.34.4-exp.1] - 2024-03-21
### Unity Integration
- Fix: Explictly use `@needle-tools/gltf-build-pipeline` v1.3.1 until 1.4 is out
- Change: Update UnityGLTF to 2.10.2-rc fixing exception when animating a missing material
- Internal: clarify EditorSync warning and minor warning fix

## [3.34.4-exp] - 2024-03-20
### Needle Engine
- Fix: OrbitControls autoFit frame delay causing a wrong perspective for one frame
- Fix: Multiple WebXR components causing menu item icons to appear multiple times

### Unity Integration
- Fix: Generate lightmaps on export if the scene is using baked lighting but no lightmaps exist yet
- Fix: Improve SamplesWindow tags and search filtering to restore state after recompile
- Change: Clarify some logs regarding local server and export from an scene without ExportInfo

## [3.34.3-exp.1] - 2024-03-19
### Needle Engine
- Add: debug on-screen console (`?console`) now has a tab to inspect the scene
- Fix: TransformGizmo component preventing OrbitControls input

### Unity Integration
- Fix: workaround for welcome window exception

## [3.34.3-exp] - 2024-03-18
### Needle Engine
- Add: `context.maxRenderResolution` clamping the max renderer size
- Add: `PointerEventData` now expose raw three.js Intersection object (e.g. in onPointerDown)
- Add: `addComponent` can now take an optional init parameter which can be used to set default values during creation of the component instance, modify options or assign fields. The init parameter is fully typed and only shows available options
- Fix: Issue where async import of Needle Engine breaks registering custom gltf extensions registered from local packages. Needle Engine now waits for all dependent packages to be ready
- Change: Bloom effect defaults to normal Bloom effect for performance reasons (previously it was using SelectiveBloom by default). To change this back set `Bloom.useSelectiveBloom = true` in global scope
- Change: Volume component now exposed `effects` array which is a list of the currently active postprocessing effects
- Change: NEPointerEvents now create a threejs Ray when accessing the `ray` property and no ray was created before
- Change: Allow audio playback when keyboard input is detected
- Change: Gizmos now ignore fog
- Change: Improve `getComponent` api types. For example previously `addComponent` did return the generic `IComponent` interface instead of the concrete type (e.g. `Animator`)
- Change: Deprecate `addNewComponent` because it is redundant. You can just use `addComponent`

### Unity Integration
- Internal: update materials to 2021.3 serialization format

## [3.34.2-exp.2] - 2024-03-13
### Needle Engine
- Fix: vite build pipeline plugin should wait for output directory up to 10 seconds
- Fix: input event button index for mouse on pointer move
- Fix: basic styles for links inside needle menu

### Unity Integration
- Fix: EditorSync settings serialization causing error on export
- Internal: project generator should not change local paths in web project

## [3.34.2-exp] - 2024-03-13
### Needle Engine
- Fix: issue where asap plugin path would be falsely resolved causing a vite build error
- Fix: serializable warning in WebARCameraBackground and Animation components
- Change: set Needle Menu zindex to 1000

### Unity Integration
- Change: Remove legacy support for Versions lower than Unity 2021.3
- Change: TransformControls component can now be disabled
- Change: Opening workspace now updates Needle Engine folder display name
- Change: Update vite workspace template removing wrong compile-hero setting

## [3.34.1-exp.1] - 2024-03-12
### Needle Engine
- Fix: Input regression when querying mouse button states (e.g. `getPointerClicked(1)` for the middle mouse button)

### Unity Integration
- Add: Expose OrbitControls `autoTarget` option

## [3.34.1-exp] - 2024-03-12
### Needle Engine
- Add: OrbitControls exposing min/maxPolarAngle and min/maxAzimuthAngle
- Fix: Input should subscribe to pointer events (`pointerdown`, `pointermove`...) instead of touch and mouse. For fixing iOS and AR issues where pointerIds are stuck and we have wrong state
- Fix: Button hover state when dragging with right mouse button
- Fix: Vite build plugin error when needle gltf build pipeline package doesnt exist

### Unity Integration
- Add: OrbitControls exposing min/maxPolarAngle and min/maxAzimuthAngle
- Fix: ensure local dependencies are added to the workspace when opening it
- Fix: catch some exceptions during tests when scene has no ExportInfo
- Fix: Hidden tools project check if installation is really finished

## [3.34.0-exp.3] - 2024-03-11
### Needle Engine
- Fix: TypeScript warnings in XR components
- Fix: Add correct @type for `Button.animationTriggers`
- Fix: Incorrect serialization warnings for fields marked with `@serializable()`

### Unity Integration
- Fix: Compilation error on 2020.3.x
- Change: Open typescript files in main workspace if they're a dependency
- Change: Always add NpmDef dependencies to workspace

## [3.34.0-exp.2] - 2024-03-07
### Needle Engine
- Fix: Partially revert alias plugin changes to fix issues with md5 package
- Fix: Check if asap exists before referencing it

### Unity Integration
- Fix: Remove URP light/camera components when samples are opened on BiRP and components are missing

## [3.34.0-exp] - 2024-03-06
### Needle Engine
- Add: Vite plugin for showing indicator that needle engine bundle is still loading (Needle ASAP)
- Add: Vite plugin for build info now includes file hash so we can skip uploading unchanged files
- Add: `this.context.input.addEventListener` now exposes options for `once` (remove event listener after the first invocation) and `signal` (remove event listener when signal is aborted)
- Add: `this.context.time` now has `deltaTimeUnscaled`
- Fix: Issue where Needle Menu size would sometimes switch between compact and stretched view
- Fix: Improve icon rendering while icon font is still being loaded
- Fix: Ensure NeedleXRSession does only subscribe once to `session granted`
- Fix: Vite alias plugin now explictly resolved three exports for addons and nodes
- Fix: wrong colorspace in scene lighting texture
- Fix: Show Needle Menu in AR dom overlay
- Fix: LOD layers are now properly set (e.g. it's now possibly to disable raycasting via layer 2)
- Fix: Issue where timescale would affect XR movement
- Change: Bump three.js version to 0.162
- Change: Bump postprocessing to 6.35.1

### Unity Integration
- Add: DeployToFTP now skips uploading unchanged files based on data stored in `needle.buildinfo.json` (this is automatically generated via a vite plugin during building)
- Fix: Issues in Unity 2022 due to Unity regression in inspector height calculation
- Change: Bump UnityGLTF to version to 2.10.0-rc.2 (latest)
- Change: Update CollabSandbox Scene Template
- Change: Remove Needle Engine Trial limitations (previously affecting AnimatorController and Timeline export)

## [3.33.0-pre] - 2024-03-02
### Needle Engine
- Add: Needle Menu can now create `Mute` and `Fullscreen` buttons.
- Add: Needle Menu buttons now have icons (configuration options will be added in a future version)
- Add: Needle Menu `postMessage` support to inject buttons to open an URL
- Add: SyncedRoom now creates a `Join Room` button to the menu (or `Leave Room` button)
- Add: WebXR support for the Needle Menu
- Add: Pre-XR room when user enters via `sessiongranted` when the main content is still loading
- Add: OneEuroFilter `reset` method
- Fix: Resume AudioContext when interrupted (happens e.g. on VisionOS when entering VR)
- Fix: GroundProjection should not be visible when in pass-through AR
- Fix: XRController ray and hit rendering now correctly respects rig scale
- Fix: Button with state transition colors now correctly work with alpha values
- Fix: Issue where the Needle Menu was visible without any button or content
- Change: Spatial console becomes visible when an error happens on local server while in XR

### Unity Integration
- Fix: Generate the correct template when user selects a template in the ExportInfo and then click the Unity Play button
- Change: Improve the custom shader inspector for when the shader is a subasset or in an immutable package

## [3.32.28-pre] - 2024-02-28
### Needle Engine
- Fix: AR placement for Chrome 122

### Unity Integration
- EditorSync inspector now shows uninstall button while server is running

## [3.32.27-exp] - 2024-02-26
### Needle Engine
- Fix: Quicklook export caused by typo in USDZ mime-type
- Fix: Workaround iOS/visionOS bug: always include the tap trigger for audio even if nothing to tap on
- Change: Input `addEventListener` can now take options as a third argument

## [3.32.26-exp] - 2024-02-23
### Needle Engine
- Add: Needle Menu. This first version will contain the WebXR options for now. Future versions will allow for more configuration
- Fix: webpack based project code optimization. This will improve e.g. nextjs production builds
- Fix: Raycasting offset in AR for touches near the screen border
- Fix: Spatial console text z fighting
- Fix: PostProcessing Exposure not working anymore with tonemapping
- Change: `NeedleWebXRHtmlElement` has been renamed to `WebXRButtonFactory`
- Change: bump postprocessing package to `6.34.3`
- Change: Improve PostProcessing when tonemapping is enabled

### Unity Integration
- Change: Automatically enable progressive textures when overriding TextureImporter settings for Needle Engine platform

## [3.32.25-exp] - 2024-02-22
### Needle Engine
- Fix: WebXR switching between controllers and hands
- Fix: WebXR `onControllerAdded` being called twice at the start of a session
- Fix: WebXR buttons container should not capture pointer events
- Fix: Improve spatial console (visible in XR with `?console` in the URL)
- Fix: Allow audio playback once a XRSession starts
- Fix: USDZ fill in missing transform data when multile animation clips on the same object have different detected animation roots
- Fix: generated USDZ mime type
- Change: new cleaner loading screen

### Unity Integration
- Change: ExportInfo.AutoCompress now also generates progressive textures for local dev

## [3.32.24-exp] - 2024-02-21
### Needle Engine
- Add: Spatial debug console, add `?console` url parameter to get a debug console in XR floating before the camera view
- Add: OpenURL component support for opening email addresses (without `mailto:` prefix, just enter your email address in the url field)
- Add: Progress api for performance logging (use via `Progress.start`, `Progress.report` and `Progress.end`)
- Fix: error when using RenderTextures
- Fix: USDZ text material missing
- Fix: USDZ prevent unclear animation export error
- Fix: USDZ improve endTimeCode calculation
- Fix: USDZ improve audio export, implicit register audio sources with scene start triggers
- Change: USDZ unpremultiplied texture readback on chrome
- Change: Expose DragControls.DragMode enum

### Unity Integration
- Change: bump helper package dependency

## [3.32.23-exp] - 2024-02-19
### Needle Engine
- Add: `FileReference` support to reference, export and load almost any file:
  ```ts
  @serializable(FileReference)
  myFile?: FileReference;
  ```
- Add: initial support for [building PWAs with needle engine](https://engine.needle.tools/docs/html.html#creating-a-pwa) and `vite-plugin-pwa`
- Fix: false check in VideoPlayer `videoElement` getter

### Unity Integration
- Fix: WebXR component false warning for missing Avatar component
- Change: improve WebXR component DX (for example when the WebARSessionRoot component is present in the scene the WebXR component will render the correct `userScale` value)

## [3.32.22-exp] - 2024-02-16
### Needle Engine
- Add: Needle build-pipeline plugin to run compression as part of the vite build process
- Fix: Loading of progressive textures in Canvas UI
- Fix: Loading of progressive textures in Spritesheet renderer
- Change: SpritesheetRenderer does now directly apply rounded sprite index (instead of relying on the value to be an integer)

### Unity Integration
- Change: Vite projects are now running `build:dev` for production builds as well since the compression is handled by the new needle build-pipeline vite plugin
- Change: Update UnityGLTF dependency from 2.8.1-exp to 2.9.1-rc
- Change: Click on `Install` in ExportInfo now runs install in all locally installed packages (e.g. all references npmdef packages)

## [3.32.21-exp] - 2024-02-16
### Needle Engine
- Add: USD `displayName` support for nodes and materials
- Fix: input double click data on iOS
- Fix: USDZ material indenting
- Fix: check if `linearVelocity` exists on XRPose to prevent typecheck error in CI environment
- Change: export USDZ materials, geometry and textures with proper names related to their original names
- Change: improve USDZ opacity and opacityThreshold conversion, add minimal alphaHash support since QuickLook seems to allow that now

### Unity Integration
- Fix: ensure supported three.js is installed in needle engine web projects
- Change: WebXR component now shows infos about assigned avatar and shows a `Fix` button

## [3.32.20-exp] - 2024-02-10
### Needle Engine
- Fix: WebXR buttons should catch exception when accesing `navigator.xr` which can happen in iframe without spatial-tracking permissions
- Fix: Issue where `getPointerDelta(pointerId)` did not return data when using multitouch except for the first touchpoint
- Fix: Issue where `backgroundBlurriness` and `backgroundIntensity` would not be reset to the default when enabling a camera that didnt have explicit settings (NE-4243)
- Change: `getComponentsInChildren` and `getComponentsInParents` now clear the optionally provided buffer arrays by default

## [3.32.19-exp] - 2024-02-09
### Needle Engine
- Fix: CanvasGroup causing performance to drop over time
- Change: Expose `createNewRenderer()` on context

## [3.32.18-exp] - 2024-02-09
### Needle Engine
- Add: vite plugin for collecting build information
- Fix: USDZExporter not respecting button option so `Open in Quicklook` button was always created
- Fix: vite plugin for copying files didnt respect needle.config.json build directory
- Fix: AnimatorController exception happening in deepClone (NE-4227)
- Fix: minor false warning log for deserializing postprocessing effects
- Fix: vite dependency watcher plugin should handle version containing alias like `npm@three@160`
- Fix: NeedleXRSession feature support should catch exception that might happen when running inside iframe with insufficient permissions
- Change: ARSessionRoot align placed scene to camera during placement preview and actual placement so they match up
- Change: bump postprocessing package to 6.33.4 for three 160 support
- Change: vite plugin dependency watcher should now reload website when the server has changed due to changed package.json dependency

### Unity Integration
- Fix: DeployToFTP component respecting needle.config.json
- Change: ExportInfo `Open Workspace` button now opens Readme in project directory by default (if it exists)

## [3.32.17-exp] - 2024-02-08
### Needle Engine
- Fix: visionOS input handling and hands rendering
- Fix: Error during USDZ text export
- Fix: vite plugin error for cases where config object was missing
- Fix: Rare issue where re-entering AR causes error due to missing reticle
- Change: component `onDestroy` is now called before Object3D and resouces are disposed/destroyed
- Change: WebARSessionRoot z-forward looks now towards user/camera

### Unity Integration
- Fix: DeployToFTP URL encoding when opening url
- Fix: Fog not exporting for scenes without GltfObject component
- Fix: Issue in ComponentGenerator component when no ExportInfo is in scene
- Change: BugReporter description can now be cancelled
- Change: Try fix issue where npm tools package fails to install
- Change: Try improving license check for cases where the internet connection is lost

## [3.32.16-exp] - 2024-02-07
### Needle Engine
- Add: NeedleXRController `isHand` property
- Add: WebARSessionRoot `customReticle` to allow how the AR session placement looks
- Add: NeedleXRController `getHandJointPose(jointName)` API
- Add: Static `NeedleXRSession.onXRSessionStart` and `onXRSessionEnd`
- Fix: Custom Avatar was despawning when not in multi-user session (added `dontDestroy` flag to PlayerState)
- Fix: USDZExporter without WebXR component in scene does now again create quicklook button
- Fix: Bug in Avatar where assigning head or hands objects would cause errors
- Fix: Hide XR buttons during running session
- Fix: Bump threejs version to fix OrbitControls not handling `pointerup` if it doesnt happen over the passed in target element
- Change: NeedleXRController does now not emit `pointermove` event every frame but only when above a set position/rotation threshold
- Change: time.smoothedFPS is now smoothed over 60 frames
- Change: loaded GLB name in loading overlay is now less technical
- Change: static `NeedleXRSession.onXRStart` is now `onXRSessionStart`

## [3.32.15-exp] - 2024-02-06
### Needle Engine
- Fix: Hands rendering on visionOS
- Fix: NeedleXRController now supports pinch gesture and emits pinch event for devices that don't properly implement the WebXR API and don't invoke the `selectstart` events (e.g. visionOS)
- Fix: USDZExporter should not use `doubleSided` for skeletal meshes
- Fix: safeguard against null reference error in DragControls.alignManipulator
- Fix: Gizmo labels being raycastable
- Fix: Gizmo cache stopped working due to wrong `isDestroyed` check
- Fix: Error caused by Canvas UI when starting XR session
- Fix: `onPointerEnter` and `onPointerExit` is not called for all controllers

## [3.32.14-exp] - 2024-02-04
### Needle Engine
- Add: Renderer `sharedMeshes` property to easily access all mesh objects that belong to the renderer
- Fix: WebXRController ray rendering frame delay
- Fix: Avoid WebXRHand model sometimes not being properly cleaned up
- Fix: minor console.log fixes

### Unity Integration
- Fix: compiler error on windows

## [3.32.13-exp] - 2024-02-03
### Needle Engine
- Add: AudioSource `preload` property
- Fix: BoxCollider now automatically detects changes on `scale` property and updates underlying physics engine collider size
- Fix: error in mobile VR touch (without physical controllers)
- Change: Calls to `instantiate` now don't accept a null or missing object to instantiate anymore
- Change: Rigidbody component now updates underlying physics properties immediately when dirty before invoking `applyForce()` or `applyImpulse()`

### Unity Integration
- Add: AudioSource `preload` property
- Change: Samples Window now shows installed version
- Change: Warn if installed samples are out-dated or not supported with the current Needle Engine version

## [3.32.12-exp] - 2024-02-01
### Needle Engine
- Fix: ParticleSystem modifying assigned material instance in some cases
- Fix: PostProcessing DepthOfField effect api change in `postprocessing` package
- Fix: nextjs production builds with needle-engine
- Fix: `input.addEventListener` for key events stopped working
- Fix: issue in physics async draincollision callback sometimes failing when objects were already destroyed
- Fix: WebXR controller and hand models should not be destroyed with the XRRig 
- Change: WebXR on non-secure connections now shows warning and button for WebAR and WebVR are disabled

### Unity Integration
- Add: Support for Build Window `preview` build button for nextjs projects
- Add: nextjs template to templates dropdown
- Fix: issue where tools project generation ran multiple times

## [3.32.11-exp] - 2024-01-30
### Needle Engine
- Add: various more documentation comments
- Add: NeedleXRController now exposes gripspace [`linearVelocity`](https://developer.mozilla.org/en-US/docs/Web/API/XRPose/linearVelocity)
- Fix: input event causing error due to missing pointerId
- Fix: DragControls not checking if an event was already used
- Fix: EventSystem calling input event methods on disabled components
- Change: Calculate worldspace data only once per frame in NeedleXRController

### Unity Integration
- Add: expose collider [membership and filter group](https://rapier.rs/docs/user_guides/javascript/colliders#collision-groups-and-solver-groups) options
- Fix: prevent rare error caused by empty/missing entries in UnityGLTF plugins

## [3.32.10-exp] - 2024-01-30
### Needle Engine
- Add: XRControllerFollow option to follow gripspace or rayspace
- Fix: ParticleSystem trail not rendering if assigned material culling was set to front
- Fix: ParticleSystem incorrect InheritVelocity when creating a new instance
- Fix: NeedleXRController index in NeedleXRSession.controllers array now matches the `NeedleXRController.index` (the index of the inputdevice)
- Change: `?stats` url parameter now also shows FPS in WebXR session
- Change: `this.context.xr` now contains the whole type information

## [3.32.9-exp] - 2024-01-29
### Needle Engine
- Add: various more documentation comments
- Add: pointer events now expose `pressure` property
- Fix: `pointerCapture` now works with all buttons
- Fix: raycast call did not skip hidden objects
- Fix: XRController hit rendering does now skip SkinnedMeshes for performance reasons
- Change: touchup did emit `onPointerExit` every time, it now only happens if the touch hits another object (or none)
- Change: `pointerId` now is a unique id generated from device-index + button-index, input events now also expose a `deviceId` property
- Change: EventSystem optimization for skipping raycast for e.g. `pointermove` event on objects that don't have a component that implements the `onPointerMove` method

## [3.32.8-exp] - 2024-01-29
### Needle Engine
- Fix: AudioSource error when creating three's PositionalAudio object where GameObject was missing
- Fix: Collider center offset being falsely applied resulting in wrong object placement
- Fix: Physics debug visualization should be updated in post physics step fixing a visual frame delay
- Fix: WebXR AR/VR buttons are now always created

## [3.32.7-exp] - 2024-01-28
### Needle Engine
- Add: first version of `setPointerCapture` and `releasePointerCapture` that acts similarly to [HTML pointer capture](https://developer.mozilla.org/en-US/docs/Web/API/Element/setPointerCapture) and can be used to receive onPointerMove events when the pointer has left the object until it is either released or `onPointerUp` happens (this currently only works with the primary button)
- Fix: Remove console.log in `instantiate` call
- Change: used pointer events will still be propagated to all components
- Change: reduce default Gizmos.Label size

### Unity Integration
- Fix EditorSync, bump dependency
- Change: EditorSync editor now shows warning if `Auto Compress` for local development is enabled. With compression the required extensions that are only used for development are stripped and therefore `Auto Compress` needs to be enabled while using Editor Sync.

## [3.32.6-exp] - 2024-01-27
### Needle Engine
- Fix: `AssetRefernence.instantiate` does now clone instantiate options before awaiting asset loading
- Fix: `onPointerMove` event was not being called in XR
- Fix: Timeline paused but evaluated should still start audio playback e.g. when controller through scroll

### Unity Integration
- Fix: avoid call to `SessionState.GetBool` on other threads when generating the tools project

## [3.32.5-exp] - 2024-01-26
### Needle Engine
- Fix: issue where `onEnterXR` callback was possibly not being invoked on all scripts if a script was removed or deleted during onEnterXR (and the underlying array was modified) causing e.g. the CollaborativeSandbox AR placement to not work
- Fix: minor issue where controller ray visualization would not respect rig scale

## [3.32.4-exp] - 2024-01-26
### Needle Engine
- Add: NeedleXRSession `fadeTransition()` that can be used to cover teleportation. It returns a promise that is resolved when fade to black has completed
- Fix: error in next template caused by wrong internal imports
- Fix: WebXR should not show quicklook button when `useQuicklookExport` is disabled
- Change: debug mobile console now also captures errors at load time, mobile console shows automatically on quest browser (for local development)

## [3.32.3-exp] - 2024-01-26
### Needle Engine
- Add: `origin` field to NEPointerEvent which references the object that raised the event (e.g. the XRController)
- Add: `delayForFrames(numOfFrames)` util method returning a promise that will resolve after the given amount of frames (equivalent to `delay` which will take a time in milliseconds)
- Fix: prevent access to geometry of a destroyed mesh in physics call
- Fix: AR passthrough placement with controllers
- Fix: issue where AR could not be started twice
- Change: AR placement fallback to camera placement if controller hit-test is not available (e.g. when using Quest simulator)

## [3.32.2-exp] - 2024-01-26
### Needle Engine
- Add: expose options to disable XRController rays, hit points and teleport on the XRControllerMovement component
- Add: NeedleXRController `emitEvents` to disable input events for controllers
- Fix: Cases where XRController button `isDown` and `isUp` wasn't updated for `primary` and `squeeze` buttons.
- Change: WebXRButton container zIndex is now 5000

### Unity Integration
- Fix: DefaultAvatar hands model
- Change: XRControllerMovement minimal turn angle is no 0 to disable rotation
- Change: ComponentGenerator.debug will now include more information regarding node.js not being found
- Change: ComponentGeneratorRunner will now use session state for determining if node is installed
- Change: Bump EditorSync package dependency to `2.0.2-beta`

## [3.32.1-exp] - 2024-01-25
### Needle Engine
- Add: NeedleXRController `getButton()` now returns an enhanced GamepadButton object which contains bools for `isUp` and `isDown`
- Add: Access to NeedleXRSession via `this.context.xr` 
- Fix: XR lifecycle issue where script became inactive during onEnterXR
- Fix: XR Avatar component for synchronization should not log an error when not connected to networking backend
- Fix: XR renderOnTop option for worldspace UI
- Fix: XR screenspace UI should not render for now
- Fix: change event argument for `space` (input in 3D space) is now of type `IGameObject` to expose `worldPosition` etc
- Fix: `offerSession` should request AR mode when only `showAR` button is enabled in WebXR component
- Change: calls to `instantiate(prefab, {})` can now be invoked with anonymous options object as second parameter, for example `instantiate(prefab, { parent: myParent })`
- Change: the SyncTransform component does now automatically throttle `fast` mode (reducing the frequency of updates) when running on the glitch backend and when having set more than 10 components to `fast`

## [3.32.0-exp] - 2024-01-24
### Needle Engine
- Add: new **Needle WebXR** system 
  - Core component event methods give much easier access to the XR system (e.g. `onEnterXR` or `onXRControllerAdded`)
  - Easy access to XRSystem data like `gripWorldSpace` or controller buttons using the `NeedleXRController` class   
  - Default functionality like movement, teleport and rendering of controllers or hands is now encapsulated in separate components that can easily be enabled or disabled or overriden. 
  - All XR input events now go through the event system and can be received on components using the input event methods (like `onPointerDown`)
  - Support for `offerSession` for QuestBrowser
  - Support for new `depth-sensing` in Quest pass-through mode
- Add: much improved `DragControls` offering different modes for screen and XR interaction, snapping or XR distance grab
- Change: **Update three.js to 0.160**
- Change: pointer events don't need the `IPointerEventHandler` interface anymore - they're now already available on the core `Behaviour` class via e.g. `onPointerDown`
- Change: for networked avatars add a `PlayerState` and a `Avatar` component to your avatar prefab

### Unity Integration
- Add: `UnityEngine.TextAsset` references will now be copied to the output directory
- Change: update Needle Engine to the new XR system

## [3.31.0] - 2024-01-23
### Needle Engine
- Fix: `isDesktop()` util method should return false on iPad

### Unity Integration
- Bump Editor-Sync dependency fixing issue with changed engine API
- Change: update button now warns when updating to a pre-release version

## [3.30.0] - 2024-01-18
### Needle Engine
- Add: VideoPlayer `preload` method to start loading the video file without having to start playback
- Add: VideoPlayer support for m3u8 video stream format (set the mode to URL and assign the video streaming url)
- Add: SceneSwitcher `autoLoadFirstScene` option
- Add: `this.context.connection` now has a getter for current websocket url
- Add: USDZExporter option to set max texture size
- Fix: SceneSwitcher should not load it's own scene again causing recursive loading
- Fix: Gizmo lines being culled sometimes
- Fix: Gizmo parented to another object should not be returned to cache if it got destroyed while being rendered (e.g. Gizmo.Label)
- Fix: prevent destroyed component from being added to an object again
- Change: Physics.raycast does now ignore lines by default (you can pass in a custom line threshold >= 0 to override that)

### Unity Integration
- Fix: Add guarding against trying to reference the root web project scene from a referenced sub-scene
- Fix: Exception when calling FindObjectOfType from different thread
- Change: Custom Shader setting is now at the top of a material
- Change: Update UnityGLTF to [2.8.1-exp](https://github.com/prefrontalcortex/UnityGLTF/blob/dev/CHANGELOG.md#281-exp---2024-01-18)

## [3.29.0] - 2024-01-08
### Needle Engine
- Add: `isDesktop` util method
- Add: physics engine now has `debugRenderRaycasts` boolean or `debugraycasts` url parameter to visualize raycasts
- Fix: issue where Gizmos would be rendered for more than one frame or the expected time 
- Fix: issue in physics engine `sphereOverlap` where dynamic rigidbodies would not be captured
- Change: more stable guid generator on initial scene load 
- Change: Collider `membership` and `filter` can now be undefined for default collision groups (all enabled)
- Change: `Collider.filter` is now set to undefined to include all groups in the filter (allow collision with all groups)
- Change: vite meta plugin to write secure url

### Unity Integration
- Fix: DeployToGithubPages now correctly opens github pages url for `.git` urls
- Bump UnityGLTF to 2.7.1-exp

## [3.28.8] - 2024-01-04
### Needle Engine
- Fix: pre-bundled version should contain Needle Engine version
- Change: generate component guids based on original guid for initial scene load per component for more stable guids across local and deployed versions and where the order of components in the scene will not affect the guid for components anymore

### Unity Integration
- Change: improve editor typescript link rendering, labels are displayed with higher contrast and the package where a typescript file is located is now always displayed

## [3.28.7] - 2024-01-02
### Needle Engine
- Add: SceneSwitcher `progress` event, `currentLoadingProgress` and `currentlyLoadingScene` properties exposing loading progress. Passing `sceneSwitcher` instance as first argument to `sceneOpened` callback
- Fix: Creating the renderer should not modify the static `Context.DefaultWebGLRendererParameters`, this caused multipage sites to not find the correct canvas anymore (e.g. causing sveltekit sample to not work anymore when changing back and forth between pages)

### Unity Integration
- Add: `Custom Template` option when creating a new web project allowing to just paste in a github url to pull the project from
- Fix: Preview Build should not open localhost url `/true`
- Fix: Build Window Deployment buttons

## [3.28.7-beta.1] - 2023-12-30
### Needle Engine
- Fix: Revert AnimatorController condition evaluation change

## [3.28.7-beta] - 2023-12-30
### Needle Engine
- Fix: Issue in AnimatorController evaluation of bool condition where the threshold wasnt taken into account (e.g. only making a transition if a bool parameter was set to false)

## [3.28.6-beta] - 2023-12-30
### Needle Engine
- Add: EventList serialization can now have multiple arguments allowing support for e.g. `setBool` on Animator component being called from a Button directly
- Add: Animator `toggleBool` method 
- Fix: Issue where destroying an object in `onCollisionEnter` it would not be removed from the physics event queue and result in `onCollisionStay` being called with an already destroyed component
- Fix: Rigidbody matrix changed watcher should ignore events during physics to threejs synchronization
- Fix: Set gizmos renderOrder to be always rendered last to avoid cases where gizmos are hidden due to custom renderOrder on scene objects

### Unity Integration
- Fix: Issue where codegen would produce invalid type registration code when the typescript class would not contain any space after the name
- Improve DeployToFTP logging an error when the tools package could not be found 

## [3.28.5-beta] - 2023-12-22
### Needle Engine
- Fix: WebXRController falsely triggering click in EventSystem

### Unity Integration
- Fix: Timeline window opened during export for evaluation should now be automatically closed again

## [3.28.3-beta] - 2023-12-21
### Needle Engine
- Fix: Issue in pointer events not triggering onPointerEnter and Exit in VR
- Remove: warning log in USDZ export about double sided materials not being supported

### Unity Integration
- Fix: Issue where incomplete types information was generated for the component compiler
- Fix: Issue regarding generics in typescript types not being parsed correctly by Unity

## [3.28.2-beta] - 2023-12-20
### Needle Engine
- Add: Support double sided material export for USDZ
- Fix: `PlayAudioOnClick` when explicit clip is given, use loop from attached audio source
- Change: Improve Everywhere Action `SetActiveOnClick`

### Unity Integration
- Change: On OSX local server now also starts in external terminal window

## [3.28.1-beta] - 2023-12-18
### Needle Engine
- Add: `context.recreate` to destroy the whole scene and reload everything (including all script instances)
- Fix: issue where EventSystem pointer events would not be received anymore if the event component was on e.g. an empty object in the parent hierarchy

## [3.28.0-beta] - 2023-12-14
### Needle Engine
- Fix: USDZ: `emissiveIntensity` was not applied and `emissiveColor` wasn't used for scale/bias properly
- Fix: USDZ: compressed textures with alpha channel were not being read back correctly
- Change: USDZ: bake `effectiveOpacity` into the opacity texture if needed, since QuickLook/usdview don't support `.a` scale values
- Change: Bump UnityGLTF version to 2.6.0-exp to support GPU Instancing on imported glTF materials

### Unity Integration
- Add: enable GPU instancing support for custom shaders
- Fix: prevent NullRef in `TextureImportSettings`

## [3.27.5-beta] - 2023-12-12
### Needle Engine
- Fix: Offscreencanvas support for iOS 16.x
- Fix:  `PlayAudioOnClick` now respects playOnAwake if an explicit audio source is assigned
- Change: `EventSystem` should only check objects if they're meshes

## [3.27.4-beta] - 2023-12-11
### Needle Engine
- Fix: instancing now updating bounds in Needle Engine before render callback if necessary
- Change: expose `onPauseChanged` on components
- Change: handle case where a added coroutine function is not a coroutine (Generator) 

### Unity Integration
- Change: clean install only stops certain node processes

## [3.27.3-beta] - 2023-12-09
### Needle Engine
- Add: `InstancingUtil.getRenderer` to get the three InstancedMesh for any Object3D (if it's using instancing)
- Add: instancing does now automatically update culling bounds if it's dirty. This can be disabled via `InstancingUtil.setAutoUpdateBounds(obj, false)`
- Add: Rigidbody method documentation
- Add: `ParticleSystem.addBehaviour` method and expose underlying particle system. We now also export the particle types
- Fix: Issue where `this.physics.raycastFromRay` was modifying the default raycast options
- Fix: Issue where sprites would be falsely interpreted as builtin sprite causing the image to be not displayed correctly
- Change: `this.physics.raycast()` can now be called with anonymous options (instead of having to use a `RaycastOptions` class), for example `this.physics.raycast({ray:myRay})`

### Unity Integration
- Fix: npmdef component generation with abstract classes

## [3.27.2-beta] - 2023-12-08
### Needle Engine
- Fix: don't set font on `<needle-engine>` host styles to prevent leaking into child elements
- Fix: `AssetReference.loadAsync` being called multiple times should always return the `asset` result
- Change: register OrbitControls events on the canvas and not the needle engine element to allow child HTML objects to capture input

### Unity Integration
- Fix: error when saving scene without a Needle Engine component
- Fix: resolve symlinks when opening vscode workspace on windows for VSCode to correctly suggest imports when editing code that is installed by local file path
- Fix: reduced FileWatcher count for OSX where this caused issues in mono
- FIx: minor editor UI layouting issues in ExportInfo and BuildWindow on OSX
- Change: re-generate component compiler types more frequently

## [3.27.1-beta] - 2023-12-06
### Needle Engine
- Add `addCustomExtensionPlugin` API to register custom glTF importer and exporter extensions
- Fix: issue where instanced and animated object was rendered for one frame with a wrong matrix
- Change: expose `imageToCanvas` method from USDZExporter
- Change: GltfExporter component does not cache exporter anymore and expose all exporter options

### Unity Integration
- Fix: Welcome Window `ShowWindowAtStartup`
- Change: validation of lightmap encoding is now only displayed when lightmaps are being used
- Internal: Npmdef peerDependencies are now automatically set to local package for development

## [3.27.0-beta] - 2023-12-04
### ⭐ Highlights
#### **Skinned Mesh support for USDZ export**
Our runtime USDZ export now supports skinned meshes and animations. This allows you to export animated characters to USDZ for AR on iOS/visionOS devices. Additionally, we're now handling animator state transitions much smarter on export, leading to improved consistency between browser runtime and QuickLook.

### Needle Engine
- Add: USDZ: skinned mesh export, including animations
- Add: USDZ: ability to specify if we're exporting for QuickLook or not
- Add: USDZ: animation export respects basic root motion (translation and rotation)
- Add: USDZ: `USDZExporter` API supports exporting binary buffer similar to `GLTFExport` API
- Fix: USDZ: correct `defaultPrim` encapsulation so that it also contains materials
- Fix: USDZ: render texture readback failure when exporting
- Fix: `ISceneEventListener` only being found when first component
- Fix: error in vite plugin facebook instant games if no config would exist
- Fix: Gizmo label padding and border radius were not properly applied when re-using label
- Change: USDZ: animation export now automatically includes Animator states
- Change: USDZ: `PlayAnimationOnClick` now automatically uses Animator state logic to determine looping and continuation of animation (what happens after the specified animation has finished playing)
- Change: USDZ: current animator states are exported as `PlayAnimation` on scene start

### Unity Integration
- Change: USDZ `PlayAnimationOnClick` loop state and next state are now implict

## [3.26.2-pre] - 2023-11-28
### Needle Engine
- Add physics NaN safeguards to avoid invalid rapier data propagating through three objects

## [3.26.1-pre] - 2023-11-27
### Unity Integration
- Add warning label to experimental components

### Needle Engine
- Add: information about mouse button, hit point, hit normal, hit distance to `PointerEventData`
- Fix: EventSystem regression where UI events stopped working

## [3.26.0-pre] - 2023-11-27
### Unity Integration
- Add: option to ExportInfo to automatically compress local exports (enabled by default)
- Change: Improve first time installation
- Change: Clean install now also deletes tools package
- Change: Update scene templates

### Needle Engine
- Fix: LOD update frame delay
- Change: EventSystem optimization to avoid raycasting objects without event receiver components

## [3.25.5] - 2023-11-24
### Unity Integration
- Add `DeployToFacebookInstantGames` component
- Add `Open Build Window` button to ExportInfo and Needle Engine Settings
- Change: Show warning if Needle Engine TextueSettings MaxSize can not be applied because the texture is already imported at a lower resolution

### Needle Engine
- Add: vite plugin for facebook deployment
- Fix: Physics collider center being not applied correctly with rotated parent

## [3.25.4] - 2023-11-23
### Unity Integration
- Add: Button to open Build Window settings with Needle Engine target selected to ExportInfo component
- Fix: ComponentCompiler should not generate types in UnityEditor namespace
- Bump Needle Engine version

### Needle Engine
- Add: `onInitialized(ctx => {...})` and `onBeforeRender` event functions
- Add: `Camera.cullingLayer` property
- Fix: Physics collider center being modified causing rapier runtime error 

## [3.25.3] - 2023-11-23
### Unity Integration
- Bump Needle Engine version

### Needle Engine
- Add: `onStart((ctx)=>{})` and `onUpdate((ctx)=>{})` functions API
- Fix: AudioSource not starting autoplay anymore after registered user interaction (which is necessary to playback audio in the browser)
- Fix: `TransformGizmo` component not working as expected anymore since it also defines `worldPosition`
- Change: Improve performance for colliders without rigidbody to not create an implicity rapier rigidbody anymore

## [3.25.2] - 2023-11-20
### Needle Engine
- Add: `worldForward`, `worldRight` and `worldUp` to Object3D and GameObject types 
- Add: `getTempVector` utility method that has a circular array of vector3 instances for re-use
- Fix: ImageTracking hysteresis for images to stay visible during bad tracking for up to a second after tracking has been lost

## [3.25.1] - 2023-11-15
### Unity Integration
- Change: Minor project validation window and menu item changes
- Bump Needle Engine dependency

### Needle Engine
- Add: getters and setters on Object3D and GameObject types for `worldPosition`, `worldRotation`, `worldQuaternion` and `worldScale`
- Fix: ImageTracking now has hysteresis for how long to keep a tracked object visible before disabling it (if tracking is lost just for a few frames)
- Fix: Catch and log exception in rapier during collider creation
- Change: WebXRImageTracking objects that are already present in the scene are now hidden when entering WebXR/AR

## [3.25.0] - 2023-11-15
### Unity Integration
- Add: ObjectRaycaster expose option to ignore skinned mesh renderers
- Add: project setup validation window and improve first installation UX
- Change: Improve OrbitControls inspector and expose methods to pass in camera position or look target from Unity events

### Needle Engine
- Add: `Mathf.easinOutCubic` utility method
- Add: ObjectRaycaster expose option to ignore skinned mesh renderers
- Fix: detecing website interaction to allow playing audio before any component or audio component has been loaded
- Fix: Physics capsule height creation
- Fix: WebXRImageTracking not hiding tracked objects after tracking has been lost
- Change: Improved ContactShadows component
- Change: Improve OrbitControls for smoother lerping. Add methods to set camera position and look at target by passing in an Object3D reference.
- Change: PlayAudioOnClick does now create an AudioSource implictly if non is assigned
- Change: EventSystem does ignore SkinnedMeshRenderers by default now
- Change: WebXR reticle is now hidden when image tracking starts

## [3.24.0] - 2023-11-15
### Unity Integration
- Fix: issue where font naming didn't match casing of file on disc causing fonts not being found at runtime
- Change: add additional logs to license `Refresh` button

### Needle Engine
- Fix: Everywhere Action material otherVariants was not cleared between behaviour generation
- Fix: Everywhere Action Change Material loosing track of the target material
- Fix: issue where SpatialTrigger calling AudioSource.play doesnt work because of wrong argument
- Fix: text linebreak in USDZ
- Fix: material bindings API for Preliminary_Text and don't apply material when no geometry is found
- Fix: material assignments for USDZ text and fix color space
- Fix: Capsulecollider height
- Fix: Core networking issue where throwing callbacks would silently be ignored and causing not all callbacks being called
- Fix: PlayerSync `owner-changed` being raised twice
- Fix: PlayerSync unsubscribe from UserLeftRoom event once the player is leaving/has left
- Fix: Catch exception in creating `new type()` during deserialization of animationclip if the clip is just a string and could not be resolved because it's missing, falsely serialized or annotated
- Change: OnClick Everywhere Actions now ensure they have a raycaster component assigned or in parent hierarchy
- Change: Everywhere Action PlayAnimationOnClick remove target field

## [3.24.0] - 2023-11-13
### Unity Integration
- Add: Timeline AudioTrack now exports volume per track
- Add: `RigidbodyData` component to expose `autoMass` toggle to Unity
- Add: ContactShadows component
- Fix: Timeline infinite animationclip should serialize pre- and post extrapolation
- Change: Bump UnityGLTF version to 2.5.2-exp to support camera backgroundColor animation

### Needle Engine
- Add: ContactShadows component
- Add: `Gizmos.DrawLabel`
- Fix: issue with AnimatorController behaviour when using loop and cycle offset
- Fix: error when loading component with missing AnimatorController field
- Fix: use correct colorspace for UI components
- Fix: loading of remote GLB and `skybox-image` url where default camera should set clearflags to skybox
- Fix: Various colorspace issues fixed
- Change: USDZ export `imageToCanvas` now uses OffscreenCanvas for improved performance
- Change: Update pmndrs postprocessing package to 6.33.3 to fix SSAO not working on mobile android
- Change: `ObjectUtils.createPrimitive` so all primitives use the same settings

## [3.23.1] - 2023-11-09
### Unity Integration
- Fix: regression where AnimatorController could not be used on multiple objects anymore introduced in last version
- Change: menu item `Setup Scene` now creates scene without extra `GltfObject` component since this actually not needed anymore 

### Needle Engine
- Add: `Gizmos.DrawLabel` method
- Add: `LookAtObject` utils method with options to keep upwards direction and copying target rotation to stay screen aligned
- Fix: WebXRPlaneTracking dispose old mesh data properly, heuristically determine if a shape should be convex or not
- Fix: AudioSource `play` not working anymore if called without parameters
- Internal: rapier meshcolliders now use convexHull instead of convexMesh, the latter already expects the input data to be convex

## [3.23.0] - 2023-11-08
### Unity Integration
- Add: AnimatorController cycle offset and speed export settings per state
- Change: AnimatorController can now be referenced from custom scripts and properly exported
- Change: Bump UnityGLTF version to fix issues related to animation export and exporting glTF files via context menu that reference EXR textures (which were previously exported using a wrong encoding causing errors at runtime)
- Remove: csharp to typescript package dependency

### Needle Engine
- Add: support for changing animatorcontroller at runtime
- Add: AnimatorController support for cycle offset and speed being used from parameter (or fixed serialized value)
- Add: `OrbtiControls.allowInterrupt` property that can be set to false to prevent animation to a target point or autoRotate being interrupted by user input like clicking or dragging
- Add: Physics collision now includes tangent vector for contact points
- Add: Physics exposes API for getting the object velocity per collider (`context.physics.engine.getLinearVelocity`)
- Add: Physics Material can now be updated at runtime. Call `updatePhysicsMaterial` on the collider with the changed physics material
- Fix: scaled capsule collider being created with wrong size
- Fix: timeline audiotracks not respecting speed property on PlayableDirector (effectively being cut-off instead of being played back at another speed/playbackRate) 
- Fix: Prevent using XRAnchor on Quest in AR mode (pass-through)
- Fix: Timeline evaluate is now done in lateUpdate which gives animated objects time to apply the changed data (e.g. OrbitControls where the target object may be animated)
- Fix: Timeline Audio tracks do not require AudioListener in scene anymore

## [3.22.6] - 2023-11-06
### Unity Integration
- Fix: Linked Npmdef issue where the local npm package wasnt removed correctly from the web project
- Fix: compilation warnings and errors on 2020.x

### Needle Engine
-  Change: work on approximated transmission export in USDZ

## [3.22.5] - 2023-11-03
### Unity Integration
- Fix: drag linked npmdef into ExportInfo dependencies array 
- Fix: skybox resolution editing in component

## [3.22.4] - 2023-11-03
### Unity Integration
- Fix: npmdef string comparison not working in some cases in Unity 2022.3
- Fix: Opening npmdef directory when the directory didnt contain a vscode workspace file
- Change: Show licence type in Needle Engine headers

### Needle Engine
- Add: Option to WebXRControllers to disable default controls (`enableDefaultControls`) and raycasting (`enableRaycasts`)
- Fix: AnimatorController transition with exitTime and trigger didnt work since trigger was reset before transition could be made
- Fix: USDZExporter isssue where compressed textures always ended up as JPG after decompression since format check was only checking for RGBAFormat, now also checks for compressed formats
- Change: catch and display unhandled exceptions during creation of engine, make sure bubble messages are on top of loading overlay
- Change: USDZExporter should re-use renderer 

## [3.22.3] - 2023-11-02
### Unity Integration
- Fix: issue where externally linked npmdef could not be added to ExportInfo dependencies in some cases
- Fix: issue where creating a linked npmdef did cause AssetDatabase warnings
- Change: show warning in ExportInfo footer if toktx wasnt found

### Needle Engine
- Add: Coroutine can now yield on promise and wait for the promise to be resolved
- Fix: CharacterController not being grounded on mesh collider
- Fix: ShadowCatcher set to additive mode didnt work anymore

## [3.22.2] - 2023-11-01
### Unity Integration
- Change: Linked npmdef creation improved UX

### Needle Engine
- Add: Optional `RectTransform.minWidth` and `RectTransform.maxWidth`
- Fix: GameObject instantiate should clone color objects (to not share instance)
- Fix: Call to `parseSync` with full url argument now correctly passes base url to three GLTFLoader for resolving external resources
- Change: PhysicsMaterial properties are now all optional (e.g. so we can set only friction)
- Change: `Rigidbody.setForce` to take a vec3
- Change: `AudioSource.onDisable` should pause not stop
- Change: `Mathf.random(<min>?, <max>?)` now takes optional min and max parameters

## [3.22.1] - 2023-10-30
### Unity Integration
- Bump Needle Engine version

### Needle Engine
- Fix: `Screencapture.autoconnect` when already connected to networking server or connection is in progress / window selection is currently open
- Change: when `isManagedExternally` is enabled then framerate is user controlled and not automatically clamped
- Change: add more documentation to networking methods

## [3.22.0] - 2023-10-26
### Unity Integration
- Change: Attempting to export unsaved scene stops export now

### Needle Engine
- Fix: SyncedCamera deserialization warning
- Change: Collider property updates now trigger rigidbody mass recalculation immediately (if set to auto-mass)
- Change: Rigidbody methods now take Vec3 object as well as arguments like `{x:0, y:1, z:0}`
- Change: Improve Screencapture
- Change: Screencapture now also allows `Microphone` as input device
- Change: Updated VOIP script and removed old VOIP implementation - it now uses the same underlying codebase as screencapture
- Change: Update imports to use `type` where appropriate

## [3.21.5] - 2023-10-25
### Unity Integration
- Change: Allow using Node 20 LTS (currently showing a warning)

### Needle Engine
- Fix: remove leftover console.log
- Fix: Hovered button should reset pointer state when destroyed
- Fix: WebXR Rig parenting in VR when switching scenes
- Fix: Timeline activation track not properly evaluating when timeline is paused and manually evaluated from user code
- Fix: Sphere Collider radius not being set correctly

## [3.21.3] - 2023-10-23
### Unity Integration
- Bump Needle Engine version

### Needle Engine
- Fix: Issue where Chrome touch emulation caused "onPointerClick" being called twice per click
- Fix: EventList instances are now not shared anymore between components created via `instantiate`
- Fix: Regression where SphereCollider radius was not being applied

## [3.21.2] - 2023-10-23
### Unity Integration
- Fix: npmdef references should properly update itself when the npmdef path or name of the package have changed 

### Needle Engine
- Change: Expose EventList subscriber count
- Change: `PointerEventArgs.use()` should not stop propagation

## [3.21.1-pre] - 2023-10-20
### Unity Integration
- Bump UnityGLTF dependency to 2.5.0
- Bump helper package to get meshopt compression fix for animation (requires Needle Build Pipeline 1.3.1)
- Fix: False export where GltfObject was *not* in the root scene hierarchy *but* somewhere on a child object
- Fix: Double click on SampleInfo should not attempt to open immutable scene asset (which is not allowed) but instead handle the regular `Open Sample Scene` event to correctly open the sample

### Needle Engine
- Add: Multitouch support on `input` events, our EventSystem implementation now handles multitouch cases and is using the browser events directly and immediately (before events via `window` where deferred and hanlded during the engine update loop)

## [3.21.0-pre] - 2023-10-19
### Unity Integration
- Add: WebXRSessionRoot preview feature option to enable touch controls for drag, scale, rotate in AR
- Bump Needle Engine version

### Needle Engine
- Add: support for translate, rotate and scale of AR scene on android devices (needs `WebXRSessionRoot.arTouchTransform` set to true right now)
- Add: `SphereCollider.radius` and `BoxCollider.size` updates at runtime are now automatically propagated to the physics engine updating the physics shapes. Additionally the object scale for SphereCollider objects is watched and automatically applied on change 
- Fix: removal of all colliders on an object now also fully cleansup the implictly created RigidBody
- Change: `Context.isManagedExternally` can now be set at runtime as a first step towards allowing complete external control over the Needle Engine lifecycle loop in cases where Needle Engine scenes or components are mixed with an external three.js scene (and projects that require more explicit control)

## [3.20.3] - 2023-10-18
### Unity Integration
- Add: UI to settings to refresh license
- Change: Improve displayed information when nodejs is not found or installed
- Bump Needle Engine version

### Needle Engine
- Add: Expose Rapier dominance group option on Rigidbody
- Fix: Ignore root motion when animator weight is <= 0 due to Timeline playing
- Fix: Rapier race condition caused by dynamic loading
- Change: Allow setting Rigidbody mass explictly now by either setting `autoMass` to false or by setting the `mass` property

## [3.20.2] - 2023-10-17
### Unity Integration
- Fix: path for tools package not being found in some cases
- Fix: 2020 compiler errors
- Change: Make it more obvious in cases where samples can not installed and why
- Change: Remove 2020 from recommended Editor versions in samples window

### Needle Engine
- Fix: renderer access nullreference exceptions caused by deferred initialization
- Fix: disabling postprocessing now restores renderer clear state (which got disabled by the postprocessing package)
- Fix: disable generating WebXRPlane tangents

## [3.20.1] - 2023-10-16
### Unity Integration
- Fix: license check sometimes not working correctly
- Change: Improve error message when trying to export unsaved scene

### Needle Engine
- Change: `addComponent` can now take component instance or type

## [3.20.0] - 2023-10-13
### Unity Integration
- Add options for mesh tracking to `WebXRPlaneTracking` component

### Needle Engine
- Fix: issue where physics colliders where not yet fully initialized in `start` event
- Fix: Pointer delta while cursor is locked
- Change: Expose `context.phyiscs.engine.world` and  `context.physics.engine.getComponent` method to directly work with rapier physics engine and to easily get access to Needle Engine components from rapier colliders
- Change: Expose `Context.DefaultWebGLRendererParameters` that can be modified in static context before renderer is created

## [3.19.9] - 2023-10-11
### Unity Integration
- Bump Needle Engine version

### Needle Engine
- Change: `AnimatorController.createFromClips` now sets the state hash to index of clip
- Fix: AnimatorController root motion direction when runtime instantiating and using the same clip on multiple objects
- Fix: AnimatorController root motion forward direction when rotating object from script as well

## [3.19.8] - 2023-10-10
### Unity Integration
- Add: Animation component export for clips array
- Bump Needle Engine version

### Needle Engine
- Add: `AnimatorController.createFromClips` utility method taking in a animationclips array to create a simple controller from. By default it creates transitions to the next clip
- Fix: occasional issue where the scrollbar would cause flickering due to hiding/showing when the website was zoomed
- Fix: screenshot utility method respecting page zoom
- Fix: vite dependency watcher plugin running installation if dependency in package.json would change
- Fix: Animator root motion working with multiple states, clips and transitions

## [3.19.7] - 2023-10-04
### Unity Integration
- Add: OrbitControls `enableRotate` property
- Change: LODGroup export in correct format and fix issue with last LOD not being used in cases without a "Cull" state

### Needle Engine
- Add: OrbitControls `enableRotate` property
- Fix: LODGroup not using last LOD in cases where the last LOD is never culled
- Fix: PostProcessing EffectStack correctly ordered when using N8 Ambient Occlusion (together with Bloom for example)
- Fix: Postprocessing N8 should not modify gamma if it's not the last effect in the stack
- Fix: AudioSource does now create an AudioListener on the main camera if none is found in the scene
- Change: VideoPlayer does fallback to clip if src is empty or null
- Change: OrbitControls now expose `enableRotate` property
- Fix: web component font import

## [3.19.4] - 2023-09-29
### Unity Integration
- Bump Needle Engine version

### Needle Engine
- Fix: Remove leftover OrbitControls log
- Change: Timeline TrackModel `markers` and `clips` fields are now optional
- Change: VideoPlayer is set to use url as default video source (if nothing is defined)

## [3.19.3] - 2023-09-28
### Unity Integration
- Bump UnityGLTF dependency to `2-4-2-exp` which fixes export for root level objects marked as `EditorOnly`

### Needle Engine
- Fix: regression in OrbitControls without lookat target assigned
- Fix: progressive textures loading with custom reflection probe
- Fix: WebAR touch event screenspace position using `this.context.input` 

## [3.19.2] - 2023-09-27
### Unity Integration
- Add: OrbitControls `autoFit` property
- Fix: Error when creating a FTPServerAsset in Unity 2022.3

### Needle Engine
- Add: OrbitControls `autoFit` property
- Add: API to access underlying Rapier physics body using `context.physics.engine.getBody(IComponent | IRigidbody)`

## [3.19.1] - 2023-09-27
### Unity Integration
- Add: ParticleSystem now supports HorizontalBillboard and VerticalBillboard
- Fix: exception in ComponentGenerator when clicking `regenerate components` in npmdef without ever having opened a Needle Project

### Needle Engine
- Add: ParticleSystem now supports HorizontalBillboard and VerticalBillboard
- Fix: [WebXR chromium bug](https://bugs.chromium.org/p/chromium/issues/detail?id=1475680) where the tracking transform matrix rotates roughly by 90° - we now add an WebXR Anchor to keep the scene at the placed location in the real world
- Fix: SceneSwitcher does now call event on first `ISceneEventListener` found on root level of a loaded scene (e.g. if a Unity scene is loaded that contains multiple children and does not have just one root object)
- Fix: Text UI clipping with multiple active screenspace canvases in scene
- Fix: Screenspace canvas events should not be blocked anymore by objects in 3D scene
- Fix: FirstPersonController rotation not being correctly / falsely resetted and flipped in some cases

## [3.19.0] - 2023-09-26
### Unity Integration
- Add: commandline argument to accept EULA via `--accept-needle-eula`
- Change: disable ExportInfo UI while cloning a remote project template
- Change: move react three fiber project template into remote repository

### Needle Engine
- Fix: collider scale wrongly affecting physics objects
- Fix: collider debug lines should not be raycastable
- Fix: mesh-collider behaving unexpectedly
- Fix: animator root motion causing error due to uninitialized Quaternion object

## [3.19.0-pre] - 2023-09-25
### Unity Integration
- Add: Project templates cloneable from github (added Sveltekit, Svelte and React templates)
- Fix: Improve installation of npmdef dependencies to be able to just click Play when opening or switching a sample
- Fix: saving of remote url in FTP server asset
- Fix: ShadowCatcher `Create` button creating plane with wrong rotation in some cases
- Change: clarify EULA window text
- Internal: Fix SampleInfo asset not being editable in inspector

## [3.18.0] - 2023-09-21
### Unity Integration
- Add: SceneSwitcher has now a field for `LoadingScene` which can be used to display a scene / 3D content while loading other scenes
- Fix: Improve license check
- Fix: Rare MissingReference error caused by EditorSync component while adding/removing components inside a prefab
- Change: Improve feedback when clicking the red Typescript component link (for scripts used in the scene but not installed in the web project)
- Change: Improse feedback for Needle Engine Pro Trial limits

### Needle Engine
- Add: SceneSwitcher has now a field for `loadingScene` which can be used to display a scene while loading other scenes
- Add: `ISceneEventListener` which is called by the SceneSwitcher when a scene has been loaded or a scene is being unloaded. It can be used to handle showing and hiding content gracefully. It has to be added to the root of the scene that is being loaded (e.g. the root of the scene or prefab assigned to the `loadingScene` field or the root of a scene assigned to the `scenes` array)
- Add: `hide-loading-overlay` attribute to `<needle-engine>` webcomponent (use like `<needle-engine hide-loading-overlay>`). Custom loading requires a PRO license. See [all attributes in the documentation](https://engine.needle.tools/docs/reference/needle-engine-attributes.html).
- Fix: Loading overlay should not link to needle website anymore when using a custom logo
- Fix: Add safeguard to user assigned events on `<needle-engine>` for cases where methods are not defined in the global scope
- Change: Update loading message displayed in overlay while waiting for `ContextCreated` promise (e.g. in cases where a large environment skybox is being loaded)

## [3.17.1] - 2023-09-20
### Unity Integration
- Fix: DeployToFTP deployment producing wrong meta image url 

## [3.17.0] - 2023-09-20
### Unity Integration
- Add: ExportInfo `remoteUrl` field which allows to pull or download projects from a remote repository instead being created from a local template
- Fix: ExportInfo shows an error if the directory paths contains invalid characters

### Needle Engine
- Fix: handle exception when loading GLB/glTF files with invalid lightmapping extension

## [3.16.5] - 2023-09-18
### Unity Integration
- Bump Needle Engine version

### Needle engine
- Add: help balloon message if user tries to open a local file without using a webserver
- Add: helpful console.log if user tries to add a component that is not a Needle Engine component
- Change: Ignore shadow catcher and GroundProjectedEnvironment sphere when running OrbitControls.fit

## [3.16.3] - 2023-09-15
### Unity Integration
- Add: `EditorModificationHandler.HandleChange` event to allow modification (or ignoring) of editor modifications, ignore UnityEvent changes by default
- Change: EditorSync ping should not run on main thread and block the editor

### Needle Engine
- Add: logo now respects prefer-reduced-motion, reduce and is immediately added instead of after 1s
- Fix: use default background color if GLB without camera and skybox is loaded
- Fix: ensure custom KTX2 loader is correctly initialized
- Fix: revert RectTransform change that broke hotspot rendering
- Change: adjust default backgroundBlurriness to match Blender defaults

## [3.16.2] - 2023-09-15
### Unity Integration
- Fix: License Check for exporting AnimatorController animation

### Needle Engine
- Add: mesh collider handling for invalid mesh data (non-indexed geometry)

## [3.16.2-pre] - 2023-09-13
### Needle Engine
- Add: `camera.environmentIntensity` property
- Change: default background blurriness for fallback camera to match blender default

## [3.16.1-pre] - 2023-09-13
### Needle Engine
- Change: if loaded glTF doesnt contain a camera we now also create the default OrbitControls (e.g. glTF exported from a Blender scene without a camera)

## [3.16.0-pre] - 2023-09-13
### Needle Engine
- Add: `NEEDLE_lightmaps` entries `pointer` property can now also be a path to a local texture on disc instead of a texture pointer. This allows Blender EXR and HDR maps to be used at runtime until Blender export supports hdr and exr images to be stored inside the GLB

## [3.15.0-pre] - 2023-09-13
### Unity Integration
- Fix: glTF `OnAfterImport` exception if imported glTF produced a missing GameObject

### Needle Engine
- Fix: remove leftover console.log
- Fix: `DeviceFlag` component not detecting devices correctly for iOS safari
- Fix: loading glTF without any nodes
- Fix: `SceneSwitcher` bug where a scene would be added twice when switching in fast succession
- Fix: `Animation.isPlaying` bool was always returning false
- Fix: Handle typescript 5 decorator change to prevent VSCode error message (or cases where `experimentalDecorators` is off in tsconfig). See [179](https://github.com/needle-tools/needle-engine-support/issues/179)
- Fix: Improve internal lifecycle checks and component method calls
- Change: Improse ContextRegistry/NeedleEngine `ContextEvent` enum documentation
- Change: `<needle-engine skybox-image=` and `environment-image=` attributes are now awaited (loading overlay is still being displayed until loading the images have finished or failed loading)

## [3.14.0-pre] - 2023-09-11
### Unity Integration
- Add: custom shader material inspectors now have UI with export options and information
- Remove: react template, please use https://github.com/needle-engine/react-sample instead
- Update: react-three-fiber template
- Bump UnityGLTF dependency to 2.4.1-exp containing an fix for KHR_animation_2 export and added compressed texture import

### Needle Engine
- Add: exposing `Needle.glTF.loadFromURL` in global scope to support loading of any glTF or GLB file with needle extensions and components when using the prebundled needle engine library (via CDN)
- Add: `context.update` method for cases where needle engine is now owning renderer/scene/camera and the update loop is managed externally
- Fix: animating custom shader property named `_Color` 
- Fix: issue with wrong CSS setting in Needle Engine breaking touch scroll
- Change: `?stats` now logs renderer statistics every few seconds
- Change: simplify creating a new Needle Context that is controlled externally (not owning camera/renderer)

## [3.13.0-pre] - 2023-09-08
### Unity Integration
- Add: `ActionsBrowser.BeforeOpen` event to allow modification to local server url or to customize the browser being opened / handle browser opening yourself
- Bump UnityGLTF dependency to 2.4.0-exp containing various color export fixes
- Fix: EditorSync issue when dragging transform position.x
- Fix: ArgumentOutOfRange exception in UnityEvent when no method name is assigned (or missing)
- Fix: Various cases where colors where exported in wrong colorspace affecting UI, materials and particles

### Needle Engine
- Add: ParticleSystem now also uses material color
- Add: `IEditorModificationListener.onAfterEditorModification` callback (requires `@needle-tools/editor-sync@2.0.1-beta`)
- Bump: Three.Quarks dependency to 0.10.6
- Update draco decoder include files

## [3.12.2-pre] - 2023-09-04
### Unity Integration
- Fix: Unity Progress display description on Windows interpreting `\n` as a newline which caused description to be cut off

### Needle Engine
- Add: option to override peerjs host and id (options) via `setPeerOptions`
- Fix: potential nullreference error in AudioListener
- Fix: Networking component cases where invalid localhost input with "/" causes url to contain "//" sometimes -> we can skip one "/" in this case and make it just work for users
- Fix: package.json `overrides` syntax for quarks three.js version
- Change: Screensharing bool to disable click to start networking + add deviceFilter to share(opts:ScreenCaptureOptions)

## [3.12.1-pre.1] - 2023-09-04
### Unity Integration
- Change: Update npmdef package version dependencies
- Fix: Handle `Win32 operation completed successfully` exception 

## [3.12.1-pre] - 2023-09-04
### Unity Integration
- Fix: editor web request failing on OSX

### Needle Engine
- Fix: next.js/webpack useRapier setting
- Change: typestore is now globally registered

## [3.12.0-pre.4] - 2023-08-29
### Unity Integration
- Change: Update cloning a project from github

## [3.12.0-pre.3] - 2023-08-28
### Unity Integration
- Change: Update git clone
- Fix: exporting all colors in linear colorspace now

## [3.12.0-pre.2] - 2023-08-28
### Unity Integration
- Fix: UnityEvent arguments not being used anymore

### Needle Engine
- Fix: vite hot reload plugin to dynamically import needle engine to support usage in server-side rendering context

## [3.12.0-pre] - 2023-08-28
### Unity Integration
- Add: commonly used skyboxes
- Change: Drop support for Unity 2020
- Change: bump UnityGltf from 2.2.0-exp to [2.3.1-exp](https://github.com/prefrontalcortex/UnityGLTF/blob/dev/CHANGELOG.md)
- Fix compiler error caused by HideInCallstacks in Unity 22.1.23
- Fix: issue where needle.config `assetDirectory` path wasn't respected (e.g. for sveltekit)

### Needle Engine
- Add: Timeline api for modifying final timeline position and rotation values (implement `ITimelineAnimationCallbacks` and call `director.registerAnimationCallback(this)`)
- Change: Update three quarks particlesystem library to latest
- Fix: issue where onPointerExit was being called when cursor stops moving
- Fix: USDZ normal scale Z was incorrect
- Fix: Timeline Signal events using different casing than UnityEvent events
- Fix: issue where `isLocalNetwork` was falsely determined

## [3.11.6] - 2023-08-15
- Remove beta

## [3.11.6-beta] - 2023-08-14
### Unity Integration
- Add: Tag filters to samples window
- Fix: HideInCallstacks compiler error

### Needle Engine
- Fix: find exported animation by PropertyBinding 
- Fix: USDZExporter was not exporting animation from Animation component but only from Animator
- Fix: potential issues with Animation component `clip.tracks` being null/undefined on USDZ export
- Fix: `loadstart` event not being called
- Fix: getComponent should always either the component or null (never undefined)
- Fix: dynamic import of websocket-ts package
- Fix: progressive texture loading wasn't properly awaited on USDZ export
- Fix: apply XR flags when exporting to QuickLook
- Fix: USDZ alpha clipping for materials without textures
- Fix: USDZ same material used in different ChangeMaterialOnClick resulted in duplicate behaviour names
- Change: set default WebARSessionRoot to "1" instead of "5"

## [3.11.5-beta] - 2023-08-10
### Unity Integration
- Fix: Shader export uniform parsing and error log
- Fix: Opening Typescript files in Visual Studio or Rider (Unity Default Code Editor) [issue 175](https://github.com/needle-tools/needle-engine-support/issues/175)
- Fix: issue with logging into file in certain cases on windows
- Fix: incorrect warning when wanting to clone from a repository that ends with `/` (via ExportInfo project path)
- Fix: extra styles in template and absolute positioning of shadowroot div
- Change: sanitize live url in DeployToFTP
- Change: add `.DS_Store` to gitignore
- Internal: add tracing scenario to WebHelper, explicit 1s timeout waiting for npm package response
- Internal: Bugreporter improvements

### Needle Engine
- Fix: components keep their gameObject references until `destroy` call of object's is completed when destroying an hierarchy. Previously child components might already be destroyed resulting in `myChildComponent.gameObject` being null when called in `onDestroy` from a parent component
- Fix: regression where timeline was not updating animations anymore if Animator had an empty AnimatorController assigned (without any states)
- Fix: `SceneSwitcher.switchScene` can now handle cases where it's called with a string url instead of an AssetReference 
- Fix: issue where `onPointerMove` event was being called continuously on mobile after touch had already ended
- Fix: issue where GLTFLoader extensions where missing name field resulting in extensions not being properly registered (causing stencils to not work sometimes)
- Change: EventSystem raycast is now only performed when pointer has changed (moved, pressed, down, up) which should improve performance on mobile devices when raycasting on skinned meshes
- Change: peer and websocket-ts import asynchronously
- Remove: legacy include files

## [3.11.4-beta] - 2023-08-04
### Unity Integration
- Bump UnityGLTF fixing issue with blend shape animation not being exported if animation also contained humanoid animations

### Needle Engine
- Fix: USDZExporter exception caused by programmatically calling `exportAsync` without quicklook button
- Fix: Timeline `evaluate` while being paused
- Bump three to fix issue with blend shape animation not being applied to Group objects (KHR_animation_pointer)

## [3.11.3-beta] - 2023-08-03
### Unity Integration
- Add: new option to export glb from context menu without progressive texture processing
- Change: Improve feedback when installing samples
- Fix: finding toktx default installation on MacOS

### Needle Engine
- Change: improve styling of `<needle-engine>` DOM overlay element to allow positioning of child elements
- Fix: USDZExporter normal bias when normalScale is used
- Fix: Nullreference in SceneSwitcher when creating the component from code and calling `select` with a new scene url
- Fix: Quicklook button creation
- Fix: Particlesystem layermask not being respected

## [3.11.2-beta] - 2023-07-31
### Unity Integration
- Fix: CustomReflection texture should not be renamed
- Fix: CustomReflection texture should be at least 64 pixel when exporting
- Change: Bump UnityGLTF dependency, fixing texture transform export for metallicRoughness
- Change: improve "Setup Scene" default names

### Needle Engine
- Fix: `ChangeMaterialOnClick` with multi material objects
- Fix: progressive textures regression

## [3.11.1-beta] - 2023-07-31
### Unity Integration
- Minor editor UI changes

### Needle Engine
- Add: `saveImage` utility method and make `screenshot` parameter optional
- Add: `loading-style="auto"`
- Fix: skybox image caching
- Fix: finding animation tracks for unnamed nodes when using the `autoplay` attribute
- Change: improved `<needle-engine>` default sizes
- Change: smoother src changes on `<needle-engine>` by only showing loading overlay when loading of files takes longer than a second
- Change: bump three version to 154.2 fixing KHR_animation_pointer not working with SkinnedMesh

## [3.11.0-beta] - 2023-07-29
### Unity Integration
- Fix: hide FTP password in console logs
- Fix: incorrect check in Samples Window for installing samples in 2022 LTS and later 2023 LTS
- Change: show installed versions in ExportInfo even if web project is not yet installed

### Needle Engine
- Add: Support for blending between Timeline and Animator animation by fading out animation clips allowing to blend idle and animator timeline animations
- Fix: WebXR buttons style to stay inside `<needle-engine>` web component
- Fix: `OrbitControls.fitCamera` now sets rotation target to the center of the bounding box
- Fix: Timeline animation regression causing Animator not being enabled again after timeline has finished playing
- Fix: Timeline should re-enable animator when ended reached end with wrap-mode set to None
- Change: add `.js` extensions to all imports
- Change: allow overriding loading style in local develoopment
- Change: expose flatbuffer scheme helper methods

## [3.10.7-beta] - 2023-07-28
### Unity Integration
- Fix: Shader uniform export
- Fix: edge case when using URLs in ExportInfo directory 
- Fix: console log prints in certain cases containing control characters
- Fix: Toktx detection not working properly on OSX
- Change: Warn if debug mode is enabled

### Needle Engine
- Fix: Camera using RenderTarget (RenderTexture) now applies clear flags before rendering (to render with solid color or another skybox for example)
- Fix: RenderTexture not working in production build due to texture being compressed
- Fix: RenderTexture warning `Feedback loop formed between Framebuffer and active Texture`
- Fix: Handle Subparticlesystem not being properly serialized avoiding runtime error
- Internal: add resource usage tracking of textures and materials

## [3.10.6-beta] - 2023-07-27
### Unity Integration
- Bump Engine version

### Web Engine
- Fix: Timeline ActivationTrack behaves like `leave as is` when timeline is disabled (not changing the activate state anymore)
- Fix: Timeline Signal Track with duration of 0 and signal at time 0 does now trigger
- Fix: Timeline disabling or pausing does now activate animator again
- Fix: CustomShader Time node for BiRP
- Fix: ParticleSystem simulation mode local now correctly applies parent scale
- Change: Show warning for wrong usage of `@serializable` with `Object3D` where a `AssetReference` is expected
- Change: ParticleSystem shows warning when using unsupported scale mode (we only support local right now)

## [3.10.5-beta] - 2023-07-25
### Unity Integration
- Add: DeployToFTP does not support SFTP
- Add: `overscroll-behaviour` CSS to templates
- Add: `type: module` to templates
- Fix: issue where ftp deployment didnt work on OSX
- Fix: export of referenced scenes or prefabs with timeline where timeline graph was exported in the wrong state

### Web Engine
- Fix: warning at runtime when methods called by `EventList`/`UnityEvent` are in the wrong format
- Fix: OrbitControls issue where double clicking/focus on screenspace UI would cause the camera to be moved far away
- Fix: `OrbitControls.fitCamera` where three `expandByObject` now requires an additional call to `updateWorldMatrix` [26485](https://github.com/mrdoob/three.js/issues/26485#issuecomment-1649596717)
- Change: replace some old `Texture.encoding` calls with new `Texture.colorSpace`
- Change: improve `PlayerSync` networking and add `onPlayerSpawned` event
- Remove: `RectTransform.offsetMin` and `offsetMax` because it's not implemented at the moment

## [3.10.4-beta] - 2023-07-24
### Unity Integration
- Fix: rare InvalidOperationException when codewatcher list is cleared while foreach runs

### Web Engine
- Fix: activating UI elements in VR not applying transform

## [3.10.3-beta] - 2023-07-21
### Unity Integration
- Internal: include package and Unity versions in bug report description

### Web Engine
- Fix: AnimatorController error caused by missing animationclip
- Fix: next.js webpack versions plugin
- Fix: Occasional `failed to load glb` error caused by not properly registering `KHR_animation_pointer` extension 
- Fix: UI issue where Text in worldspace canvas would be visible at wrong position for a frame
- Fix: UI issue where Text would not properly update when switching between text with and without richtext
- Fix: UI issue where Image would not automatically update when setting texture from script
- Fix: issue where RenderTexture would not be cleared before rendering
- Change: make `addEventListener` attribute on `<needle-engine>` optional

## [3.10.2-beta] - 2023-07-19
### Unity Integration
- Change: Samples window clicking `Install Samples` now displays feedback that samples are being installed

### Web Engine
- Fix: iOS double touch / input
- Change: minor WebXRController refactor moving functionality into separate methods to be patchable

## [3.10.1-beta] - 2023-07-18
### Unity Integration
- Fix: workaround for TextureImporter.spritesheet being obsolete without proper replacement
- Change: update LTS version warning; no warning on 2022 LTS (and 2023 LTS) but warn on 2020 LTS since that's out of support.

### Web Engine
- Fix: prebundled package
- Fix: runtime license check
- Fix: Input being ignored after first touch
- Fix: SpatialTrigger, reverting previous change where we removed the trigger arguments

## [3.10.0-beta] - 2023-07-17
### Unity Integration
- Fix: shadow catcher BiRP support
- Change: add link to [feedback form](https://fwd.needle.tools/needle-engine/feedback) to License window

### Web Engine
- Fix: Text clipping in VR
- Fix: AR overlay `quit-ar` button not being properly detected
- Fix: Timeline animation track post-extrapolate set to `Hold`
- Fix: iOS touch event always producing double click due to not properly ignoring mouse-up event
- Change: DragControls to automatically add ObjectRaycaster if none is found in parent hierarchy
- Change: DragControls now expose options to hide gizmo and to disable view-dependant behaviour

## [3.10.0-exp] - 2023-07-15
### Exporter
- Add: support to download project via git repository
- Fix: issue with opening project directory for certain relative paths

### Engine
- Change: WebXR component now automatically adds a WebARSessionRoot on entering AR when no session root was found in the scene
- Change: `@syncField` can now sync objects by re-assigning the object to the same field (e.g. `this.mySyncedObject = this.mySyncedObject` to trigger syncing)
- Change: log error when `@syncField` is being used in unsupported types (currently we only support syncField being used inside Components directly)
- Change: improve message when circular scene loading is detected and link to documentation page 

## [3.9.1-exp] - 2023-07-14
### Exporter
- Fix: compiler errors in Unity 2023.1
- Fix: bug in npmdef registry causing packages to not be properly registered on first editor startup
- Fix: OSX editor stall due to FileWatcher
- Change: Add badge to scene templates
- Change: Don't make insecure calls (localhost running on `http`) when `PlayerSettings.insecureHttpOption` is turned off starting from Unity 2022
- Change: component compiler should ignore .d.ts files
- Change: component compiler can now work without web project (only requires ExportInfo in the scene)
- Internal: cleanup Collab Sandbox scene template, remove unused material

### Engine
- Add: SceneSwitcher now uses scene name by default. Can be turned off in component
- Fix: ParticleSystem lifetime not respecting simulation speed 
- Fix: ParticleSystem prewarm with simulation speed and improved calculation of how many frames to simulate
- Fix: Exit AR and Exit VR now restores previous field of view
- Change: close AR button adjusted for better visibility on bright backgrounds
- Change: bump @types/three to 154

## [3.9.0-exp] - 2023-07-12
### Exporter
- Bump Engine version

### Engine
- Add: `<needle-engine>` web component slot support, AR DOM overlay can now be added by simple adding HTML elements inside the `<needle-engine></needle-engine>` web component. Fixing [165](https://github.com/needle-tools/needle-engine-support/issues/164)
- Add: Basic USDZ exporting of UI shadow hierarchies as mesh hierarchies for UI in Quicklook AR support
- Fix: WebXR Rig not being rotated as expected when setting up in Unity [129](https://github.com/needle-tools/needle-engine-support/issues/129)
- Fix: WebXR VR button click, hover is still not working
- Fix: Issue with Lightmaps breaking when switching back and forth between multiple lightmapped scenes
- Change: Button click should not invoke with middle or right mouse

## [3.8.0-exp] - 2023-07-11
### Exporter
- Bump Engine version to use three.js 154 (latest)

### Engine
- Update three.js to 154 (latest)
- Bump postprocessing dependency
- Add: `this.context.xrCamera` property
- Fix: screenspace canvas should not run in VR
- Fix: OrbitControls should not update while in AR and touching the screen
- Change: allow using vanilla three.js by dynamically importing KHR_animation pointer api 

## [3.7.7-pre] - 2023-07-11
### Exporter
- Bump Engine

### Engine
- Fix: LookAt copyTarget + keepUpDirection
- Fix: DragControls not working on first touch on mobile / clone input event
- Fix: Renderer assigning renderOrder in URP on SkinnedMesh with multi-material

## [3.7.6-pre] - 2023-07-10
### Exporter
- Fix: react and r3f templates
- Fix: warnings on OSX
- Fix: invalid cast exception due to change with prefab export
- Change: use UnityWebRequest.EscapeURL for BugReporter description
- Change: show reason for why Bugreporter dialogue popup shows again

## [3.7.5-pre.3] - 2023-07-08
### Exporter
- Fix: compiler error in Unity 2021.3.28 (latest)

## [3.7.5-pre.2] - 2023-07-07
### Exporter
- Fix: Unity 2020.3 compiler error

## [3.7.5-pre.1] - 2023-07-07
### Exporter
- Fix: minor 2021+ compiler warning
- Change: Allow longer bug report descriptions

## [3.7.5-pre] - 2023-07-07
### Exporter
- Add: support for bugreporter descriptions
- Change: Fonts handle semibold variant
- Change: make sure PlayerSync can be enabled/disabled in the editor
- Internal: specifically log when reading file is not allowed

### Engine
- Fix: USDZExporter should not show Needle banner when branding information is empty (pro only)
- Fix: USDZExporter sessionroot scale should be applied to object to be exported when the root is in the parent
- Fix: DropListener localhost without explicit backend url + dropping file caused exceptions
- Fix: instanceof error that tsc complained about
- Change: Fonts handle semibold variant
- Internal: DropListener re-use addFiles method, remove old code
- Internal: Bump tools package dependency

## [3.7.5-exp] - 2023-07-06
### Exporter
- Fix: catch access lock exception when trying to read npm log
- Change: component in prefab referencing root prefab should not export as glb path

### Engine
- Add: SignalEvents support for arguments
- Fix: SpatialTrigger Unity events removing extra (unexpected) event arguments
- Fix: safeguard `AudioSource.play` to not fail when `clip` argument is not a string
- Change: change Timeline signal event trigger time to use last frame deltatime with padding to estimate if the event should fire

## [3.7.4-exp] - 2023-07-05
### Exporter
- Fix: Sprite colorspace export taking sRGB textures into account

### Engine
- Change: targetFps, use timestamp that we get from the animation callback event because it is more reliable on Firefox

## [3.7.3-exp] - 2023-06-26
### Exporter
- Bump engine version

### Engine
- Add: physics gravity to `IPhysicsEngine` interface to be available via `this.context.physics.engine.gravity`
- Fix: USDZ text alignment

## [3.7.2-exp] - 2023-06-23
### Exporter
- Add: `Preview Build` button to PlayerBuildWindow
- Fix: PlayerBuildWindow for Unity 2022.3.3 

### Engine
- Fix: Nullref in SpectatorCamera.onDestroy when camera wasnt active

## [3.7.1-exp] - 2023-06-22
### Exporter
- Fix: Font export with styles that are unknown to the Unity FontStyle enum (e.g. `-Medium`)

### Engine
- Add: ChangeMaterialOnClick `fadeDuration` option (Quicklook only)
- Change: USDZ export now enforces progressive textures to be loaded before export
- Change: USDZ export callbacks for `beforeCreateDocument` and `afterCreateDocument` can now run async
- Fix: USDZExporter quicklook button
- Fix: USDZExporter Quicklook button not being removed when exporter gets removed or disabled
- Fix: USDZ ChangeMaterialOnClick clear cache before exporting, this caused USDZ export to fail on third export in USDZ sample scene
- Fix: Engine loading bar not being updated
- Fix: USDZ text linebreaks
- Fix: UI font name style check. Unknown font styles are now not touched anymore (e.g. font name ending with `-Medium`)

## [3.7.0-exp] - 2023-06-21
### Exporter
- Bump Engine version

### Engine
- Change: Move HTML elements into <needle-engine> shadow dom

## [3.6.13] - 2023-06-21
### Exporter
- Fix: use assemblylock to handle regenerating all components in npmdef
- Bump Engine version

### Engine
- Add: static Context.DefaultTargetFrameRate
- Add: option to prevent USDZExporter from creating the button
- Fix: `@prefix` handling promise resolving to false

## [3.6.11] - 2023-06-19
### Exporter
- Bump UnityGLTF version adding support for importing draco compressed meshes and KTX2 compressed textures

### Engine
- Add: UI InputField API for clear, select and deselect from code
- Change: LODGroup serialization
- Fix: mobile debug console should be above loading overlay if error happens during loading
- Fix: LODGroup not being able to resolve renderer reference
- Fix: Particles direction being wrong in some causes with scaled parent and scaled particle system
- Fix: Particles subsystem emitter position being wrong when main particle system was scaled
- Fix: Bundled library failing to load due to undeclared variable
- Fix: UI InputField hide html element
- Fix: Joining empty room name is not allowed anymore
- Fix: Clamp Room name length to 1024 chars

## [3.6.10] - 2023-06-14
### Exporter
- Bump engine version

### Engine
- Fix: Text with richText not updating properly
- Internal: Change font style parsing

## [3.6.9] - 2023-06-12
### Exporter
- Bump Engine version

### Engine
- Fix: Particles SizeOverLifetime module for mesh particles

## [3.6.8] - 2023-06-12
### Exporter
- Fix: LookAt component exception when being used in prefab

## [3.6.6] - 2023-06-12
### Exporter
- Bump Engine version

### Engine
- Internal updates

## [3.6.5] - 2023-06-09
### Exporter
- Bump Engine version

### Engine
- Add: NestedGltf `loaded` event being raised when the glb has been loaded
- Add: AnimationCurve cubic interpolation support
- Change: set targetFramerate to 60 by default (in `context.targetFrameRate`)
- Fix: USDZ metalness/roughness potentially being undefined when exporting Unlit materials
- Fix: Handle exception when loading components due to bug when using meshopt compression and material count changes
- Fix: ColorAdjustments setting tonemapping exposure to 0 when exposure parameter override is off [824]

## [3.6.4] - 2023-06-02
### Exporter
- Bump engine version

### Engine
- Add: `ObjectUtils.createPrimitive` for cube and sphere
- Change: expose `ObjectUtils`
- Fix: BoxGizmo component
- Fix: vite copy plugin when needle.config.json "assets" directory starts with "/"

## [3.6.3] - 2023-06-01
### Engine
- Change: OrbitControls apply settings in update
- Fix: Rapier stripping not being respected

## [3.6.2] - 2023-06-01
### Exporter
- Change: Try fix curl 60 error when server is already running on http

### Engine
- Fix: wrong UI z-offset in some cases
- Fix: Particle velocity over lifetime not using world rotation
- Fix: Particle burst being played twice
- Fix: Particle `playOnAwake` option not being respected

## [3.6.2-pre] - 2023-05-31
### Exporter
- Bump Engine Version

### Engine
- Add: `setAllowOverlayMessages` to explictly disable overlay messages without url parameter
- Add: allow larger textures for USDZ generation
- Fix: nested gltf with disposing of resources leading to broken files

## [3.6.1-pre] - 2023-05-29
### Exporter
- Change: enable ProgressiveTexture compression by default. Use the `ProgressiveTextureSettings` component to explictly disable it

### Engine
- Fix: removing `<needle-engine>` from DOM does now dispose the context properly and unsubscribes from browser events. Add `keep-alive` attribute to disable disposing

## [3.6.0-pre] - 2023-05-29
### Exporter
- Add: `ScreenSpaceAmbientOcclusionN8` component
- Bump Engine version

### Engine
- Add callbacks for ContextClearing
- Add: [n8AO postprocessing effect](https://github.com/N8python/n8ao) (Screenspace Ambient Occlusion) support
- Add: option to disable automatic poster generation (use `noPoster` in options in vite.config) 
- Fix: `<needle-engine>` without any src should setup an empty scene
- Change: `OrbitControls.fit` now handles empty scene and ignores GridHelper
- Change: TimelineAudio disconnect audio in onDestroy
- Change: Ensure PostProcessing VolumeParameters are initialized
- Change: Improve memory allocs and disposing of resources
- Change: Update three.js fixing GLTFLoader memory leak

## [3.6.0-exp] - 2023-05-27
### Exporter
- Fix: Exception when npmdef package had no `devDependencies` key
- Bump Engine version

### Engine
- Add: Changing `src` attribute now does scene cleanup and loads new files
- Add: `skybox-image` and `environment-image` attributes, allow changing both at runtime 
- Fix: error display overlapping in cases where somehow engine is imported twice
- Fix: logo overlay should only show when loading is done, change error during render loop message
- Fix: OrbitControls camera fitting now done once before rendering when loaded glb does not contain any camera
- Fix: Vite client plugin imports
- Change: Context now handle errors during initializing or when starting render loop
- Change: ContextRegistry exported as NeedleEngine and export hasIndieLicense function
- Change: Remove need to manually define global engine variables in cases without bundler or Needle plugins

## [3.5.13-pre] - 2023-05-26
### Exporter
- Change: ExportInfo editor performance improvements: check if npm is installed only once per session, run project validation on thread, dont collect template files in onEnable
- Fix: Prevent spawning more than one "npm installed" check task

### Engine
- Change: OrbitControls camera fitting improved

## [3.5.12-pre] - 2023-05-24
### Exporter
- Add: `IAdditionalFontCharacters` interface to allow components to add additional characters for font atlas generation
- Change: schedule Font export task to be awaited at end of export
- Change: GltfValueResolver should export Object3D node reference instead of Transform if referencing GameObject in UI hierarchy

### Engine
- Add: option to toggle collider visibility from script via `this.context.physics.engine.debugRenderColliders` 
- Change: engine.physics raycast doesnt need any parameters now anymore
- Change: OrbitControls default target should be related to distance to center (if nothing is hit by raycast)
- Fix: EventList object and component argument deserialization

## [3.5.11-pre.1] - 2023-05-22
### Exporter
- Fix: missing texture for importer overrides inspector header

## [3.5.11-pre] - 2023-05-22
### Exporter
- Bump Engine Version

### Engine
- Add: `@registerType` decorator that can be added to classes for registration in TypeStore. Currently only useful for cases outside of Unity or Blender for Hot Reload support
- Fix: `Component.name` should return Object3D name
- Fix: GameObject static methods generic
- Fix: Logo animation causing browser scrollbar to appear

## [3.5.10-pre] - 2023-05-22
### Exporter
- Add: SpriteRenderer now exposes shadow casting and transparency options via SpriteRendererData component

### Engine
- Add: SpriteRenderer now exposes shadow casting and transparency options
- Fix: vite plugin issue caused by missing src/generated/meta
- Fix: nullref in debug_overlay, typo in physics comment
- Fix: disabling collider with rigidbody component did cause an error in rapier
- Fix: HTMLElement css, cleanup loading element, move logo into html element
- Fix: GameObject.addComponent now takes Object3D type too
- Fix: loading overlay not hiding when <needle-engine> src changes
- Change: OrbitControls now sets target to 10 meter by default if nothing is assigned or hit in the scene (previously it was set to 1 meter)
- Change: fit camera to scene after loading when no camera is present in file

## [3.5.9-pre.2] - 2023-05-20
### Exporter
- Fix: Component links should use default app

### Engine
- Add: WebXRPlaneTracking should initiate room setup on Quest when no tracked planes are found

## [3.5.9-pre.1] - 2023-05-19
### Exporter
- Fix: DeployToGlitch
- Internal: move compression components into Needle AddComponent sub-menu

### Engine
- Fix: SceneSwitcher should ignore swipe events when `useSwipe` is disabled

## [3.5.9-pre] - 2023-05-19
### Exporter
- Change: when using a custom reflection texture use the texture size
- Fix: issue where npmdef to react-three-fiber package was being removed when generating the project

### Engine
- Add: Support for progressive texture loading for custom shaders
- Fix: react-three-fiber template

## [3.5.9-exp.2] - 2023-05-18
### Exporter
- Bump Needle Engine package

### Engine
- Add: needle-engine attributes documentation
- Change: assign main camera during gltf component deserialization when no camera is currently assigned

## [3.5.9-exp] - 2023-05-18
### Exporter
- Change: allow opening component links with default editor too (when VSCode is unticked in Needle settings)
- Change: clear .next/cache directory on full export

### Engine
- Add: add nextjs plugin to handle transpiling and defines
- Change: expose USD types to make custom behaviours, add proximityToCameraTrigger
- Fix: loading element position to absolute to avoid jumps when added to e.g. nextjs template
- Fix: texcoords werren't quicklook compatible in ThreeUSDZExporter
- Fix: `LookAt` component with invertForward option was flipped vertically in QuickLook

## [3.5.8-exp] - 2023-05-16
### Exporter
- Add option to settings to open web projects and files with default code editor (e.g. Rider)
- Add NeedleConfig `baseUrl` for codegen e.g. when the served file path is not the local path (e.g. `./public/assets` but server url is `./assets`)
- Change: improve check for http and https, remove usage of UnityWebRequest because is logs ssl error that we can not prevent when pinging local server urls
- Change: dont append toktx path as argument anymore when running build command, it is automatically discovered by build pipeline
- Fix: NullReferenceException in ProjectGenerator

### Engine
- Add NeedleConfig `baseUrl` for codegen
- Change: AudioSource should pause in background on mobile
- Fix: logo svg import for nextjs
- Fix: particle system playOnAwake

## [3.5.7-exp] - 2023-05-15
### Exporter
- Add: Initial support for text in USDZ
- Fix: EditorSync, prevent error caused by serialization of UnityObject
- Fix: Components can now reference RectTransforms
- Change: expose `SyncedRoom.tryJoinRoom` method
- Change: add some more information to networking components
- Bump UnityGLTF fixing issues with material animation export

### Engine
- Add: Initial support for text in USDZ
- Change: add generic to `networking.send` for validation of model
- Change: SyncedRoom, expose tryJoinRoom method + remove error thrown when roomName.length <= 0, join room in onEnable
- Fix: setting position on UI object (RectTransform) works again
- Fix addressable instantiate options called with `{ position: .... }` and without a parent, it should then still take the scene as the default parent
- Fix: WebXR `arEnabled` option
- Fix: Worldspace canvas always being rendered on top
- Fix: CanvasGroup alpha not being applied to text

## [3.5.6-exp] - 2023-05-12
### Exporter
- Add component tags for easier searching of Everywhere Actions (USDZ/QuickLook support)

### Engine
- Add: `addComponent` method to this.gameObject
- Add: "light" version on bundle processing
- Add: bundled library now comes with `light` variant to be installed from cdn (e.g. [`needle-engine.light.min.js`](https://unpkg.com/@needle-tools/engine@3.5.6-alpha/dist/needle-engine.light.min.js))
- Remove: some spurious logs
- Fix: defines for vanilla JS usage
- Fix: CanvasGroup not overriding alpha

## [3.5.5-exp] - 2023-05-11
### Exporter
- Change license display: holt ALT to show clear text + trim whitespace
- Bump engine version

### Engine
- Add: getWorldDirection
- Add: needle.config.json `build.copy = []` to copy files on build from arbitrary locations into the dist folder for example:
  ```md
  "build": {
    "copy": [
      "cards" <-- can be relative or absolute folder or file. In this case the folder is named "cards" in the web project directory
    ]
  }
  ```
- Add ip and location utils
- Change: add buffers for getWorldQuaternion, getWorldScale util methods
- Change: animatorcontroller should only log "no-action" warning when in debug mode
- Fix: apply and check license

## [3.5.4-exp] - 2023-05-11
### Exporter
- Change: introduce FileReference and derive ImageReference from it, add FileReferenceTypeAttribute. It can be used to copy any file type from Unity to the desired output directory without modification or going through the exporter to, for example, reference `usdz` files.

### Engine
- Fix: wrong serialization check if a property is writeable
- Fix: mark UI dirty when text changes
- Change: allow UI graphic texture to be set to null to remove any texture/image
- Change: rename USDZExporter `overlay` to `branding`

## [3.5.3-exp.1] - 2023-05-10
### Engine
- Fix: wrong check in serialization causing particles to break (introduced in 3.5.3-exp)

## [3.5.3-exp] - 2023-05-10
### Exporter
- Change: hold ALT to show Netlify access key

### Engine
- Add: `IPointerMoveHandler` interface providing `onPointerMove` event while hovered
- Add: USDZ AudioSource support and PlayAudioOnClick
- Change: balloon messages can now loop
- Change: pointer event methods are now lowercase
- Change: allow `moveComponent` to be called with component instance that was not added to a gameObject before (e.g. created in global scope and not using the `addComponent` methods)
- Fix: input pointer position delta when browser console is open
- Fix: GameObject.destroy nullcheck
- Fix: typescript error because of import.meta.env acccess
- Fix: issue where added scenelighting component by extension caused animation binding to break
- Fix: UI layout adding objects dynamically by setting anchorMinMax
- Fix: Prevent exception during de-serialization when implictly assigning value to setter property
- Fix: screenspace canvas being rendered twice when using explicit additional canvas data component
- Fix: EventSystem cached state of hovered canvasgroup not being reset causing no element to receive any input anymore after having hovered a non-interactable canvasgroup once
- Fix: empty array being returned in `GameObject.getComponents` call when the passed in object was null or undefined

## [3.5.2-exp] - 2023-05-09
### Exporter
- Add: SceneSwitcher preload feature
- Change: USDZBehaviours can now be enabled on USDZExporter component

### Engine
- Add: SceneSwitcher preload feature
- Change: interactive behaviours for QuickLook are on by default now
- Fix: SetActiveOnClick toggle for QuickLook
- Fix: USDZ texture transform export works in more cases

## [3.5.1-exp] - 2023-05-09
### Exporter
- Change: Allow overriding the default GltfValueResolver

### Engine
- Fix: reflection probes not working anymore
- Fix: false RectTransform return breaking some cases with reparenting
- Fix: RectTransform mark dirty when anchors change (due to animation for example)

## [3.5.0-exp] - 2023-05-08
### ⭐ Highlights
#### **Tree-shake Rapier / Physics engine**
With this version the physics engine can be marked to be removed in bundles reducing the overall `needle-engine` size by 600 KB (when using gzip) or 2 MB (without gzip). See the [documentation](https://fwd.needle.tools/needle-engine/docs/compression) for more information

#### **Choose between draco and meshopt mesh compression**
Add support to compress exported glTFs either with draco or meshopt compression. See the [documentation](https://fwd.needle.tools/needle-engine/docs/compression) for more information

#### **Various USDZ export fixes**
This release fixes various issues with USDZ export like exporting occlusion maps, texture input scale not being used and normal maps color space

### Exporter
- Add: vite plugin to watch package.json dependency changes to restart the server (can be disabled by adding `{noDependencyWatcher:true}` as a third parameter to the needle plugin)
- Add: `MeshCompression` component to be able to select compression per prefab/scene/gltfobject
- Add: `NeedleEngineModules` component to be able to remove rapier from bundle reducing overall engine size by 2MB (or 600KB with gzipping)
- Fix: nullref when adding new `DeployToFTP` component
- Fix: colorspace and texture flip issues in USDZ export in production builds (compressed texture readback)

### Engine
- Change: allow tree-shaking rapier physics
- Fix various USDZ export issues:
  - fix UV2 for occlusion maps (paves the way for lightmaps), had to be texCoord2f[] instead of float2[]
  - fix missing MaterialBindingAPI schema
  - fix normal scale for non-ARQL systems (ARQL doesn't support it though, but needed for other viewers)
  - fix input:scale for textures not being used if it's (1,1,1,1)
  - fix normal maps not being in raw colorSpace

## [3.4.0-exp.1] - 2023-05-05
### Exporter
- Fix: inspector injections stopped working

## [3.4.0-exp] - 2023-05-05
### ⭐ Highlights
#### **QuickLook Behaviours (experimental)**
This version adds support for interactive USDZ files for iOS devices. A number of built-in components work out of the box, with more to come! Try the USDZExporter sample to see for yourself. The high-level components will likely change over the next releases, but now is a great time to experiment and provide feedback.  

#### **AR Image Tracking**
AR Image Tracking is now available! Place content on trackable, configurable markers. On Android, it requires Chrome and currently the flag `chrome://flags#webxr-incubations` needs to be enabled. On iOS, Image Tracking works without additional settings.

#### **UI Improvements**
This version adds initial support for Vertical- and Horizontal LayoutGroups for Unity's UI Canvas System.

### Exporter
- Add: high-level USDBehaviours components: ChangeMaterialOnClick, PlayAnimationOnClick, SetActiveOnClick, HideOnStart
- Add: DeployToFTP: add option to disallow toplevel deployment
- Add: SamplesWindow filtering by tags and sorting by priority
- Change: various editor performance improvements
- Change: add @types/three when generating new NpmDefs
- Bump UnityGLTF dependency including fixes for NaNs in Unity's tangents and sorting of AnimationClip channels

### Engine
- Add: low-level USD Actions/Triggers API for building complex interactions for iOS devices
- Add: high-level USDBehaviours components: ChangeMaterialOnClick, PlayAnimationOnClick, SetActiveOnClick, HideOnStart
- Add: LookAt component now supports iOS AR
- Add: more settings for LookAt
- Add: support for Horizontal- and VerticalLayoutGroup (UI)
- Fix: `setWorldScale` was setting incorrect scale in some cases
- Fix: WebXR Image Tracking now works with WebARSessionRoot / rig movements
- Fix: vite reload only when files inside "assets" change and only if its a known file type
- Fix: UI scale set to 0 not being applied correctly

## [3.3.0-exp] - 2023-05-02
### ⭐ Highlights 
#### **Screenspace UI and improved RectTransform support**  
This versions updates to latest three-mesh-ui 7.x and adds support to correctly apply RectTransform anchoring and pivot settings as well as the ability to create screenspace UI (both modes for screenspace overlay and screenspace camera are supported)

### Exporter
- Add: deploy to github pages component
- Add: Linked npm package support
- Fix: recursively installing locally referenced packages
- Fix: check if scene is saved before trying to export when not using any GltfObject
- Fix Unity warning when exporting canvas in scene without GltfObject
- Change: SceneSwitcher now allows assigning both prefabs and scenes

### Engine
- Add: AssetReference can now handle scene reference
- Add: UI update with support for screenspace UI, anchoring, pivots, image outline effect, image pixelPerUnit multiplier
- Add: basic LookAt component
- Add: basic UI outline support + handle builtin Knob image
- Add: WebXRImageTracking ability to directly add a tracked object to an image marker
- Fix: OrbitControls should only update when being the active camera
- Fix: UI input ignored browser "mouseDown" for each "touchUp" event
- Fix: OrbitControls requiring additional tab after having clicked on UI
- Fix: OrbitControls only being deactivated when down event starts on UI element
- Fix: loading bar text not being decoded (displayed e.g. `%20` for a space)
- Fix: TransformGizmo not working anymore

## [3.2.15-exp] - 2023-04-28
### Exporter
- Add: USDZExporter exposes download usdz file name

### Engine
- Add: SceneSwitcher.select(AssetReference) support to be invoked from a UnityEvent with an object reference (must be an asset)
- USDZExporter: change exported usdz name, remove needle name for license holders

## [3.2.14-exp] - 2023-04-28
### Exporter
- Add: OpenURL component
- Fix: USDZ export breaking if the object name is just a number 
- Fix: allow to specify local three version in package

### Engine
- Add: OpenURL component
- Change: Implictly add Raycaster to scene if it is not found.
- Fix: USDZ export breaking if the object name is just a number 

## [3.2.13-exp.1] - 2023-04-27
### Exporter
- Fix: Vite template missing `base: "./"` for FTP subfolder deployment
- Fix: Vite template missing `server.proxy` option for HTTP2
- Change: DeployToFTP can now run `Build & Deploy` even if the project was never built before

## [3.2.13-exp] - 2023-04-27
### Exporter
- Add: USDZExporter editor shows warning if no objects are assigned and exposes quicklook overlay texts
- Add: USDZExporter callToActionButton can now invoke url to open
- Change: EditorSync improved feedback during installation
- Change: Remove Copy files run from editor, run copy files on via vite plugin
- Change: remove console log in pro license
- Fix: Fix vite html transform plugin
- Fix: EditorSync false check if Materials were enabled, otherwise it would not inject
- Fix: minor SemVer warning

### Engine
- Add: USDZExporter editor shows warning if no objects are assigned and exposes quicklook overlay texts
- Add: USDZExporter callToActionButton can now invoke url to open
- Add: SceneSwitcher can now use history without updating the url parameter
- Fix: Fix vite html transform plugin

## [3.2.12-exp] - 2023-04-26
### Exporter
- Change: ProcessHelper should fail if working directory doesnt exist
- Change: ProcessHelper starts command windows minimized
- Change: BugReporter can now run without web project
- Fix: BugReporter should run by using Needle managed tools package
- Fix: When mesh compression `override` was enabled the `useSimplifier` would not be used

### Engine
- Fix: issue where removing an object from the scene would disable all its components

## [3.2.11-exp] - 2023-04-25
### Exporter
- Bump Needle Engine version
- Bump Tools package version

### Engine
- Fix: lighting settings being implictly switched (enabled/disabled) when using SyncCamera / any loaded prefab at runtime

## [3.2.10-exp] - 2023-04-25
### Exporter
- Remove: creation of legacy `scripts.js` file
- Change: improve first time installation logs
- Change: Clean install now recursively runs for locally referenced packages
- Change: EditorSync now can allow camera sync only / only inject materials if enabled and only inject other properties if `components` sync is enabled
- Change: EditorSync should disable scene camera sync when a scene is closed to not lock camera view in browser
- Change: EditorSync: schedule reconnect exponentially slower over time if it fails

### Engine
- Fix: Remove log in `Animator.SetTrigger`
- Fix: GroundEnv radius property setting wrong value internally
- Fix: Apply license to unnamed local vite chunk files

## [3.2.9-exp] - 2023-04-23
### Exporter
- Change: ExportInfo big install button should run clean install silently if the project does not exist at all
- Change: Cleanup vite template config

### Engine
- Fix: VideoPlayer not restarting when enable/disable being toggled
- Fix: Builtin serializer for URLs `@serializable(URL)` should ignore empty strings
- Change: set `enabled` to true before calling `onEnable`
- Change: VideoPlayer now deferrs loading of the video until the video should play
- Change: ScreenSharing component now changes cursor pointer on hover to indicate that is can be clicked

## [3.2.8-exp] - 2023-04-23
### Exporter
- Add: DeployToNetlify component
- Change: SceneView now shows server start information
- Change: Improve npm installation logs in Unity and run installations in sequency rather than in parallel
- Change: automatically update workspace title making it easier to work with multiple VSCode editors open
- Change: wait a bit longer before opening browser URL (mainly for safari not refreshing when the vite server takes a bit longer to fully start)
- Change: remove npmdef dependencies in temporary projects (in Library/) when they have not been added explitly in the Unity scene (this is useful when switching many sample projects where one web project is shared for many Unity scenes that might use different local packages - when switching many scenes more and more dependencies would been added to the project altough only few were actually used by the current example scene)
- Fix: font export where font name is "Arial" but font file name is "arial"
- Fix: npmdef dependency path update (remove unnecessary log, only write dependencies if they've actually changed)
- Fix: Catch some timeline export bugs when animation window is open but has no clip

### Engine
- Add: this.context.getPointerUsed(index) and setPointerUsed(index)
- Change: physics now by default receives collisions/triggers between two colliders that are set to trigger

## [3.2.7-exp] - 2023-04-22
### Exporter
- Change: reduce warnings when font style could not be found
- Change: improve switching of scenes in samples repository where packages are added to shared project

### Engine
- Change: ambient light does now look closer to Unity ambient light
- Fix: guard calls to component event methods when the component or object has been destroyed

## [3.2.6-exp] - 2023-04-21
### Exporter
- Fix_ editor sync for enums
- Change: Delete package.lock.json when installing

### Engine
- Add: SceneSwitcher has now option to automatically set scene environment lighting
- Fix: Issue caused by NeedleEngineHTMLElement import from SceneSwitcher
- Change: Allow component to be disabled in awake (e.g. calling `this.enabled = false` in awake callback)
- Change: Export more types e.g. AnimatorStateMachineBehaviour
- Change: VolumeParameter.value should return rawValue (unprocessed)
- Change: rename "rendererData" to "sceneLighting"
- Change: scene lighting must now be enabled explictly when additional scene are being loaded, use `this.context.sceneLighting.enable(...)` with the AssetReference or sourceId of the scene you want to enable

## [3.2.5-exp] - 2023-04-20
### Exporter
- Add: Occluder mode to ShadowCatcher component
- Add: WebXRPlaneTracking

### Engine
- Add: WebXRPlaneTracking
- Add: `<needle-engine loading-style="light">` for a brighter loading screen
- Fix: InputField.onEndEdit should send string
- Change: move webxr into subfolder
- Change: export more types

## [3.2.4-exp] - 2023-04-20
### Exporter
- Add: auto updater for scripts importing types using `engine/src` paths (to skip auto update add `// @noupdate` in the beginning of your file)
- Internal: NpmDef devDependency is now set to current local engine if the current project does use a locally installed engine package

### Engine
- Change: export more types (e.g. `syncField`)
- Fix: PlayerSync
- Fix: Environment lighting
- Fix: license check

## [3.2.3-exp] - 2023-04-20
### Exporter
- Change: bump UnityGLTF dependency to `2.0.0-exp.2`

### Engine
- Fix: VideoPlayer AudioOutput.None should mute video
- Fix: SpriteRenderer applies layer from current object (e.g. for IgnoreRaycast)

## [3.2.2-exp] - 2023-04-19
### Exporter
- Change: Bump engine version

### Engine
- Fix: issue where the environment lighting would be falsely disabled
- Change: minor improvements to initial state of the SceneSwitcher

## [3.2.1-exp] - 2023-04-19
### Exporter
- Remove: New shaders will not be changed anymore
- Change: DriveHelper now runs in background to prevent long stalls on windows call
- Fix: timeline signal asset export

### Engine
- Change: SceneSwitcher clamp option
- Change: timeline signals without bound receiver are now invoked globally on all active SignalReceivers with the specific signal asset
- Change: internal check preventing errors during initialization for projects where the package is falsely added multiple times to the project by importing from internal types directly instead of `from "@needle-tools/engine"`

## [3.2.0-exp] - 2023-04-19
### Exporter
- Add gzip option to DeployToFTP and always enable gzip compression for DeployToGlitch
- Fix minor Unity warnings
- Change: Allow exporting root scene without GltfObject

### Engine
- Add: built-in SceneSwitcher component
- Change: VideoPlayer.playInBackground is set to true by default
- Change: Screensharing should continue playback of receiving video when the sending tab becomes inactive
- Change: log additional information when button events can not be resolved
- Change: AudioSource.playInBackground set to true by default to allow audio playback when tab is not active
- Change: syncField can now take function argument
- Change: Renderer.sharedMaterials can now be iterated using `for(const mat of myRenderer.sharedMaterials)`
- Fix: lightmap not being resolved anymore
- Fix: environment lighting / reflection not switching with scenes
- Fix: progressive texture did not check if the texture was disposed when switching to an unloaded scene resulting in textures being black/missing
- Fix: timeline does enable animator again when pausing/stopping allowing to switch to e.g. idle animations controlled by an AnimatorController
- Fix: changing material on renderer with lightmapping will now re-assign the lightmap to the new material

## [3.1.0-exp.3] - 2023-04-18
### Exporter
- Fix: font export not working when tools helper package was not yet initialized
- Fix: NestedGltf exporting wrong file path

## [3.1.0-exp.2] - 2023-04-18
### Exporter
- Fix: UI font path export

### Engine
- Fix: UI font style resolving

## [3.1.0-exp.1] - 2023-04-18
### Engine
- Fix: RemoteSkybox not being able to load locally reference dimage
- Fix: ParticleSystem sphere scale not being applied anymore
- Fix: WebXRImageTracking url not being resolved

## [3.1.0-exp] - 2023-04-18
### Upgrade Guide
With version 3.x the Needle Engine Unity integration will install Needle Engine from npm instead of installing a separate Unity package and installing it by filepath. This change is an important step to alig the Unity integration with Blender and all future integrations.

After upgrading please make sure to apply the following changes:

- Open your web project package.json and check that the dependency for `three` does not contain an old `file:` path to a previous installation. It may be necessary to change the value from `"file:/path/"` to `""` (empty string) so that the Unity integration can fill in the correct version. You may also remove the explicit dependency to `three` completely if you are not using e.g. react-three-fiber.

- Open the `vite.config.js` and make sure to remove the custom `alias` configuration for `@needle-tools/engine` and `three`

- If you have been using the Unity `ImageReference` class to export images to external files you should change your runtime code to use the new typescript `ImageReference` class as well (using `@serializable(ImageReference)`)

- The `build:dev` script not contain an extra `tsc` compile call

### ⭐ Highlights 
#### **Needle Engine is now installed from NPM**  
Needle Engine in Unity is now also installed from NPM. This is an important step to align Unity, Blender and all future integrations. It will also make it easier to publish projects on platforms like Netlify without having to modify the web project. Please see the changelog for the Upgrade Guide.

#### **WebXR ImageTracking**  
Add the `WebXRImageTracking` component to your scene and assign images to be tracked. Currently requires the `webxr-incubations` chrome flag to be enabled.

#### **Screensharing**
Reliability when making new connections or joining a room with an active screensharing session has been improved.

### Exporter
- Add: automatically update npm dependencies for certain packages (e.g. `@needle-tools/engine`) when a normal Semversion is being used
- Add: initial experimental component import support allowing to import glTF files from Blender into Unity with their components intact (similarly glTF files that have been exported in other Unity projects can now be imported including their components) 
  ↪ **NOTE**: this feature is experimental and not yet production ready. It needs further testing and import does not yet work for all types (known issues where import does not yet work include ParticleSystems and AnimatorControllers where states have missing animation clips)
- Change: bump UnityGLTF dependency to `2.0.0-exp` for import plugin API
- Change: build pipeline tools are now run from an internal package, this removes the need to have a web project setup to export and compress glTF files (e.g. during CI or when using the context menu item on an model or prefab asset)
- Change: remove dependency to extra Unity package and local engine installation.
- Change: paths to external files are not relative to the exported glb (previously they did contain the full path relative to the project root) - this allowed modifications to `needle.config.json` assetsDirectory to work when the folder structure for the deployed version is different to the development structure. NOTE: if you've been exporting external images using `ImageReference` you can now use the new `ImageReference` runtime type to easily resolve them
- Change: Compressed glTF export is now possible without web project
- Change: Full Export now does not restart the server but deletes both Needle Engine as well as vite caches
- Fix: Projects using `needle.config.json` to modify the assets directory are now being built correctly

### Engine
- Add: `ImageReference` type, use to export textures to external files and load them as `img`, `texture` or to get the binary data for e.g. image tracking
- Add: api for `WebXRImageTracking`, this does currently require the ``webxr-incubations`` flag to be enabled
- Add: TiltShift postprocessing effect
- Add: AnimatorController support for negative speed
- Add: `this.context.xrFrame` to get access to the current XRFrame from every lifecycle event method
- Add: `<needle-engine>` loading visuals can now be customized by commercial license holders
- Change: ParticleSystem now has a reset method to allow for clearing state, stop has options for calling stop on Sub-Emitters and to clear already emitted particles
- Change: license check is now baked
- Change: Rename "EngineElement" to NeedleEngineHTMLElement
- Change: disable "Enable Keys" on OrbitControls by default as it conflicts with so many things
- Fix: ParticleSystem circle shape
- Fix: balloon messages are now truncated to 300 characters
- Fix: Screensharing connection setup and start of video playback
- Fix: Screensharing muting now local audio
- Fix: AudioSource does not play again when it did finish and the user switches tabs
- Fix: ParticleSystem prewarm
- Fix: ParticleSystem minMax size, it's currently not supported and should thus not affect rendering
   
## [2.67.16] - 2023-04-13
### Exporter
- Change: Improved handling of error during export if referenced scenes have the same name causing an IOException. Regular export now still continues and an error with some more information is being logged.
- Fix: Exception in attribute drawer
- Fix: Nullreference exception from EditorSync when trying to re-assign a missing script

### Engine
- Change: postprocessing DOF exposes resolution scale and takes device pixel density into account. By default the resolution is slightly lowered on mobile devices

## [2.67.16-pre.1] - 2023-04-12
### Exporter
- Fix: key exception in ExportInfo version check

## [2.67.16-pre] - 2023-04-12
### Exporter
- Change: dont change font name casing

### Engine
- Add: static ``AudioTrackHandler.dispose`` for disposing loaded audio data in timeline
- Fix: issue where only the first audio clip would be played in a timeline with multiple audio clips of the same file
- Change: Text should not change font name casing
- Change: Timeline does now wait for audio and first interaction by default (if any audio track is being used, this can be disabled by setting `waitForAudio` to false on the PlayableDirector component)

## [2.67.15-pre] - 2023-04-12
### Exporter
- Change: Automatically use PBRGraph or UnlitGraph for known shaders when creating a new material
- Change: adding nullchecks to DriveHelper, it seems a drive or drive name can also be null
- Change: show "MODULE NOT FOUND" as error in Unity

### Engine
- Fix: Issue where ControlTrack was not being able to resolve bound timeline
- Fix: issue with font generation where font file name contained a dot

## [2.67.14-pre] - 2023-04-12
### Exporter
- Add: symlink support check (FAT32 and exFAT)

### Engine
- Change: WebXR camera now copies culling mask from main camera
- Fix: WebXRController raycast on all layers
- Fix: WebXR all layers should be visible
- Fix: set pointer position properly on mouse down to prevent jumps in delta
- Fix: respect IgnoreRaycast in physics raycasts
- Fix: issue with CircularBuffer where sometimes the same item was returned twice
- Fix: boxcolliders with scale 0 (such as adding a BoxCollider to a plane) resulted in flipped normals being returned from raycasts
- Fix: parenthesis error in CharacterController
- Fix: issue with mouse vector position being re-used causing delta position being falsely modified

## [2.67.13-pre] - 2023-04-11
### Exporter
- Add: disc formatting check for FAT32

### Engine
- Fix: Animation component settings
- Fix: instanced renderer matrix auto update
- Change: enable shadow casting in instanced rendering when any mesh has castShadow enabled
- Change: export ui pointer events

   
## [2.67.12-pre] - 2023-04-09
### Exporter
- Add: Support exporting immutable scenes (e.g. when referencing a scene in an immutable package)
- Add: SSAO color and luminance influence options
- Fix: handle invalid formatting of vscode workspace json
- Change: Clicking on missing script (uninstalled npmdef / rendered in red font) now pings npmdef (you can double click to still open the script)
- Change: try to find toktx in default install directory on windows and not show warning/error when user has it installed (but not in parth)
- Change: Rename `CustomPostprocessingEffect` to `PostprocessingEffect` to make codegen work (e.g. when creating a custom PostProcessingEffect)

### Engine
- Add: SSAO color and luminance influence options
- Change: postprocessing now exposes effect order

## [2.67.11-pre] - 2023-04-08
### Exporter
- Add: support for exporting skybox and fog settings for referenced scenes
- Fix: ExportInfo getting stuck without install button
- Fix: ProcessHelper remove invalid control characters that might come in from external process breaking logs / showing incomplete information
- Change: Delete broken Unity package install directory (e.g. when hidden Needle Engine directory exists but is empty)
- Change: Show info in "Install Project" Button tooltip what is missing / why it's not installed
- Change: update react template

### Engine
- Add: some checks for WebGPURenderer

## [2.67.10-pre] - 2023-04-06
### Exporter
- Add: tags to samples window
- Change: hold ALT to restart local server (when server is running)
- Change: EditorSync: install ^1.0.0-pre by default

### Engine
- Add vite copy files build plugin
- Fix: PostProcessing not applying effects when enabled for the second time as well as removing earlier hack
- Change: update user-select/touch-action in project templates style.css to prevent accidental iOS selection of canvas
- Change: disable text selection on Needle logo
- Bump three version, see changes below

### Three
- change USDZExporter: pass writer into onAfterHierarchy callback, move onAfterHierarchy callback after scene hierarchy write
- fix USDZExporter: fix exception when trying to process render targets
- fix WebXRManager: Correctly update the user camera when it has a parent with a non-identity transform.

## [2.67.9-pre] - 2023-04-03
### Exporter
- Add: Bug Report upload functionality
- Fix: spritesheet export and display for non-similar sprite shapes
- Change: Improve feedback when nodejs is not installed
- Change: run install in local package.json dependencies
- Change: optimized Spritesheet data export resulting in smaller files

### Engine
- Change: SpriteRenderer material to transparent
- Bump: tools package dependency

## [2.67.8-pre.1] - 2023-03-31
### Exporter
- Bump engine version
- Bump UnityGLTF dependency

### Engine
- Fix: exception when using BoxColliders caused by error in initial activeInHierarchy state assignment

## [2.67.8-pre] - 2023-03-31
### Exporter
- Bump engine version

### Engine
- Fix: vite plugins must have a name
- Fix: activeInHierarchy update when key is undefined (e.g. in r3f context)
- Change: cleanup r3f component

## [2.67.7-pre] - 2023-03-30
### Exporter
- Internal: samples can now have tags

### Engine
- Add: time smoothed delta time
- Add this.context.targetFrameRate
- Fix: enum / type conversion errors
- Fix: CanvasGroup overriding text `raycastTarget` state in event handling causing problems with button events
- Fix: Text z-fighting from invisible ThreeMeshUI frame object
- Change: Canvas `renderOnTop` moved into separate render call to avoid ordering issues and postprocessing affecting overlay UI
- Internal: Move context from engine_setup to engine_context

## [2.67.6-pre] - 2023-03-30
### Exporter
- Fix: reset blurred skybox color

### Engine
- Fix: Postprocessing enforce effect order
- Change: gizmos should not render depth

## [2.67.5-pre] - 2023-03-30
### Exporter
- Fix: issue where context menu export didnt export all known components
- Fix: dont import npmdef types multiple times

### Engine
- Fix: issue where postprocessing did not check composer type (e.g. when using threejs composer instead of pmndrs package)
- Change: Postprocessing now uses stencil layers

## [2.67.4-pre] - 2023-03-28
### Exporter
- Bump engine version

### Engine
- Change: Postprocessing effects value mapping / settings improved (Bloom & ColorAdjustments)

## [2.67.3-pre] - 2023-03-28
### Exporter
- Bump engine version

### Engine
- Fix: issue where progressive textures would not be applied correctly anymore
- Fix: Timeline audio loading on firefox
- Fix: issue where progressive textures with reflection probes wouldn't be applied correctly anymore

## [2.67.2-pre] - 2023-03-28
### Exporter
- Fix: registry component codegen should not generate extra c# types for npmdefs that exist locally
- Change: bump UnityGLTF dependency

### Engine
- Change: calculations for rect transform animation offsets
- Change: Warn if engine element src contains a url without .glb or .gltf

## [2.67.1-pre] - 2023-03-28
### Exporter
- Change: disable soft restart button while installing EditorSync
- Remove: physics debug log when raycasting

### Engine
- Fix: PostProcessing failing to be re-applied after exit XR

## [2.67.0-pre] - 2023-03-27
### ⭐ Highlights 
#### **Editor Live Sync 🔴**  
Immediately see changes made in the Unity Editor in your three.js scene. Add the Needle Editor Sync component to your scene to get started.  

#### **More PostProcessing effects**  
Adding Bloom, Depth of Field, ColorAdjustments, Chromatic Aberration, Screenspace Ambient Occlusion, Vignette, AntiAliasing powered by pmndrs' postprocessing package.

#### **WebAR Camera Background**  
Taking scene screenshots in AR now also includes the camera image. This paves the way for adding custom camera and AR effects in future versions! 

### Exporter
- Add: **EditorSync** component and package to send changes in the editor to the three.js scene. In this first version it can be used to modify material properties, change certain component values at runtime, enable or disable objects as well as to render from the Unity scene camera. To use just add the EditorSync component to scene and click the Install button.
- Add: **More postprocessing effects**: Bloom, ChromaticAberration, ColorAdjustments, DepthOfField, Pixelation, Screenspace Ambient Occlusion, Tonemapping, Vignette. Custom effects can easily be implemented by deriving from the `PostProcessingEffect` base class
- Add: Support to adjust postprocessing effects from the Unity Editor when EditorSync is being used
- Add: **WebARCameraBackground** that can be used to apply effects to the camera image (or capture the image when taking scene screenshots while in AR) using the WebXR Raw Camera access API
- Add: NpmDef `Publish to npm` button
- Add: **Integration of using NpmDefs published to npm**, generating C# components from packages with Needle Engine components installed via npm. This allows to publish packages with Needle Engine typescript components and when installed in a Unity project the corresponding C# components will be generated and discovered on export.
- Change: DeployToFTP should not log error in check if a directory already exists
- Change: Show info in scene view when using SmartExport and scene would not be exported because nothing has changed
- Fix: Remove legacy `camera` field on `SyncCamera` component
- Fix: Typo in allowed camera fields causing `ARBackgroundAlpha` to not be exported
- Fix: wrong warning for NpmDef being missing when name was containing a `.` 
- Fix: Issue in welcome window where certain URLs were opened twice
- Fix: Issue where assembly reload lock would not be unlocked again 
- Other: Internal cleanup and deletion of a lot of legacy code that was originally used to build three.js scenes from javascript codegen
- Other: Improved menu items order and wording

### Engine
- Add: `this.gameObject.destroy` as a shorthand for `GameObject.destroy(this.gameObject)`
- Add: support for camera `targetTexture` to render into a RenderTexture (when assigned to e.g. the main camera in Unity)
- Add: utility methods to toplevel engine export (for example `getParam`, `delay`, `isMobileDevice`, `isQuest`)
- Add: first version of usage tracking of materials, textures and meshes. This is off by default and can be enabled using the `?trackusage` url parameter. When enabled `findUsers` can be used to find users of an object.
- Add: pmnders postprocessing package
- Change: improved PlayerState component and added event for PlayerState events for owner change (to properly listen to first assignment)
- Change: `AssetReference.unload` now calls dispose to free all resources
- Change: `WebXR` component now has static field for modifying optionalFeatures 
- Change: `Physics.RaycastOptions` now have a `setLayer` method to simplify setting the correct layer mask (similar to Unity's layers to enable e.g. just layer 4 for raycasting)
- Change: `RemoteSkybox` now requests to re-apply clear flags on main camera when disabling to restore previous state.
- Fix: issue where component instances were created using wrong method causing arrow functions to be bound to the wrong instance
- Fix: `@syncField` now properly handles destroy of an component
- Fix: react-three-fiber template
- Fix: ParticleSystem prewarm safeguard and additional checks where emission is set to 0
- Fix: Timeline only playing first audio clip
- Fix: Issue where scene with multiple root glbs cross-referencing each other were not being resolved correctly in all cases
- Fix: Progressive textures not being loaded when using tiling
- Fix: Text UI anchoring

## [2.66.1-pre] - 2023-03-16
### Exporter
- Add: additional option to Lightmap format dialogue to set the right format when exporting using unsupported lightmapping quality setting
- Fix: AutoInstall nullref if installing a package in a scene that has no Needle Engine project
- Fix: BRP `shadowBias` conversion
- Fix: unassigned methods in Unity Events ("No Function") were throwing an exception on export
- Fix: change detection (Smart Export) for referenced assets was not checking for changes in some cases
- Change: component header link now has file extension
- Change: Bump UnityGLTF dependency
- Change: allow specifying double jump force separately in CharacterController

### Exporter
- Add: `sphereOverlapPhysics` function for more accurate sphere overlap detection using rapier
- Fix: Gizmos should be excluded from being hit by raycasts
- Fix: Gizmo sphere radius was twice the desired size
- Fix: Physics now prevent negative collider scale
- Fix: renderer instancing auto update when using multi material objects
- Change: Show warning that stencil and instancing doesnt work together at the moment

## [2.66.0-pre.1] - 2023-03-14
### Exporter
- Fix: issue where scene assets wouldnt be exported properly anymore
- Fix: issue with very first web project installation in a new unity project

## [2.66.0-pre] - 2023-03-14
### Exporter
- Fix: AssetReferenceResolver should not export references on a asset root as separate glbs
- Fix: skip serialization for `transform` property on components
- Fix: Updater should trim leading whitespace when testing if line is import and needs to be updated
- Fix: npmdef button "Add to project" / "Remove from project" not updating workspace
- Fix: deprecated import in basic_component template
- Fix: NestedGltf should not dont traverse into EditorOnly objects
- Change: Export cache: check if the parent context is null (in case of e.g. export from prefab)
- Change: Prevent exporter from re-exporting glb with the same name multiple times per run. Instead print a warning. This can be caused by e.g. using nested gltfs or GltfObject components where the GameObject has the same name as another previously exported GltfObject
- Change: Full Export does now also kill local server, add "force" to search for node processes running current web project
- Change: Updater can now run for immutable package when in hidden directory
- Remove legacy transform.guid code that slowed down export in scenes with many objects

### Engine
- Add: particle system prewarm support
- Add: `poseMatrix` argument in WebARSessionRoot.placedSession event
- Add: MeshCollider minimal support for submeshes (they can be added but currently not removed from the physics world)
- Fix: debug_overlay error when rejection reason is null
- Fix typo beginListenBinrary → beginListenBinary
- Fix: particle system staying visible after disabling gameObject
- Fix particle system not finding subemitter in certain cases
- Fix: particles with subemitters and trails and disabled "die with particle" should emit subemitter when the particle dies (and not when the trail dies)
- Fix: Loading overlay now hides when loading is done and the first frame has finished rendering
- Change: rename networking `stopListening` to `stopListen` for consistency
- Change: addressables allow up to 10k instantiations per frame before aborting
- Change: set material shadowSide to match side
- Change: generate poster image with 1080x1080 px and add `og:image:width` and `og:image:height` meta tags

## [2.65.2-pre] - 2023-03-11
### Exporter
- Fix: custom shader export of uv2 etc
- Fix additional data button should now show up on model importer
- Change: dont run Updater automatically if `allowProjectFixes` is disabled (in Needle settings)
- Change: dont run Updater for script files in PackageCache
- Change: custom shader export now caches previously exported shaders (per export) when having a scene with multiple materials using the same custom shader
- Change: log warning when exporting with gzip disabled (it is disabled by default, please check your Build Settings)

### Engine
- Change: custom shaders should not log warning for unsupported ``OrthoParams`` shader property 
- Change: Animator methods starting with uppercase are marked as deprecated because UnityEvent methods are now exported starting with lowercase letter, added lowercase methods

## [2.65.1-pre] - 2023-03-10
### Exporter
- Change: Updater now locks assemblies during auto-update to avoid components from recompiling and aborting export or server start

### Engine
- Fix: ParticleSystem using `min/max` size in the renderer module is now minimally handled
- Fix: ParticleSystem emission when using local space with scaled parents
- Fix: ParticleSystem not finding SubEmitter systems
- Fix: ParticleSystem simulation speed not being applied to gravity and initial speed
- Fix: RemoteSkybox not resolving relative url correctly (when assigning a cubemap in the editor)

## [2.65.0-pre.1] - 2023-03-10
### Exporter
- Fix: issue where `<needle-engine>` element was not yet in the DOM when queried by exporter codegen which caused the paths to not be assigned and the engine to not load

### Engine
- Fix: issue where `<needle-engine>` element was not yet in the DOM when queried by exporter codegen which caused the paths to not be assigned and the engine to not load

## [2.65.0-pre] - 2023-03-09
### Exporter
- Add: Updater to fix wrong import paths due to change in engine package structure (`@needle-tools/engine/src/...`)
- Change: codegen for loading the exported glbs in main scene is now simplified, removing previous and legacy code completely. It now just collects all exported files in an array and sets that as the `src` attribute on the `<needle-engine>` web component. 
- Change: project settings should not show warning for Engine package not being installed when the current scene is not a web project
- Change: engine package should only be automatically installed on update when the current scene is a web project
- Change: type imports now generate without an extension to fix distributed `lib` imports
- Fix: error where unresolved package.json variable did cause IllegalChar exception
- Fix: improved npmdef error handling caused by the hidden package being missing which might happen if a user copies the npmdef to another Unity project but does not copy the hidden package folder (ending with ~). Such cases are now also properly displayed in the ExportInfo component
- Fix: BugReporter should not print error message about toktx not being installed when collecting project information

### Engine
- Add: runtime checks for recursive loading to prevent it from breaking
- Change: internal duplicate active state on `Object3D` has been removed, instead `visible` is used. This was previously a workaround for the `Renderer` setting the visible state when being enabled or disabled but this has been changed in a previous version and it now only sets a flag in the object's Layers instead (which allows for a single object in the hierarchy to not being rendered by setting `Renderer.enabled = false` while objects in the child hierarchy stay visible) 
- Change: `<needle-engine>` src attribute can now also take an array of glbs to load. This simplifies codegen done by exporters and also prevents errors due to bundler optimizations as well as being easier to understand. Runtime changes of the `src` attribute (especially when using arrays of files) have not been tested for this release. Networking for `src` changes has been removed in this release. 
- Change: move engine into src subfolder. All paths to explicit script files are now `@needle-tools/engine/src/...`
- Change: poster screenshot will be taken after 200ms now
- Change: canvas default set to false for castShadow and receiveShadow
- Change: Remote skybox should not set `scene.background` when in XR with pass through device (e.g. when using Quest Pro AR or AR on mobile)
- Fix: issue where ColorAdjustments Volume effect was applied with `active` set to false
- Fix: `Light` not being enabled again after disabling the component once

## [2.64.0-pre] - 2023-03-07
### Exporter
- Add: dependency to `csharp to typescript` package. 
  > This allows to quickly create typescript skeleton or stub components from existing csharp code
  > Created components try to import used types if known, create fields with `@serializable` decorations and methods (the method body needs to be implemented manually)
  > Use the context menu item on csharp script assets or on components in the scene
- Add: When your package.json contains a script named `install` the exporter will invoke this script instead of directly running `npm install`. This allows running projects that e.g. require `yarn install`
- Add: Export audio volume for timeline clip (Timeline audio track settings are not yet supported)
- Fix: Clean install when using project paths starting with `Packages/` or `Assets/`
- Fix: Issue where the `enabled` property of some component types was not exported anymore (e.g. colliders)
- Fix: Issue with license import when using vite 2 caused by BOM

### Engine
- Add: `PlayableDirector` now correctly applies timescale
- Add: `PlayableDirector.speed` property allowing to play the timeline at different speeds or even reverse (reversed audio playback is not supported at the moment)
- Add: `Physics.enabled` property for disabling physics updates. This also prevents any collider shapes to be created
- Add: `this.gameObject.transform` property to ease getting started for devs coming from Unity. This property is merely a forward to `this.gameObject` and shouldnt really be used. The property description contains information and a [link to the docs](https://fwd.needle.tools/needle-engine/docs/transform) with further information.
- Fix: instanced materials using progressive loading are now correctly updated
- Fix: Timeline animation tracks now disable the `Animator`. This fixes cases where two animations were played at the same time. When the PlayableDirector is paused or stopped the animator state is reset
- Fix: License styles leaking into website
- Fix: Timeline audio not stopping correctly at end of timeline when set to hold
- Change: improve instancing updates, instanced objects now auto update detect matrix changes. This includes improvements of instancing when used with `Animation` components
- Change: set particle system layers to `IgnoreRaycast` to not receive wrong click events on batched particle meshes
- Change: Timeline audio is now loaded on evaluation. Only clips in a range of 1 second to the current time are loading. To manually trigger preload of audio you can invoke `loadAudio` with a time range on audio tracks of a timeline

## [2.63.3-pre] - 2023-03-03
### Exporter
- Change: ExportInfo should install should not show too much information when ALT is pressed
- Change: project generator should not replace version for needle engine in package.json paths
- Fix: project generator should insert package path if the dependency is an empty string

### Engine
- Fix: engine published to npm was missing vite plugins

## [2.63.2-pre] - 2023-03-03
### Exporter
- Add: ExportInfo context menu `Internal/Move Project` to move a web project
- Change: allow web projects in Assets/ and Packages/ directories (when they're in a hidden folder like `Assets/MyProject~` or `Packages/com.my.package/MyProject~`)
- Change: ignore build.log
- Change: bump UnityGLTF dependency
- Fix npmdef open button, rename "path" to "name" because that's what it is
- Fix: deploy to ftp when server name starts with ftp.

### Engine
- Fix: license styling in some cases
- Fix: duplicatable + draggable issue causing drag to not release the object (due to wrong event handling)
- Fix duplicatable + deletion not working properly
- Fix: timeline breaking when time is set to NaN

## [2.63.1-pre] - 2023-03-02
### Engine
- Add: components now have `up` and `right` vectors (access via ``this.up`` from a component)
- Fix: import of license and logo for npm package

## [2.63.0-pre] - 2023-03-01
### Exporter
- Add: licensing information

### Engine
- Add: licensing information
- Add: logo to loading display
- Change: VideoPlayer now exposes VideoMaterial
- Change: Screencapture now only accepts clicks with pointerId 0 (left mouse button, first touch) to toggle screen capture
- Change: expose physics gravity setting `this.context.physics.gravity`

## [2.62.2-pre] - 2023-02-27
### Engine
- Add: support for `camera-controls` attribute on `<needle-engine>` element. When added it will automatically add an `OrbitControls` component on start if the active main camera has no controller (you can assign custom controllers by calling `setCameraController` with the camera that is being controlled)
- Fix: rare error in extension_util
- Fix: timeline preExtrapolation setting
- Fix: disabling Light component should turn off light
- Fix: animating camera fov, near or far plane
- Fix: threejs layer patch for Renderer visibility is now always applied
- Fix: UI runtime instantiate of canvas from templates in scene
- Fix: UI text did not update shadow-component-owner on font loading
- Fix: UI EventSystem raising click event multiple times in some cases
- Fix: UI Text raycast now respects object layer (NoRaycast)
- Fix: UI duplicate pointerUp event
- Fix: UI highlighting getting stuck in wrong state sometimes

## [2.62.1-pre] - 2023-02-23
### Exporter
- Add: FontAdditionalCharacters component to allow to specifiy additional fonts to be included in a font atlas
- Fix: missing animationclip in director caused export exception
- Fix: ComponentGenerator not watching subdirectories in `src/scripts`
- Fix vite plugin not using config codegen directory
- Fix vite plugin assuming <needle-engine> web component in index.html and producing error if not found
- Change: bump UnityGLTF to 1.22.4-pre

### Engine
- Fix: pause wasn't evaluating and thus not pausing audio tracks
- Fix: debug overlay styles were not properly scoped and affected objects inside needle-engine tag
- Fix: Addressables wrong recursive instantiation error
- Fix: UI not showing fully when setting active at runtime
- Change: timeline tracks are now created immediately but their audio clips are deferred until audio is allowed

## [2.62.0-pre] - 2023-02-13
### Exporter
- Add: Meshopt decoder support for engine-loaded glTF files
- Add: better logs when running in headless mode and operations fail due to non-installed packages
- Fix: nullrefs when saving in scenes that don't have ExportInfo components
- Fix: explicit texture compression "None" resulted in wrong compression applied in some cases
- Change: Update UnityGLTF dependency including fixes for specular extension roundtrips, importer improvements

## [2.61.0-pre] - 2023-01-30
### Exporter
- Add: batch export now allows `-scene` arg to point to a prefab or asset and adds `-outputPath` argument to define the path and name of the exported glb(s)
- Fix: rare vite plugin poster error when include directory does not exist
- Fix poster incorrectly being generated when building
- Fix: Dialog that shows up when lightmap encoding settings are wrong now shows up less often
- Fix: serialized npmdefs with wrong paths are not automatically repaired or cleaned up from serialized data
- Change: Bug reporter now assumes .bin next to .gltf is a dependency until .bin is properly registered as a dependency in Unity
- Change: bump gltf-pipeline package fixing a rare bug where toktx could not be found

### Engine
- Add: canvas applyRenderSettings
- Add: progressive support for particle system textures

## [2.60.4-pre] - 2023-01-27
### Exporter
- Change: dont reload page while build preview is in progress (when running ExportInfo/Compress/PreviewBuild)
- Fix: bump build pipeline package to fix issue where texture compression settings were taken from wrong texture
- Fix: vite reload plugin sometimes preventing reload

### Engine
- Fix UI prefab instantiate throwing error at runtime
- Change: show warning when unsupported canvas type is selected
- Change: show warning when trying to use webxr features in non-secure context

## [2.60.3-pre] - 2023-01-26
### Exporter
- Fix register type error when component class with the same name exists multiple times in the same web-project in different files [issue 49](https://github.com/needle-tools/needle-engine-support/issues/49)
- Fix: NeedleAssetSettingsProvider and simplify setting texture settings on import like so:
   ```csharp
   if (NeedleAssetSettingsProvider.TryGetTextureSettings(assetPath, out var settings))
   {
      settings.Override = true;
      settings.CompressionMode = TextureCompressionMode.UASTC;
      NeedleAssetSettingsProvider.TrySetTextureSettings(assetPath, settings);
   }
   ```

### Engine
- Fix: camera fov for blender export allowing fieldOfView property to be undefined, where the fov should be handled by the blender exporter completely.

## [2.60.2-pre.1] - 2023-01-26
### Exporter
- Fix: remove accidental codice namespace using in 2022

## [2.60.2-pre] - 2023-01-26
### Exporter
- Add: Api to access texture compression settings (use `NeedleAssetSettingsProvider`)
- Add pre-build script to run tsc
- Fix: cubemap export fallbacks to LDR format if trying to export cubemap on unsupported build target (e.g. Android)
- Fix: project paths replacement when path has spaces
- Fix: remove global tsc call before building

### Engine
- Fix: particle textures being flipped horizontally

## [2.60.1-pre.1] - 2023-01-25
### Exporter
- Fix: Smart export file size check if file doesnt exist

## [2.60.1-pre] - 2023-01-25
### Exporter
- Change: Make cubemaps use correct convolution mode, downgrade error to warning
- Change: Cubemap warning should not show for skybox
- Change: Smart export check if file exported was < 1 kb in which case we always want to re-export
- Change: vite server plugin now communicates scheduled page reload to client
- Change: bump gltf extensions package dependency
- Fix: vite reload on changed codegen files (it should not reload there)

### Engine
- Change: export Mathf in `@needle.tools/engine`

## [2.60.0-pre] - 2023-01-25
### Exporter
- Add: check allowed cubemap convolution types and log error if that doesn't match
- Change: Remove `backgroundBlurriness` setting on RemoteSkybox (should be controlled on the camera)
- Fix: ExportInfo now doesnt display packages as `local` on OSX anymore
- Fix: GltfReference nullref when exporting via context menu, always ignore smart export for context menu exports
- Fix NEEDLE_gltf_dependencies extension causing gltfs to be invalid
- Fix: Platform compiler errors

### Engine
- Add: Particles support for horizontal and vertical billboards
- Add: Timeline now supports reversed clip (for blender timeline)
- Change: bump gltf pipeline package dependency adding support for global `NEEDLE_TOKTX` environment variable
- Change: timeline clip pos and rot are now optional (for blender timeline)
- Fix: when first loading a gltf pass guidsmap to components (for blender timeline)
- Fix: scrubbing TimelineTrack scrubs audio sources as well now
- Fix: stencils for multimaterial objects

## [2.59.3-pre] - 2023-01-21
### Exporter
- Add: particles basic support for on birth and ondeath subemitter behaviour
- Change: run typescript check before building for distribution
- Fix: saving referenced prefab with auto-export causing export to happen recursively
- Fix: improve vite reloading, generate needle.lock when exporting from referenced scene or prefab to prevent reloading while still exporting
- Fix: vite reloading scripts for usage with vuejs

### Engine
- Add: particles basic support for on birth and ondeath subemitter behaviour

## [2.59.2-pre.1] - 2023-01-20
### Engine
- Fix: issue where click on overlay html element did also trigger events in the underlying engine scene  

## [2.59.2-pre] - 2023-01-20
### Exporter
- Add: save in Prefab Mode does now attempt to re-export currently viewed prefab (similarly to how referenced scenes will re-export if they are referenced in a currently running web project)
- Change: EXR textures are now exported zipped (UnityGLTF)
- Change: OrbitControls now use damping (threejs)
- Change: default mipmap bias is now -0.5 (threejs)
- Change: DeployToFTP inspector now shows info if password is missing in server config asset 
- Change: Bump dependencies 
- Fix: export of human animation without transform when discovered from animatorcontroller should not cause errors
- Fix: handle cubemap export error on creating texture when Unity is on unsupported platform
- Fix: context click export for nested element in hierarchy
- Fix: bump gltf-transform-extensions package fixing a failure when using previously cached texture data but not setting the texture mime-type which caused errors at runtime and the texture to not load
- Fix: timeline now skips exporting clips with missing audio assets in AudioTrack
- Fix: subasset importer throwing nullref when selecting subassets from multiple assets and modifying their import settings
- Fix: Unity build being blocked by BuildPlayerHandler
- Fix: Unity build errors

### Engine
- Add: SpectatorCam.useKeys to allow users to disable spectator cam keyboard input usage (`f` to send follow request to connected users and `esc` to stop following)
- Change: expose SyncedRoom.RoomPrefix

## [2.59.1-pre] - 2023-01-18
### Exporter
- Fix: export error where object was being exported twice in the same event as transform and as gameObject due to self-referencing

## [2.59.0-pre] - 2023-01-18
### Exporter
- Add: Smart Export option, which will not re-export referenced prefabs or scenes if they didnt change since the last export (enable via ProjectSettings/Needle) improving export speeds significantly in certain cases. This option is off by default
- Add: lock file to prevent vite from reloading while export is still in process
- Add: warning when older nodejs version fails because of unknown ``--no-experimental-fetch`` argument
- Change: Some methods of DeployToFTP can now be overriden to customize uploading
- Change: TextureCompressionSettings can now be overriden to customize compression settings
- Change: Minor optimization of exported json, removing some unused data to reduce output size slightly for large or deeply nested projects 
- Fix: Issue where vite reload plugin did sometimes not trigger a reload after files have changed
- Fix: Issue where prefab containing GltfObject did not create a nested gltf to be lazily loaded
- Fix: Issue where nested gltf would cause IOException when it had the same name as an glb in the parent hierarchy
- Fix: TextureSizeHandler not being used when not added to GltfObject. It can now be added to any object in the scene to globally clamp the size of exported textures.
- Fix: Export of default font in 2022 (LegacyRuntime)
- Fix: AnimatorOverrideController is now properly ignored (currently not supported) instead of being serialized in a wrong/unexpected format which did cause errors at runtime
- Fix: Issue where DeployOnly did cause already compressed assets in output directory being replaced by uncompressed assets
- Fix: Texture compression set to ``Auto`` did not be properly export
- Fix: issue where default compression wasnt applied anymore when no specific compression settings where selected / setup anywhere
- Fix: Context menu export with compression from Project window now runs full compression pipeline (applying progressive transformation as well as compression) 
- Remove: Experimental SmartExport option on GltfObject

### Engine
- Add: AssetReference.unload does now dispose materials and mesh geometry
- Add: ``setParamWithoutReload`` now accepts null as paramValue which will remove the query parameter from the url
- Change: timeline does now skip export for muted tracks
- Change: OrbitControls can now use a custom html target when added via script and before enable/awake is being called (e.g. ``const orbit = GameObject.addNewComponent(this.gameObject, OrbitControls, false); orbit.targetElement = myElement``)
- Change: Input start events are now being ignored if a html element is ontop of the canvas
- Fix: use custom element registry to avoid error with `needle-engine element has already been defined`
- Fix: timeline not stopping audio on stop
- Fix: input click event being invoked twice in certain cases
- Fix: ParticleSystem start color from gradient
- Fix: ParticleSystem not properly cleaning up / removing particles in the scene in onDestroy
- Fix: ParticleSystem velocity now respects scale (when mode is set to worldscale)

## [2.58.4-pre] - 2023-01-14
### Exporter
- Update template vite config to improve reloading (you can update the vite config in existing projects via ExportInfo Context Menu > Update vite config) 

### Engine
- Update gltf-extensions package dependency

## [2.58.3-pre] - 2023-01-13
### Exporter
- Change: Update UnityGLTF dependency including fixes for gltf texture imports 
- Fix: run install on referenced npmdefs for distribution builds when packages have changed
- Fix: catch WebRequest invalid operation exception

## [2.58.2-pre.1] - 2023-01-13
### Exporter
- Fix: compiler error on osx and linux

## [2.58.2-pre] - 2023-01-12
### Exporter
- Add: start support for targeting existing web projects
- Add: support for animating color tracks when only alpha channel is exported
- Change: use vite for internal compiling of distributable npm package of needle-engine 
- Change: remove scene asset context menu override
- Change: bump UnityGLTF dependency
- Change: run compression commands when building web project from Unity
- Fix: OSX component compiler commands not being executed when containing spaces
- Fix: Linux using sh for terminal commands instead of zsh 
- Fix: Blendshape normals export
- Fix: error in vite plugin generating poster image
- Fix: Embedded assets for 2022 could not select Needle Engine compression settings
- Fix: Texture MaxSize setting not being passed to UnityGLTF
- Fix: Occasional error when exporting fog caused by component not being in runtime assembly
- Fix: Component compiler should update watcher when project directory changes
- Fix: Export of color alpha animation
- Fix: Light shadow bias settings export for URP when light didnt have UniversalAdditionalLightData component

### Engine
- Change: use draco and ktx loader from gstatic server by default
- Change: reduce circular dependencies
- Fix: Reflectionprobe selecting wrong probe when multiple probes had the exact same position

## [2.58.1-pre] - 2023-01-09
### Exporter
- Fix: light default shadow bias values
- Fix: template vite config
- Fix: timeline exported from prefab was sometimes not exported correctly (due to Playable graphs) - this is now fixed by rebuilding the graph once before export

### Engine
- Add: Prewarm rendering of newly loaded objects to remove lag/jitter when they become visible for the first time
- Change: renderer now warns when sharedMaterials have missing entries. It then tries to remap those values when accessing them by index (e.g. when another component has a material index serialized and relies on that index to be pointing to the correct object)

## [2.58.0-pre] - 2023-01-09
### Exporter
- Add hot reload setting (requires vite.config to be updated which can be done from ExportInfo context menu)
- Add fog export

### Engine
- Add: EventSystem input events (e.g. IPointerClick) are now invoked for all buttons (e.g. right click)
- Add: Hot reload for components

## [2.57.0-pre] - 2023-01-07
### Exporter
- Add: meta info export for vite template
- Add: HtmlMeta component to allow modification of html title and meta title/description from Unity
- Add: Support for poster image generation
- Change: Use custom vite plugin for gzip setting

### Engine
- Remove: Meshline dependency
- Fix: Testrunner Rigidbody import error

## [2.56.2-pre] - 2023-01-06
### Exporter
- Fix: BuildPlatform option for Unity 2021 and newer
- Fix: npm install command for npm 9
- Fix: Light shadowBias settings for Builtin RP
- Change: Include npm logs and version info in bug report logs

### Engine
- Change: Component.addEventListener argument can now derive from Event

## [2.56.1-pre] - 2023-01-05
### Exporter
- Add: initial batch mode / headless export support, can be invoked using `path/to/Unity.exe -batchmode -projectPath "path/to/project" -executeMethod Needle.Engine.ActionsBatch.Execute -buildProduction -scene "Assets/path/to/scene.unity"`, use `-debug` to show Unity console window during process
- Fix: sample window now locks assembly reload while downloading until after installation has finished, show progress report for user feedback 
- Fix: sample window not respecting user cancel

### Engine
- Fix: UI setting Image.sprite property did apply vertical flip every time the sprite was set

## [2.56.0-pre] - 2023-01-04
### Exporter
- Add: mesh compression support
- Add: compression settings for textures and meshes in embedded assets (e.g. an imported fbx or glb now has options to setup compression for production builds)
- Change: Bump UnityGLTF dependency adding caching of exported image data to speed up exports for texture heavy scenes

### Engine
- Add: file-dropped event to DropListener
- Add: UI image and raw image components now support updating texture/sprite at runtime
- Change: Bump needle gltf-transform extensions package adding mesh compression and caching for texture compression leading to significant speedups for subsequent production builds (only changed textures are re-processed)
- Fix: light normal bias defaults

## [2.55.2-pre] - 2023-01-02
### Exporter
- Change: log warning if node is not installed or can not be found before trying to invoke component compiler
- Fix: handle `node` commands similarly to how `npm` commands work

### Engine
- Add: Rigidbody.gravityScale property
- Add: Gizmos.DrawArrow method
- Add: Rigidbody.getAngularVelocity method
- Fix: Mesh collider center of mass

## [2.55.1-pre] - 2022-12-30
### Exporter
- Add: Command Tester window
- Fix: error on OSX when nvm directory does not exist

### Engine
- Add: Warning when serialized component field name is starting with uppercase letter
- Change: bump component compiler dependency
- Fix: Particle rotation over lifetime
- Fix: Particles should not emit when emission module is disabled
- Fix: LODGroup breaking rendering when used with multi-material objects or added on mesh to be culled directly

## [2.55.0-pre] - 2022-12-21
### Exporter
- Add: PhysicsMaterial support
- Fix Spline export
- Fix: Renderer not exporting enabled bool
- Fix: Dev <> Production build flip in DeployToGlitch component

### Engine
- Add: PhysicsMaterial support
- Add: ``Time.timesScale`` factor
- Change: VideoPlayer exposes underlying HTML video element
- Change: EffectComposer check if ``setPixelRatio`` method exists before calling
- Change: WebARSessionRoot and Rig rotation
- Fix: WebXRController raycast line not being visible in Quest AR
- Fix: Renderer that is disabled initially now hides object
- Fix: Some ParticleSystem worldspace settings when calling emit directly

## [2.54.3-pre] - 2022-12-19
### Exporter
- Change: OSX now automatically trys to detect npm install directory when installed using nvm

## [2.54.2-pre] - 2022-12-19
### Exporter
- Change: Improve SamplesWindow adding search field and better styling
- Change: Rename ``UseProgressiveTextures`` to ``ProgressiveTextureSettings``
- Change: Progressive texture loading can now be disabled completely using ProgressiveTextureSettings component
- Change: Only generate progressive loading textures when building for distribution / making a build for deployment
- Change: Remove internal ``ObjectNames.NicifyVariableNames`` which caused unexpected output for variable names starting with `_`
- Change: Remove unused NavMesh components
- Fix: Help menu item order
- Fix: Sample window styling for single column
- Fix: Initial project generation does now run installation once before replacing template variables which previously caused errors because the paths did not yet exist.

### Engine
- Change: debug parameter can now take ``=0`` for disabling them (e.g. ``freecam=0``)
- Fix: InputField opens keyboard on iOS

## [2.54.1-pre] - 2022-12-15
### Engine
- Fix: issue with progressive loading, loading files multiple times if a texture was used in multiple materials/material slots. This was causing problems and sometimes crashes on mobile devices 
- Fix: balloon messages using cached containers didnt update the message sometimes and displaying an old message instead

## [2.54.0-pre.1] - 2022-12-14
### Engine
- Fix: bump gltf extensions package fixing issue with progressive texture loading when multiple textures had the same name 

## [2.54.0-pre] - 2022-12-14
### Exporter
- Add: custom texture compression and progressive loading settings for Needle Engine platform to texture importer
- Add: support for webp texture compression
- Add: tsc menu item to manually compile typescript from Unity 
- Add: support for spritesheet animationclips
- Add: menu item to open bug reports location
- Change: sort component exports by name
- Change: update UnityGLTF version
- Fix: issue with wrong threejs path being written to package.json causing button "Run Needle Project Setup" to appear on ExportInfo

### Engine
- Add: start and end events for progressive loading
- Add: USDZExporter events for button creation and after export
- Change: apply WebARSessionRoot scale to exported object, e.g. if scene is scaled down on Android it should receive the same scale when exporting for Quicklook
- Fix: process reflection probe update in update event to avoid visible flickr after component enabled state has changed

## [2.53.3-pre.1] - 2022-12-12
### Engine
- Fix: implement ButtonColors

## [2.53.3-pre] - 2022-12-12
### Exporter: 
- Fix: InvalidCastException when trying to export AnimatorOverrideController

### Engine
- Add: GroundProjection appyOnAwake to make it possible to just use it when the environment changes via remote skybox and not apply it to the default skybox
- Change: more strict tsconfig
- Change: allow overriding loading element
- Fix: apply shape module rotation to direction
- Fix: ParticleSystem world position not being set when shape module was disabled

## [2.53.2-pre] - 2022-12-09
### Exporter
- Change: order generated types alphabetically
- Fix: engine export codegen should only run in local dev environment

## [2.53.1-pre] - 2022-12-08
### Exporter
- Fix OSX bugs regarding nvm and additional search paths not being used correctly

## [2.53.0-pre] - 2022-12-08
### Exporter
- Add: progressive build step is now separated from Unity Exporter and runs in the background to transform exported gltfs to be progressively loaded. That requires a ``UseProgressiveTextures`` component in the scene. Textures can be excluded from being processed by adding a ``noprogressive`` AssetLabel
- Add: USDZExpoter component which will display ``Open in Quicklook`` option when running on iOS Safari instead of WebXR not supported message.
- Add: Automatically update @types/three in referenced project dependencies to match types declared in core engine
- Change: Only open dist directory after building when not deploying to either FTP or Glitch
- Change: Display toktx message about non-power-of-two textures as warning in Unity
- Change: DeployToFTP inspector now behaves just like DeployToGlitch (using ALT to toggle build type)

### Engine
- Add: InstantiateIdProvider constructor can now take string too for initializing seed
- Add: USDZExpoter component enabling ``Open in Quicklook`` option by default when running on iOS Safari
- Fix: Light intensity
- Fix: Add workaround texture image encoding issue: https://github.com/needle-tools/needle-engine-support/issues/109
- Fix: OrbitControls.enableKeys
- Fix: Remove warning message about missing ``serializable`` when the reference is really missing
- Fix: ``context.domX`` and ``domY`` using wrong values when in AR mode

## [2.52.0-pre] - 2022-12-05
### Exporter
- Add initial support for Spritesheet export (spritesheet animationclip export will be added in one of the next releases)
- Add: RemoteSkybox environmentBlurriness setting
- Add: environmentBlurriness and -Intensity setting to CameraAdditionalData component
- Update templates tsConfig adding skipLibCheck to avoid errors when types/three have errors
- Change: Dont open dist folder when deploying to a server like FTP or Glitch
- Change: Start server now checks vite.config for configured port
- Change: adjust materials to UnityGltf/PBRGraph for better cross-pipeline compatibility

### Engine
- Add iOS platform util methods
- Add ``?debugrig`` to render XRRig gizmo
- Add support for Spritesheet Animation
- Add: EventTrigger implementations for onPointerClick, onPointerEnter, onPointerExit, onPointerDown, onPointerUp
- Add: RemoteSkybox environmentBlurriness setting
- Fix: Renderer reflection probe event order issue not applying reflection probes when enabling/disabling object because reflection probes have not been enabled
- Fix: remove log in ParticleSystemModules

## [2.51.0-pre] - 2022-11-30
### Exporter
- Add: basic texture compression control using ``ETC1S`` and ``UASTC`` Asset Labels, they can be added to either textures or exported Asset (for example gltf asset) to enforce chosen method in toktx (production builds) 
- Change: Improve BugReporter
- Fix: DefaultAvatar XRFlags
- Fix: Progressive texture export (high-res glb) not using selected texture compression method

### Engine
- Change: remove nebula, dat.gui and symlink package dependencies
- Change: Light does not change renderer shadowtype anymore
- Change: update threejs to 146
- Change: update threejs types
- Change: Screencapture should not start on click when not connected to networked room
- Change: WebXR returns ar supported when using Mozilla WebXR
- Fix DragControls drag interaction not disabling OrbitControls at right time
- Fix physics collider position in certain cases
- Fix Rigidbody not syncing physics position when parent transform changes
- Fix Timeline awake / active and enable
- Fix: OrbitControls calulcating target position with middle mouse click in worldspace instead of localspace causing wrong movement when parent is transformed
- Fix: Raycast in Mozilla WebXR / using window sizes instead of dom element sizes
- Fix input with scrolled window
- Fix: destroy local avatar on end of webxr session (https://github.com/needle-tools/needle-engine-support/issues/117)
- Fix: WebXRAvatar setting correct XRFlags

## [2.50.0-pre] - 2022-11-28
### Exporter
- Add: Skybox export checks to ensure texture is power of two and not bigger than 4k when exported using hdr
- Add: RemoteSkybox component to allow referencing local image texture
- Add: Set UASTC compression to sprite textures to improve production build quality for UI graphics

### Engine
- Add warning to Light when soft shadows change renderer shadow type
- Add: RemoteSkybox can now load jpg and png textures as skybox
- Change: Instantiate does now copy Vector, Quaternion and Euler objects to ensure multiple components dont share the same objects
- Fix: AnimatorController causes threejs error when creating empty animationclip (Blender) 
- Fix: AnimatorController error when transition has no conditions array (Blender)

## [2.49.1-pre] - 2022-11-25
### Engine
- Add circular instantiation check to AssetReference
- Allow filtering ``context.input.foreachPointerId()`` by pointer types (e.g. mouse or touch)
- Fix typescript error in particle system module function (happened only when ``strict`` was set to false in tsconfig)
- Fix XRFlag component not being applied on startup

## [2.49.0-pre] - 2022-11-24
### Exporter
- Change: Exporter now shows dialogue when trying to export lightmaps with wrong Lightmap encoding

### Engine
- Add: input iterator methods to loop over currently active input pointer ids
- Change: input refactor to work better with touch
- Fix GraphicRaycaster serialization warning
- Fix deserialization bug when Animation clips array is not serialized (exported from blender)
- Fix: remove leftover log in AnimatorController when cloning
- Fix XR flag not correctly restoring state
- Fix reticle not being rendered when XRRig is inside WebARSessionRoot
- Fix Mozilla XR AR overlay (https://github.com/needle-tools/needle-engine-support/issues/81)
- Fix Mozilla XR removing renderer canvas on exit AR (https://github.com/needle-tools/needle-engine-support/issues/115)

## [2.48.0-pre] - 2022-11-23
### Exporter
- Add menu item to copy project info to clipboard (``Needle Engine/Report Bug/Copy Project Info``)
- Change: Reduce max size of default cubemap to 256 (instead of 2048)
- Change: ExportInfo can open folder without explicit workspace in workspace
- Change: remove keep names options in react vite template
- Change: move default project path from ``Projects/`` to ``Needle/``
- Change: remove .quit-ar styles from templates
- Fix: Export skybox in referenced prefabs using minimal size (64px) unless otherwise defined

### Engine
- Add: debug console for better mobile debugging (shows up on error on mobile in local dev environment or when using the ``?console`` query parameter)
- Add: dom element visibility checks and suspend rendering and update loops (if ``this.context.runInBackground`` is false)
- Add: ``this.context.isPaused`` to manually suspend rendering
- Add: ``IComponent.onPausedChanged`` event method which is called when rendering is paused or resumed
- Change: update copy-from-to dev dependency version to fix build error when path contains ``(``
- Change: ``this.context.input`` does now support pointer lock state (properly reports delta)
- Fix: make sure VRButton has the same logic as in three again (regex instead of try-catch)
- Fix: WebXRViewer DOM Overlay bugs when dom overlay element is inside canvas
- Fix: exitAR not being called in some cases when exiting AR
- Fix: ``this.context.domX`` and ``this.context.domY`` when web component is not fullscreen

## [2.47.2-pre] - 2022-11-17
### Exporter
- Add info to log about where to change colorspace from gamma to linear
 
### Engine
- Add: Initial react three fiber components
- Change: OrbitControls made lerp stop distance smaller 
- Change: expose ``*enumerateActions()`` in AnimatorController
- Fix: Flipped custom reflection texture
- Fix: Volume exposure not being applied when no Tonemapping effect was set
- Fix: Volume tonemapping not respecting override state setting
- Fix: ``AudioSource.loop`` not working
- Fix: Collider center being not not applied correctly
- Fix: MeshCollider scale not being applied from object

## [2.47.1-pre] - 2022-11-16
### Exporter
- Bump Engine version and export particle trail material

### Engine
- Add: Particles subemitter support
- Add: Particles inherit velocity support
- Add: Particles size by speed support
- Add: Particles color by speed support
- Add: Particles trail now fadeout properly when "die with particle" is disabled
- Add: Particles circle shape
- Change: button hover now sets cursor to pointer
- Fix: WebXR controller disabling raycast line for hands
- Fix: WebXR hands path when not assigned in Unity
- Fix: Mesh Particles not rendering because of rotation being wrongly applied
- Fix: Mesh particles size in AR
- Fix: Particles color and size lerp between two curves

## [2.47.0-pre] - 2022-11-14
### Exporter
- Change: AxesHelper component now shows axes like in threejs
- Change: bump UnityGLTF version

### Engine
- Add: RemoteSkybox option to control if its set as background and/or environment
- Add: @serializable decorator, @serializeable will be removed in a future version
- Add: getComponent etc methods to IGameObject interface
- Add: Renderer.enable does now set visible state only without affecting the hierarchy or component active state
- Change: Expose Typestore
- Change: Animation componet does loop by default (use the AdditionalAnimationData component to set the default loop setting)
- Fix: WebXR relative hands path in subfolders
- Fix: Rigidbody did not properly detect object position change if the position change was applied a second time at the exact same target position (it worked setting it once and didnt work in subsequent calls - now it does always detect it)

## [2.46.0-pre] - 2022-11-11
### Exporter
- Change: ``Setup scene`` when creating a new camera it sets near clip plane to smaller value than default
- Change: ExportInfo pick directory button now opens last selected directory if it still exists and is in the same Unity project

### Engine
- Add: Particles limit velocity over time
- Add: Particles rotation by speed
- Add: ParticleSystem play, pause, stop and emit(count) methods
- Add: ``WebXR.showRaycastLine`` exposed so it can be disabled from code
- Fix: issues in applying some forces/values for different scaling and worldspace <> localspace scenarios
- Change: raise input events in core method to also allow receiving WebAR mock touch events
- Change: ``Animation.play()`` does not require argument anymore

## [2.45.0-pre] - 2022-11-10
### Exporter
- Add: gzip option to build menu
- Change default build to not gzipped (can be enabled in Unity's Build Window)
- Change: open output directory after building distribution
- Change: bump UnityGLTF dependency
- Fix: glitch project name must not contain spaces

### Engine
- Add: particles emission over distance
- Add: particles can enable trail (settings are not yet applied tho) 
- Add: camera now useses culling mask settings
- Add: particle VelocityOverLife
- Add: particle basic texture sheet animation support
- Change: ensure ``time.deltaTime`` is always > 0 and nevery exactly 0
- Fix: progressbar handle progress event not reporting total file size
- Fix: layer on camera did affect visibility
- Fix: cloning animatorcontrollers in builds did fail because of legacy AnimatorAction name check
- Fix: ``RGBAColor.lerpColors`` did produce wrong alpha value
- Fix: custom shader ``_ZTest`` value is now applied as threejs depthTest function

## [2.44.2-pre] - 2022-11-09
### Exporter
- add: export of particle mesh
- change: bump UnityGLTF dependency
- change cubemap export: make sure the path for flipping Y and not flipping Y applies the same Y rotation

### Engine
- add ``Graphics.copyTexture``
- add ``Renderer.allowProgressiveLoad``
- add ``Gizmos.DrawBox`` and ``DrawBox3``
- add particles burst emission
- add particles color interpolation between two gradients
- fix: reflection probe material caching for when material is being changed at certain times outside of animation loop and cache applied wrong material
- fix: AnimationCurve evaluation when time and keyframe are both exactly 0
- change: reflection probe now requires anchor override
- change: bump threejs dependency 

## [2.44.1-pre] - 2022-11-07
### Exporter
- Fix: serialization error for destroyed component

### Engine
- Add: start adding particle systems support again
- Change: update dependency version to needle gltf-transform-extensions package
- Change: light set to soft shadows now changes renderer shadow mode to ``VSMShadowMap`` (can be disabled by setting ``Light.allowChangingShadowMapType`` to false)
- Fix: WebXR creating AR button when called from script in awake 
- Fix: ``AnimationCurve.evaluate``

## [2.44.0-pre] - 2022-11-05
### Exporter
- Add: ``Create/Typescript`` can now create script files in ``src/scripts`` if the selected file in the ProjectBrowser is not part of an npmdef - it will create a template typscript file with your entered name and open the workspace
- Change: Update component compiler version fixing codegen for e.g. ``new Vector2(1, .5)`` which previously generated wrong C# code trying to assign doubles instead of floats

### Engine
- Add support for deleting all room state by calling ``context.connection.sendDeleteRemoteStateAll()`` (requires backend to update ``@needle-tools/needle-tiny-networking-ws`` to ``^1.1.0-pre``)
- Add Hinge joint
- Add ``Gizmos.DrawLine``, ``DrawRay`` ``DrawWireSphere`` and ``DrawSphere``
- Add: physics Collision Contacts now contain information about ``impulse`` and ``friction``
- Add ``physics.raycastPhysicsFast`` as a first method to raycast against physics colliders, the returning object contains the point in worldspace and the collider. This is the most simplest and thus fastest way to raycast using Rapier. More complex options will follow in future versions.
- Fix joint matrix calculation
- Fix and improve physics Contacts point calculations  
- Fix issue in physics event callbacks where ``onCollisionStay`` and ``onCollisionExit`` would only be called when ``onCollisionEnter`` was defined

## [2.43.0-pre] - 2022-11-04
### Exporter
- Change: Set template body background to black

### Engine
- Add: physics FixedJoint
- Change: CharacterController now rotates with camera
- Change: scaled mesh colliders are now cached
- Change: disable OrbitControls when in XR
- Change: first enabled camera component sets itself as rendering camera if no camera is yet assigned (mainCamera still overrides that)
- Change: package module field now shows to ``src/needle-engine``
- Change: ``Camera.backgroundColor`` assigning Color without alpha sets alpha to 1 now
- Fix: improved missing ``serializable`` detection / warning: now only shows warning for members actually declared in script 
- Fix: wrong light intensity in VR when light is child of WebARSessionRoot [issue 103](https://github.com/needle-tools/needle-engine-support/issues/103) 

## [2.42.0-pre] - 2022-11-02
### Exporter
- Add: explicit shadow bias settings to ``LightShadowData`` component (can be added via Light component button at the bottom of the component)
- Fix ComponentCompiler / CodeWatcher not starting to watch directory when project is not installed yet
- Fix ``CubemapExporter.ConvertCubemapToEquirectTexture`` now using same codepath as skybox export
- Fix ``ExportInfo.Play`` button does not use same code path as Editor Play button

### Engine
- Add ``context.isInAR`` and ``context.isInVR`` properties
- Add physics capsule collider support
- Add basic character controller implementation (experimental)
- Add ``context.input.getMouseWheelDeltaY()``
- Add: SmoothFollow option to restrict following on certain axes only for position
- Add: ``Rigidbody.teleport`` method to properly reset internal state
- Add: load glbs using build hash (appended as ``?v=123``)
- Change: Collision event args now exposes contacts array
- Fix Exit AR (X) button not showing up
- Fix physics collider center offset
- Fix removing colliders and rigidbodies throwing error (when trying to access properties for already removed bodies)
- Fix bug in AnimatorController causing broken animations when the same clip is used in multiple states (caused by ``mixer.uncacheCip``)
- Fix rigidbody friction allowing for physical bodies being transported on e.g. platforms
- Fix ``onTriggerStay`` being invoked with the correct collider argument
- Fix AnimatorController exit time not being used properly
- Fix AnimatorController not checking all possible transitions if one transition did match conditions but could not be made due to exit time setting
- Fix ``Renderer.sharedMaterials`` not handling SkinnedMeshRenderer
- Fix environment blend mode for mozilla XR browser on iOS
- Fix: Camera now removing self from being set as currently rendering in ``onDisable``


## [2.41.0-pre] - 2022-10-28
### Exporter
- Change: enable Auto Reference in Needle Engine asmdef

### Engine
- Add: rapier physics backend and overall improved physics system like constraint support, fixed physics collider updates and synchronization between rendering and physics world or animation of physical bodies 
- Remove: cannon-es
- Add basic mesh collider support
- Add ``@validate`` decorator and ``onValidate`` event method that can be used to automatically get callbacks when marked properties are being written to (for example internally this is used on the Rigidbody to update the physics body when values on the Rigidbody component are being updated)
- Change: assign nested gltf layers
- Change: reworked Rigidbody api
- Fix: allow Draco and KRTX compression on custom hand models
- Fix: applying Unity layers to threejs objects
- Fix: BoxHelper stopped working with SpatialTrigger
- Fix: AR reticle showing up in wrong position with transformed WebARSessionRoot

## [2.40.0-pre] - 2022-10-26
### Exporter
- Add: Warnings when nesting GltfObjects with gltf models that are only copied to the output directory (effectively not re-exported) with prefab overrides
- Add: Animation component can now be configured with random time scale and offset using the additional data component (see "Add AnimationData" button on Animation component)
- Add: nested .gltf assets now copy their dependencies to the output directory
- Change: Refactor deploy to FTP using ScriptableObjects for server settings
- Change: Better compression is only used when explicitly configured by adding a ``TextureCompressionSettings`` component to the GltfObject because it also increases filesize significantly and is not always needed
- Fix: Remove old texture callback that caused textures to be added to a glb twice in some cases

### Engine
- Add: Expose WebXR hand model path
- Add: Animation component can now be configured with random time scale and offset
- Change: allow blocking overlay errors using the ``?noerrors`` query parameter
- Change: don't use Composer for postprocessing in XR (see [issue](https://github.com/needle-tools/needle-engine-support/issues/101)) 
- Change: physics intersections causing NaN's are now reported prominently and physics bodies are removed from physics world as an interim solution, this provides more information about problematic colliders for debugging
- Fix: bug that caused component events for onEnable and onDisable not being called anymore in some cases
- Fix: cases where loading overlay using old project template wouldnt be removed/hidden anymore
- Fix: WebXR hide large hand grab sphere
- Fix: onPointerUp event not firing using WebXR controllers when grabbing an object for the second time
- Fix: GroundProjection can now be removed again
- Fix: Custom shaders exported using builtin RP can now use  _Time property
- Fix: Only create two controllers when in AR on oculus browser
- Fix: BoxHelperComponent can now handle multi-material objects (groups) 

## [2.39.3-pre] - 2022-10-24
### Exporter
- Change: Remove GltfObject component from default Avatar prefab
- Fix: DeployToFTP connection error

### Engine
- Add: warning balloon when unknown components are detected and have been most likely forgot to be installed, linking to npmdef docs 
- Fix: dont show serialization warning for builtin components where specific fields are not deserialized on purpose (since right now the serializer does not check which fields are actually implemented) 

## [2.39.2-pre] - 2022-10-24
### Exporter
- Change: Disable timer logs

### Engine
- Change: AudioSource exposes ``clip`` field
- Change: improve error and messaging overlay
- Change: detect when serialized Object3D and AssetReference are missing ``@serializable`` attribute and show message in overlay
- Change: add WebXR hands path to controllers
- Fix: WebXR controllers now use interactable object when grabbing (instead of hit object previously) which fixes interaction with nested hierarchies in XR and DragControls

## [2.39.1-pre] - 2022-10-23
### Exporter
- Fix: improve generating temporary project with npmdef dependencies
- Fix: avoid attempting to start server twice when project is being generated

## [2.39.0-pre] - 2022-10-23
### Exporter
- Add DeployToFTP component
- Fix automatically installing dependencies to temporary project when the project was already generated from another scene

### Engine
- Change: Renderer ``material`` is now ``sharedMaterial`` to make it more clear for Unity devs that the material is not being cloned when accessed
- Fix: When not specifying any explicit networking backend for glitch deployment it now falls back to the current glitch instance for networking

## [2.38.1-pre] - 2022-10-21
### Exporter
- Add: creating npmdef now automatically creates ``index.ts`` entry point (and adds it to ``main`` in package.json)
- Change: bump UnityGLTF dependency

### Engine
- Add: Screenshare component ``share`` method now takes optional options to configure device and MediaStreamConstraints for starting the stream 
- Fix: WebXR should show EnterVR button when enabled in Unity
- Fix: component ``enable`` boolean wasnt correctly initialized when loaded from gltf
- Fix: Object3D prototype extensions weren't correctly applied anymore
- Fix: Interaction bug when using DragControls with OrbitControls with multitouch

## [2.38.0-pre] - 2022-10-20
### Exporter
- Add: toktx compression extension is now automatically used, can be disabled by adding the ``TextureCompressionSettings`` component to the GltfObject and disabling it
- Change: adjust menu items

### Engine
- Add ``Renderer.mesh`` getter property
- Change: ``Renderer.material`` now returns first entry in ``sharedMaterials`` array so it automatically works in cases where a Renderer is actually a multi-material object
- Change: warn when trying to access components using string name instead of type
- Change: update needle gltf-transform-extensions to 0.6.2
- Fix: remove log from UIRaycastUtil
- Fix: move TypeStore import in builtin engine again to not break cases where ``import @needle-engine`` was never used
- Fix: React3Fiber template and AR overlay container access when using react

## [2.37.1-pre] - 2022-10-19
### Exporter
- Change: allow overriding minimum skybox resolution for root scene (minimum is 64)

### Engine
- Change: unify component access methods, first argument is now always the object with the component type as second argument
- Fix physics collision events throwing caused by refactoring in last version
- Fix loading screen css

## [2.37.0-pre] - 2022-10-19
### Exporter
- Add ``ImageReference`` type: textures exported as ``ImageReference`` will be copied to output assets directory and serialized as filepaths instead of being included in glTF
- Change: Reduce default size of progressive textures (in ``UseProgressiveTextures`` component)
- Change: Update UnityGLTF dependency fixing normal export bug and serializing text in extensions now using UTF8

### Engine
- Change: First pass of reducing circular dependencies
- Change: Update @needle-tools/gltf-transform-extensions version
- Change: Update component compiler to 1.9.0. Changed include:
   * Private and protected methods will now not be emitted anymore
   * ``onEnable/onDisable`` will be emitted as ``OnEnable`` and ``OnDisable`` [issue 93](https://github.com/needle-tools/needle-engine-support/issues/93)
- Change: handle Vector3 prototype extensions
- Fix: issue with UI causing rendering to break when enabling text components during runtime that have not yet been active before
- Fix: OrbitControls LookAtConstraint reference deserialization
- Fix: WebXRController raycasting against UI marked as ``noRaycastTarget`` or in CanvasGroup with disabled ``interactable`` or ``blocksRaycast``

## [2.36.0-pre] - 2022-10-17
### Exporter
- Change: Move Screensharing aspect mode settings into VideoPlayer component (in ``VideoPlayerData``)

### Engine
- Add: start adding support for 2D video overlay mode
- Change: Install threejs from @needle-tools/npm - this removes the requirement to have git installed and should fix a case where pulling the package from github would fail 
- Change: Move Screensharing aspect mode settings into VideoPlayer component
- Change: Move ``InstancingUtils`` into ``engine/engine_instancing.ts``
- Change: BoxCollider now checks if ``attachedRigidBody`` is assigned at start
- Change: Collision now exposes internal cannon data via ``__internalCollision`` property
- Fix: EventSystem now properly unsubscribes WebXRController events

## [2.35.5-pre] - 2022-10-17
### Exporter
- Change: rename ``codegen/exports.ts`` to ``codegen/components.ts``
- Change: ScreenCapture component has explicit VideoPlayer component reference to make it clear how it should be used
 
### Engine
- Add: ScreenCapture has mode for capturing webgl canvas (unfortunately it doesnt seem to work well in Chrome or Firefox yet)
- Change: move threejs prototype extensions into own file and make available to vanilla js builds
- Change: ScreenCapture component has explicit VideoPlayer component reference
- Fix: animating properties on custom shaders

## [2.35.4-pre] - 2022-10-15
### Exporter
- Change: dont automatically run install on referenced npmdefs when performing export
- Fix issue where browser scrollbar would flicker in certain cases when OS resolution was scaled 

### Engine
- Add: start implementing trigger callbacks for ``onTriggerEnter``, ``onTriggerExit`` and ``onTriggerStay``
- Change: ``GameObject.setActive`` now updates ``isActiveAndEnabled`` state and executes ``awake`` and ``onEnable`` calls when the object was activated for the first time (e.g. when instantiating from an previously inactive prefab)
- Change: improve collision callback events for components (``onCollisionEnter``, ``onCollisionExit`` and ``onCollisionStay``)
- Change: this.context.input keycode enums are now strings
- Fix: local dev error overlay now also displays errors that happen before web component is completely loaded (e.g. when script has import error)
- Fix: Rigidbody force is now correctly applied when the component was just instantiated (from inactive prefab) and added to the physics world for the first time
- Fix: DragControls component keyboard events ("space" and "d" for modifying height and rotation)

## [2.35.3-pre] - 2022-10-14
### Exporter
- Change: delete another vite cache
- Change: improve Codewatcher for scripts in ``src/scripts``

## [2.35.2-pre] - 2022-10-14
### Exporter
- Change: delete vite caches before starting server

## [2.35.1-pre] - 2022-10-14
### Exporter
- Change: only serialize used Camera fields
- Change: prevent serializing TextGenerator
- Change: prevent exporting Skybox if no skybox material exists
- Change: prevent installing referenced npmdefs while server is running hopefully fixing some issues wiht vite/chrome where type declarations become unknown
- Fix: loading relative font paths when exported via Asset context menu

### Engine
- Change: Rigidbody now tracks position changes to detect when to update/override simulated physics body
- Fix: loading relative font paths when exported via Asset context menu

## [2.35.0-pre] - 2022-10-13
### Exporter
- Change: make default SyncCam prefab slightly bigger
- Change: log error when ExportInfo GameObject is disabled in the hierarchy

### Engine
- Add: inital ScreenCapture component for sharing screens and camera streams across all connected users
- Add: ``onCollisionEnter``, ``onCollisionStay`` and ``onCollisionExit`` event methods to components

## [2.34.0-pre] - 2022-10-12
### Exporter
- Add temporary support for legacy json pointer format
- Add warning to Build Window when production build is selected but installed toktx version does not match recommended version
- Add warning if web project template does not contain package.json
- Add react template
- Add: allow exporting glbs from selected assets via context menu (previously this only worked in scene hierarchy, it now works also in project window)
- Changed: SpectatorCam improvements, copying main camera settings (background, skybox, near/far plane)
- Changed: improved ExportInfo when selecing web project template
- changed: dont export hidden Cinemachine Volume component
- Changed: update UnityGLTF dependency
- Changed: use source identifier everywhere to resolve absolute uri from relative uris as a first step of loading glbs including dependencies from previously unknown directories
- Fix: when exporting selected glbs with compression all dependent glbs (with nested references) will automatically also be compressed after export
- Fix: Cubemap rotation

### Engine
- Add: Quest 2 passthrough support
- Add: UI Graphic components now support ``raycastTarget`` again
- Add: VideoPlayer now supports ``materialTarget`` option which allows for assigning any renderer in the scene that should be used as a video canvas
- Changed: updated three-mesh-ui dependency version
- Changed: updated needle-gltfTransform extensions package, fixing an issue with passthrough of texture json pointers
- Changed: selecting SpectatorCam now requires click (instead of just listening to pointer up event)
- Fix: Avatars using instanced materials should now update transforms correctly again

## [2.33.0-pre] - 2022-10-10
### Exporter
- Fix: error log caused by unused scene template subasset
- Change: allow exporting ParticleSystem settings
- Change: re-word some unclear warnings, adjust welcome window copy
- Change: dont automatically open output folder after building

### Engine
- Add: Context.removeCamera method
- Add: SpectatorCam allows to follow other users across devices by clicking on respective avatar (e.g. clicking SyncedCam avatar or WebXR avatar, ESC or long press to stop spectating)
- Add: ``Input`` events for pointerdown, pointerup, pointermove and keydown, keyup, keypress. Subscribe via ``this.context.input.addEventListener(InputEvents.pointerdown, evt => {...})`` 
- Change: Default WebXR rig matches Unity forward
- Fix: WebXRController raycast line being rendered as huge line before first world hit
- Fix: SpectatorCam works again
- Fix: ``serializable()`` does now not write undefined values if serialize data is undefined
- Fix: exit VR lighting

## [2.32.0-pre] - 2022-10-07
### Exporter
- Add: toktx warning if toktx version < 4.1 is installed.
- Add: button to download recommended toktx installer to Settings 
- Change: Bump UnityGLTF version
- Change: Builder will install automatically if Needle Engine directory is not found

### Engine
- Add: ``resolutionScaleFactor`` to context
- Fix ``IsLocalNetwork`` regex
- Fix custom shaders failing to render caused by json pointer change
- Change: rename Context ``AROverlayElement`` to ``arOverlayElement``

## [2.31.0-pre] - 2022-10-06
### Exporter
- Add first version of TextureCompressionSettings component which will modify toktx compression settings per texture
- Fix skybox export being broken sometimes
- Fix Vite template update version of vite compression plugin to fix import error
- Change: json pointers now have correct format (e.g. ``/textures/0`` instead ``textures/0``)
- Change: Bump needle glTF transform extensions version

### Engine
- Fix: EventList failing to find target when targeting a Object3D without any components
- Fix: text now showing up when disabling and enabling again after the underlying three-mesh-ui components have been created
- Fix: Builtin sprites not rendering correctly in production builds
- Change: Bump needle glTF transform extensions version
- Change: json pointers now have correct format (e.g. ``/textures/0`` instead ``textures/0``)
- Change: Bump UnityGLTF version

## [2.30.1-pre] - 2022-10-05
### Exporter
- Fix animating ``activeSelf`` on GameObject in canvas hierarchy
- Fix ExportInfo directory picker
- Removed unused dependencies in Vite project template
- Removed wrapper div in Vite project template

### Engine
- Fix animating ``activeSelf`` on GameObject in canvas hierarchy
- Fix SpectatorCam component
- Fix WebXRController raycast line being rendered as huge line before first world hit

## [2.30.0-pre] - 2022-10-05
### Exporter
- Add: experimental AlignmentConstraint and OffsetConstraint
- Fix: font-gen script did use require instead of import
- Change: delete vite cache on server start

### Engine
- Add: experimental AlignmentConstraint and OffsetConstraint
- Remove: MeshCollider script since it is not supported yet
- Change: Camera does now use XRSession environment blend mode to determine if background should be transparent or not.
- Change: WebXR exposes ``IsInVR`` and ``IsInAR``
- Fix: RGBAColor copy alpha fix
- Fix: Avatar mouth shapes in networked environment

## [2.29.1-pre] - 2022-10-04
### Exporter
- Add folder path picker to ExportInfo
- Change message on first installation and when a project does not exist yet
- Change prevent projects being generated in Assets and Packages folders

### Engine
- Change: DropListener file drop event does send whole gltf instead of just the scene

## [2.29.0-pre] - 2022-10-04
### Exporter
- Add: Local error overlay shows in AR
- Add: itchio inspector build type can now be toggled by holding ALT
- Fix: URP 12.1 api change
- Change: Vite template is updated to Vite 3
- Change: Bump UnityGLTF dependency
- Change: Move glTF-transform extension handling into own package, using glTF transform 2 now

### Engine
- Add: allow overriding draco and ktx2 decoders on <needle-engine> web component by setting ``dracoDecoderPath``, ``dracoDecoderType``, ``ktx2DecoderPath`` 
- Add: ``loadstart`` and ``progress`` events to <needle-engine> web component
- Fix rare timeline animation bug where position and rotation of objects would be falsely applied
- Change: update to three v145
- Change: export ``THREE`` to global scope for bundled version

## [2.28.0-pre] - 2022-10-01
### Exporter
- Remove: legacy warning on SyncedCamera script
- Fix: exception during font export or when generating font atlas was aborted
- Change: Export referenced gltf files using relative paths
- Change: Bump runtime engine dependency

### Engine
- Add: make engine code easily accessible from vanilla javascript
- Fix: handle number animation setting component enable where values are interpolated
- Change: Remove internal shadow bias multiplication
- Change: Addressable references are now resolved using relative paths
- Change: Update package json

## [2.27.2-pre] - 2022-09-29
### Exporter
- Bump runtime engine dependency 

### Engine
- Add: Light component shadow settings can not be set/updated at runtime
- Fix: enter XR using GroundProjectedEnv component
- Fix: Light shadows missing when LightShadowData component was not added in Unity (was using wrong shadowResolution)
- Change: dont allow raycasting by default on GroundProjectedEnv sphere

## [2.27.1-pre.1] - 2022-09-29
### Exporter
- Fix compiler flag bug on OSX [issue 76](https://github.com/needle-tools/needle-engine-support/issues/76)

## [2.27.1-pre] - 2022-09-29
### Exporter
- Add: Detect outdated threejs version and automatically run ``npm update three``
- Add: shadow resolution to LightShadowData component
- Add: Warning to GroundProjectedEnvironment inspector when camera far plane is smaller than environment radius

### Engine
- Add: Light exposes shadow resolution

## [2.27.0-pre] - 2022-09-28
### Exporter
- Add RemoteSkybox component to use HDRi images from e.g. polyhaven
- Add GroundProjectedEnv component to use threejs skybox projection 

### Engine
- Add RemoteSkybox component to use HDRi images from e.g. polyhaven
- Add GroundProjectedEnv component to use threejs skybox projection 
- Fix: export ``GameObject`` in ``@needle-tools/engine``

## [2.26.1-pre] - 2022-09-28
### Exporter
- Add LightShadowData component to better control and visualize directional light settings

### Engine
- Add: ``noerrors`` url parameter to hide overlay
- Fix: WebXR avatar rendering may be visually offset due to root transform. Will now reset root transform to identity

## [2.26.0-pre] - 2022-09-28
### Exporter
- Add: tricolor environment light export
- Add: generate exports for all engine components
- Add: export for InputActions (NewInputSystem)

### Engine
- Add: ``@needle-tools/engine`` now exports all components
- Add: environment light from tricolor (used for envlight when set to custom but without custom cubemap assigned)
- Add: show console error on screen for localhost / local dev environment
- Fix: create environment lighting textures from exported colors
- Change: UI InputField expose text
- Change: Bump threejs version to latest (> r144) which also contains USDZExporter PR

## [2.25.2-pre] - 2022-09-26
### Exporter
- Fix collab sandbox scene template, cleanup dependencies
- Fix ShadowCatcher export in Built-in RP
- Fix WebHelper nullreference exception
- Change: remove funding logs, improve log output
- Change: exporting with wrong colorspace is now an error
- Change: Bump UnityGLTF dependency
- Change: add log to Open VSCode workspace

### Engine
- Add: custom shader set ``_ScreenParams``
- Change: DropListener event ``details`` now contains whole gltf file (instead of just scene object)

## [2.25.1-pre] - 2022-09-23
### Exporter
- Bump Engine dependency

### Engine
- Add: AudioSource volume and spatial blending settings can now be set at runtime
- Fix: AudioSource not playing on ``play`` when ``playOnAwake`` is false

## [2.25.0-pre] - 2022-09-23
### Exporter
- Add: automatically include local packages in vscode workspace
- Add: experimental progressive loading of textures
- Fix: Catch ``MissingReferenceException`` in serialization
- Fix: Environment reflection size clamped to 256 for root glb and 64 pixel for referenced glb / asset
- Fix: ShadowCatcher inspector info and handle case without renderer
- Change: ComponentGen types are regenerated when player scriptcount changes

### Engine
- Add: VideoPlayer crossorigin attribute support
- Add: ``debuginstancing`` url parameter flag
- Add: Image handle builtin ``Background`` sprite
- Add: Component now implements EventTargt so you can use ``addEventListener`` etc on every component
- Add: EventList does automatically dispatch event with same name on component. E.g. UnityEvent named ``onClick`` will be dispatched on component as ``on-click``
- Add: experimental progressive loading of textures
- Add: ``WebXR`` exposes ``IsARSupported`` and ``IsVRSupported``
- Fix: remove Ambient Intensity
- Fix: ShadowCatcher material should not write depth

## [2.24.1-pre] - 2022-09-22
### Exporter
- Remove: all scriban templating
- Change: TypeUtils clear cache ond recompile and scene change
- Change: move SyncedCamera into glb in Sandbox template
- Change: Show warning in GltfObject inspector when its disabled in main scene but not marked as editor only since it would still be exported and loaded on startup but most likely not used
- Change: scene template assets use UnityGLTF importer by default
- Change: TypeInfoGenerator for component gen does now prioritize types in ``Needle`` namespace (all codegen types), ``Unity`` types and then everything else (it will also only include types in ``Player`` assemblies)

### Engine
- Fix: SpatialTrigger intersection check when it's not a mesh
- Fix: UnityEvent / EventList argument of ``0`` not being passed to the receiving method
- Fix: Physics rigidbody/collider instantiate calls
- Fix: Physics rigidbody transform changes will now be applied to internal physics body 
- Fix: ``needle-engine.getContext`` now listens to loading finished event namely ``loadfinished``
- Change: cleanup some old code in Rigidbody component

## [2.24.0-pre] - 2022-09-21
### Exporter
- Add: new ``DeployToItch`` component that builds the current project and zips it for uploading to itch.io
- Add: FontGeneration does not try to handle selected font style
- Add: Show ``SmartExport`` dirty state in scene hierarchy (it postfixes the name with a *, similar to how scene dirty state is visualized)
- Add: ``Collect Logs`` now also includes all currently known typescript types in cache
- Remove: legacy ``ScriptEmitter`` and ``TransformEmitter``. Code outside of glb files will not be generated anymore
- Change: Renamed ``Deployment`` to ``DeployToGlitch``
- Change: Set typescript cache to dirty on full export
- Change: automatically run ``npm install`` when opening npmdef workspace
- Change: Bump UnityGLTF dependency to ``1.16.0-pre`` (https://github.com/prefrontalcortex/UnityGLTF/commit/aa19dd2a4f2f3f533888deb47920af6a6b4bf80b)
- Fix: ``Setup Scene`` context menu now sets directional light shadow when creating a light
- Fix: "Project Install Fix" did sometimes fail if an orphan but empty folder was still present in node_modules and ``npm install`` didn't install the missing package again 
- Fix: Exception where FullExport would fail if no ``caches~`` directory exists
- Fix: CodeWatcher threading exception when many typescript files changed (or are added) at once
- Fix: FontGenerator issue where builtin fonts would be unnecessarily re-generated
- Fix: Regression in custom reflection texture export

### Engine
- Add: initial support for ``InputField`` UI (rendering, input handling on desktop, mobile and AR, change and endedit events)
- Add: ``EventList.invoke`` can now handle an arbitrary number of arguments
- Change: lower double click threshold to 200ms instead of 500ms
- Change: runtime font-style does not change font being used in this version. This will temporarely break rich text support.
- Fix: custom shader regression where multiple objects using the same material were not rendered correctly
- Fix: Text sometimes using invalid path
- Fix: Remove unused imports

## [2.23.0-pre] - 2022-09-20
### Exporter
- Add: support for ignoring types commented out using ``//``. For example ``// export class MyScript ...``
- Add: ``Setup Scene`` context menu creates directional light if scene contains no lights
- Add: support for environment light intensity multiplier
- Change: typescache will only be updated on codegen, project change or dependencies changed
- Change: improve font caching and regenerating atlas for better dynamic font asset support

### Engine
- Add basic support for ``CameraDepth`` and ``OpaqueTexture`` (the opaque texture still contains transparent textures in this first version) being used in custom shaders

## [2.22.1-pre] - 2022-09-17
### Exporter
- Fix missing dependency error when serialized depedency in ExportInfo was installed to package.json without the npmdef being present in the project.
- Fix typo in BoxGizmo field name

### Engine
- Improve Animator root motion blending
- Fix SpatialTrigger error when adding both SpatialTrigger as well as SpatialTrigger receiver components to the same object
- AnimatorController can now handle states with empty motion or missing clips

## [2.22.0-pre] - 2022-09-15
### Exporter
- Add: automatic runtime font atlas generation from Unity font assets 
- Change: setup scene menu item does not create grid anymore and setup scene
- Fix: serialization where array of assets that are copied to output directory would fail to export when not all entries of the array were assigned
- Fix: obsolete SRP renderer usage warning in Unity 2021
- Fix: serialize LayerMask as number instead of as ``{ value: <number> }`` object

### Engine
- Add: automatic runtime font atlas generation from Unity font assets 
- Remove: shipped font assets in ``include/fonts``
- Fix: Physics pass custom vector into ``getWorldPosition``, internal vector buffer size increased to 100
- Fix: SpatialTrigger and SpatialTrigger receivers didnt work anymore due to LayerMask serialization

## [2.21.1-pre] - 2022-09-14
### Exporter
- Bump Needle Engine version
- Fix: WebXR default avatar hide hands in AR
- Change: UI disable shadow receiving and casting by default, can be configured via Canvas

### Engine
- Change: UI disable shadow receiving and casting by default, can be configured via Canvas
- Fix: ``gameObject.getComponentInParent`` was making false call to ``getComponentsInParent`` and returning an array instead of a single object
- Fix: light intensity in AR

## [2.21.0-pre] - 2022-09-14
### Exporter
- Remove legacy UnityGLTF export warning
- Fix: add dependencies to Unity package modules (this caused issues when installing in e.g. URP project template)
- Change: will stop running local server before installing new package version
- Change: Bump UnityGLTF version to 1.15.0-pre

### Engine
- Add: first draft of Animator root motion support
- Fix: ``Renderer.sharedMaterials`` assignment bug when GameObject was mesh
- Fix: Buttons set to color transition did not apply transition colors
- Fix: UI textures being flipped
- Fix: UI textures were not stretched across panel but instead being clipped if the aspect ratio didnt match perfectly

## [2.20.0-pre] - 2022-09-12
### Exporter
- Add Timeline AnimationTrack ``SceneOffset`` setting export
- Change: improved ProjectReporter (``Help/Needle Engine/Zip Project``)

### Engine
- Add stencil support to ``Renderer``
- Add timeline ``removeTrackOffset`` support
- Fix timeline animation track offset only being applied to root
- Fix timeline clip offsets not being applied when no track for e.g. rotation or translation exists

## [2.19.0-pre] - 2022-09-11
### Exporter
- Add ShadowCatcher enum for toggling between additive and ShadowMask
- Add initial support for exporting URP RenderObject Stencil settings
- Add support for animating ``activeSelf`` and ``enabled``
- Change: improved ProjectReporter (``Help/Needle Engine/Zip Project``)
- Bump: UnityGLTF dependency

### Engine
- Add initial UI anchoring support
- Add initial support for URP RenderObject Stencil via ``NEEDLE_render_objects`` extension

## [2.18.3-pre] - 2022-09-09
### Exporter
- Bump runtime engine dependency

### Engine
- Fix UI transform handling for [issue 42](https://github.com/needle-tools/needle-engine-support/issues/42) and [issue 30](https://github.com/needle-tools/needle-engine-support/issues/30)
- Fix AudioSource not restarting to play at onEnable when ``playOnAwake`` is true (this is the default behaviour in Unity)

## [2.18.2-pre] - 2022-09-09
### Exporter
- Change default skybox size to 256
- Fix hash cache directory not existing in certain cases

### Engine
- Fix RGBAColor not implementing copy which caused alpha to be set to 0 (this caused ``Camera.backgroundColor`` to not work properly)

## [2.18.1-pre.1] - 2022-09-08
### Exporter
- Fix gitignore not found
- Fix hash cache directory not existing in certain cases

## [2.18.0-pre] - 2022-09-08
### Exporter
- Add ``Zip Project`` in ``Help/Needle Engine/Zip Project`` that will collect required project assets and data and bundle it

## [2.17.3-pre] - 2022-09-07
### Exporter
- Add auto fix if .gitignore file is missing
- Add menu item to only build production dist with last exported files (without re-exporting scene)
- Fix dependency change event causing error when project does not exist yet / on creating a new project
- Fix updating asset hash in cache directory when exporting

### Engine
- Add support to set OrbitControls camera position immediately

## [2.17.2-pre] - 2022-09-07
### Exporter
- Bump Engine dependency version

### Engine
- Fix EventList invocation not using deserialized method arguments

## [2.17.1-pre] - 2022-09-07
### Exporter
- Fix DirectoryNotFound errors caused by dependency report and depdendency cache
- Fix writing dependency hash if exported from play buttons (instead of save) and hash file doesnt exist yet

## [2.17.0-pre] - 2022-09-07
### Exporter
- Add export on dependency change and skip exporting unchanged assets
- Add ``EmbedSkybox`` toggle on GltfObject component
- Add simple skybox export size heuristic when no texture size is explictly defined (256 for prefab skybox, 1024 for scene skybox)
- Add debug information log which allows for basic understanding of why files / assets were exported
- Remove old material export code
- Change: clamp skybox size to 8px
- Fix skybox texture settings when override for Needle Engine is disabled, fallback is now to default max size and size
- Fix exceptions in ``Collect Logs`` method
- Fix Glitch ``Deploy`` button to only enable if deployment folder contains any files

### Engine
- Add ``context`` to ``StateMachineBehaviour``
- Fix ``StateMachineBehaviour`` binding (event functions were called with wrong binding)

## [2.16.0-pre.3] - 2022-09-06
### Exporter
- Fix compiler error when no URP is installed in project

### Engine
- Fix deserialization error when data is null or undefined

## [2.16.0-pre.1] - 2022-09-05
### Exporter
- Add EXR extension and export (HDR skybox)
- Add initial tonemapping and exposure support (start exporting Volume profiles)
- Add AR ShadowCatcher
- Add automatic re-export of current scene if referenced asset changes
- Fix potential nullref in BugReporter
- Change: add additional info to test npm installed call 
- Change server process name to make it more clear that it's the local development server process 
- Change: bumb UnityGLTF dependency

### Engine
- Add initial tonemapping and exposure support
- Add AR shadow catcher
- Fix objects parented to camera appear behind camera
- Fix reticle showing and never disappearing when no WebARSessionRoot is in scene
- Fix WebARSessionRoot when on same gameobject as WebXR component
- Fix deserialization of ``@serializable(Material)`` producing a new default instance in certain cases
- Fix ``OrbitControls`` enable when called from UI button event
- Fix EventList / UnityEvent calls to properties (e.g. ``MyComponent.enable = true`` works now from UnityEvent)

## [2.15.1-pre] - 2022-09-02
### Exporter
- Add skybox export using texture importer settings (for Needle Engine platform) if you use a custom cubemap
- Bump ShaderGraph dependency
- Fix compiler error in Unity 2021
- Change automatically flag component compiler typemap to be regenerated if any generated C# has compiler errors

### Engine
- Change: ``OrbitControls.setTarget`` does now lerp by default. Use method parameter ``immediate`` to change it immediately
- Change: bump component compiler dependency to ``1.8.0``

## [2.14.2-pre] - 2022-09-01
### Exporter
- Bump runtime dependency
- Fix settings window not showing settings when nodejs/npm is not found

### Engine
- Fix EventList serialization for cross-glb references
- Fix AnimatorController transition from state without animation

## [2.14.0-pre] - 2022-09-01
### Exporter
- Add: mark GltfObjects in scene hierarchy (hierarchy elements that will be exported as gltf/glb files)
- Add FAT32 formatting check and warning
- Fix: setup scene
- Fix: try improving ComponentGenerator component to watch script/src changes more reliably

### Engine
- Fix: skybox/camera background on exit AR
- Change: AnimatorController can now contain empty states
- Change: Expose ``Animator.Play`` transition duration

## [2.13.1-pre] - 2022-08-31
### Exporter
- Fix UnityEvent argument serialization 
- Fix generic UnityEvent serialization 

## [2.13.0-pre] - 2022-08-31
### Exporter
- Add report bug menu items to collect project info and logs
- Remove legacy ResourceProvider code

### Engine
- Improved RectTransform animation support and canvas element positioning
- Fix ``Animator.Play``
- Change: Expose ``AnimatorController.FindState(name)`` 

## [2.12.1-pre] - 2022-08-29
### Exporter
- Fix UnityEvent referencing GameObject

## [2.12.0-pre] - 2022-08-29
### Exporter
- Add UI to gltf export
- Add better logging for Glitch deployment to existing sites that were not remixed from Needle template and dont expose required deployment api
- Add AnimatorController support for any state transitions

### Engine
- Add UI to gltf export
- Add button animation transition support for triggers ``Normal``, ``Highlighted`` and ``Pressed``

## [2.11.0-pre] - 2022-08-26
### Exporter
- Add Linux support
- Add additional npm search paths for OSX and Linux to the settings menu
- Add ShaderGraph dependency to fix UnityGLTF import errors for projects in 2021.x
- Fix exporting with Animation Preview enabled

### Engine
- Add ``Canvas.renderOnTop`` option
- Fix ``OrbitControls`` changing focus/moving when interacting with the UI
- Fix nullref in AnimatorController with empty state

## [2.10.0-pre] - 2022-08-25
### Exporter
- Add export for ``Renderer.allowOcclusionWhenDynamic``
- Fix issue in persistent asset export where gameObjects would be serialized when referenced from within an asset

### Engine
- Add export for ``Renderer.allowOcclusionWhenDynamic``
- Fix: bug in ``@serializable`` type assignment for inherited classes with multiple members with same name but different serialized types
- Change: ``GameObject.findObjectOfType`` now also accepts an object as a search root

## [2.9.5-pre] - 2022-08-25
### Exporter
- OSX: add homebrew search path for npm

### Engine
- Fix canvas button breaking orbit controls [issue #4](https://github.com/needle-tools/needle-engine-support/issues/4)

## [2.9.4-pre.1] - 2022-08-23
### Exporter
- Fix glitch component for private projects

## [2.9.3-pre] - 2022-08-23
### Exporter
- Fix passing UnityGLTF export settings to exporter
- Fix old docs link
- Fix timeline extension export in certain cases, ensure it runs before component extension export
- Update minimal template

### Engine
- Fix SyncedRoom to not append room parameter multiple times

## [2.9.2-pre] - 2022-08-22
### Exporter
- Fix: Minor illegal path error
- Change: ExportInfoEditor ``Open`` button to open exporter package
- Change: ExportInfoEditor clear versions cache when clicking update button

### Engine
- Add: Timeline AudioTrack nullcheck when audio file is missing
- Fix: AnimatorController error when behaviours are undefined
- Change StateMachineBehaviour methods to be lowercase

## [2.9.1-pre] - 2022-08-22
### Exporter
- Fix build errors and compilation warnings

## [2.9.0-pre] - 2022-08-22
### Exporter
- Add initial StateMachineBehaviour support with "OnStateEnter", "OnStateUpdate" and "OnStateExit"
- Update UnityGLTF dependency
- Fix: prevent scene templates from cloning assets even tho cloning was disabled
- Fix: ifdef for URP

### Engine
- Add initial StateMachineBehaviour support with "OnStateEnter", "OnStateUpdate" and "OnStateExit"
- Fix input raycast position calculation for scrolled content

## [2.8.2-pre] - 2022-08-22
### Exporter
- Fix exporting relative path when building distribution: audio path did produce absolute path because the file was not yet copied
- Fix bundle registry performance bug causing a complete reload / recreation of FileSystemWatchers
- Fix texture pointer remapping in gltf-transform opaque extension
- Change: skip texture-transform for textures starting with "Lightmap" for now until we can configure this properly 

### Engine
- Fix texture pointer remapping in gltf-transform opaque extension
- Change: skip texture-transform for textures starting with "Lightmap" for now until we can configure this properly 

## [2.8.1-pre] - 2022-08-19
### Exporter
- Fix rare timeline export issue where timeline seems to have cached wrong data and needs to be evaluated once
- Update sharpziplip dependency

## [2.8.0-pre] - 2022-08-18
### Exporter
- Add new template with new beautiful models
- Change start server with ip by default from Play button too
- Fix Glitch deployment inspector swapping warning messages when project does not exist
- Fix certificate error spam when port is blocked by another server

### Engine
- Add scale to instantiation sync messages
- Fix ``BoxHelper``
- Fix AR reticle being not visible when ``XRRig`` is child of ``WebARSessionRoot`` component
- Fix exception in ``DragControls`` when dragged object was deleted while dragging

## [2.7.0-pre] - 2022-08-18
### Exporter
- Change name of ``KHR_webgl_extension`` to ``NEEDLE_webgl_extension``
- Change start server to use IP by default (ALT to open with localhost)
- Fix export cull for ShaderGraph with ``RenderFace`` option (instead of ``TwoSided`` toggle)

### Engine
- Change name of ``KHR_webgl_extension`` to ``NEEDLE_webgl_extension``
- Change: dont write depth for custom shader set to transparent 
- Deprecate and disable ``AssetDatabase``

## [2.6.1-pre] - 2022-08-17
### Exporter
- Add codegen buttons to npmdef inspector (regenerate components, regenerate C# typesmap)
- Add DefaultAvatar and SyncedCam default prefab references
- Change: allow cancelling process task when process does not exist anymore
- Change: ExportInfo inspector cleanup and wording
- Fix Timeline Preview on export (disable and enable temporarely)
- Fix constant names
- Fix XR buttons in project templates
- Fix VideoPlayer for iOS
- Fix Editor Only hierarchy icon
- Fix order of menu items and cleanup/remove old items
- Fix timeline clip offset when not offset should be applied
- Fix project templates due to renamed web component
- Fix and improve setup scene menu item

### Engine
- Add ``Mathf.MoveTowards``
- Change: rename ``needle-tiny`` webcomponent to ``needle-engine``
- Fix ordering issue in needle web component when codegen.js is executed too late

## [2.5.0-pre] - 2022-08-16
### Exporter
- Add ShaderGraph double sided support
- Add ShaderGraph transparent support
- Add SyncedCamera prefab support
- Remove legacy shader export code

### Engine
- Add SyncedCamera prefab/AssetReference support
- Add TypeArray support for ``serializable`` to provide multiple possible deserialization types for one field (e.g. ``serializable([Object3D, AssetReference])`` to try to deserialize a type as Object3D first and then as AssetReference)

## [2.4.1-pre] - 2022-08-15
### Exporter
- Add error message when trying to export compressed gltf from selection but engine is not installed.

### Engine
- Add event callbacks for Gltf loading: ``BeforeLoad`` (use to register custom extensions), ``AfterLoaded`` (to receive loaded gltf), ``FinishedSetup`` (called after components have been created)

## [2.4.0-pre] - 2022-08-15
### Exporter
- Add minimal analytics for new projects and installations
- Add log to feedback form
- Fix minor context menu typo

## [2.3.0-pre] - 2022-08-14
### Exporter
- Add warning to Camera component when background type is solid color and alpha is set to 0
- Add ``CameraARData`` component to override AR background alpha
- Change Glitch deployment secret to only show secret in plain text when ALT is pressed and mouse is hovered over password field 
- Fix ``ExportInfo`` editor "(local)" postfix for installed version text at the bottom of the inspector
- Fix scene templates build command
- Fix Glitch project name paste to not wrongly show "Project does not exist"

### Engine
- Fix AnimatorController exit state
- Fix AR camera background alpha to be fully transparent by default 

## [2.2.1-pre] - 2022-08-12
### Exporter
- Add: Export context menu to scene hierarchy GameObjects
- Fix: Multi column icon rendering in ProjectBrowser
- Fix: Builder now waits for installation finish
- Fix: Copy include command does not log to console anymore
- Fix: Invalid glb filepaths
- Fix: URP light shadow bias exported from RendererAsset (when setup in light)

### Engine
- Fix: light shadow bias

## [2.2.0-pre] - 2022-08-11
### Exporter
- Add: Problem solver "Fix" button
- Change: Use Glitch Api to detect if a project exists and show it in inspector
- Change: Typescript template file
- Change: Disable codegen for immutable packages
- Change: Improved problem solver messages
- Change: Renamed package.json scripts
- Change: Run "copy files" script on build (to e.g. load pre-packed gltf files at runtime when project was never built before)
- Fix: Logged Editor GUI errors on export
- Fix: gltf-transform packing for referenced textures via pointers
- Fix: Don't try to export animations for "EditorOnly" objects in timeline
- Fix: ComponentLink does now npmdef VSCode workspace

### Engine
- Add ``@needle-tools/engine`` to be used as import for "most used" apis and functions
- Change: remove obsolete ``Renderer.materialProperties``
- Fix: ``NEEDLE_persistent_assets`` extension is now valid format (change from array to object)

## [2.1.1-pre] - 2022-08-09
### Exporter
- Add Option to Settings to disable automatic project fixes
- Fix Build Window

## [2.1.0-pre] - 2022-08-08
### Exporter
- Add fixes to automatically update previous projects

## [2.0.0-pre] - 2022-08-08
### Exporter
- Renamed package
- Add: npmdef pre-build callback to run installation if any of the dependencies is not installed
- Add: Glitch Deployment inspector hold ALT to toggle build type (development or production)

### Engine
- Renamed package

## [1.28.0-pre] - 2022-08-08
### Exporter
- Add: Custom Shader vertex color export
- Add: NestedGltf objects and components do now have a stable guid
- Fix: NestedGltf transfrom

### Engine
- Fix: NestedGltf transform

## [1.27.2-pre] - 2022-08-06
### Exporter
- Remove: Scene Inspector experimental scene asset assignment
- Change: update templates
- Change: Component guid generator file ending check to make it work for other file types as well
- Change: add logo to scenes in project hierarchy with Needle Engine setup

### Engine
- Remove: Duplicateable animation time offset hack
- Change: GameObjectData extension properly await assigning values
- Change: NestedGltf instantiate using guid
- Change: ``instantiate`` does now again create guids for three Objects too

## [1.27.1-pre] - 2022-08-05
### Exporter
- Change: always export nested GlbObjects
- Change: update scene templates
- Change: Spectator camera component now requires camera component

### Engine
- Add: NestedGltf ``listenToProgress`` method
- Add: Allow changing Renderer lightmap at runtime
- Fix: Environment lighting when set to flat or gradient (instead of skybox)
- Fix: ``this.gameObject.getComponentInChildren`` - was internally calling wrong method
- Fix: Spectator camera, requires Camera component in glb now

## [1.27.0-pre] - 2022-08-03
### Exporter
- Add: warning if lightmap baking is currently in progress
- Add: support to export multiple selected objects
- Change: Audio clips are being exported relative to glb now (instead of relative to root) to make context menu export work, runtime needs to resolve the path relative to glb
- Fix: Selected object export collect types from ExportInfo

### Engine
- Add: Animator.keepAnimatorStateOnDisable, defaults to false as in Unity so start state is entered on enable again
- Add: warning if different types with the same name are registered
- Add: timeline track ``onMutedChanged`` callback
- Change: PlayableDirector expose audio tracks
- Change: BoxCollider and SphereCollider being added to the physics scene just once
- Change: try catch around physics step


## [1.26.0-pre] - 2022-08-01
### Exporter
- Add: open component compiler menu option to open Npm package site
- Add: feedback form url menu item
- Add: support for nested ``GltfObject``
- Add: support to copy gltf files in your hierarchy to the output directory instead of running export process again (e.g. a ``.glb`` file that is already compressed will just be copied and not be exported again. Adding components or changing values in the inspector won't have any effect in that case)
- Change: Don't export skybox for nested gltfs
- Change: bump component compiler dependency to ``1.7.2``
- Change: Unity progress name changed when running Needle Engine server process
- Remove: legacy export options on ``GltfObject``, components will now always be exported inside gltf extension
- Fix: delete empty folder when creating a new scene from a scene template 
- Fix: CodeWatcher error caused by repaint call from background thread
- Fix: Don't serialize in-memory scene paths in settings (when creating scenes from scene templates)
- Fix: Array serialization of e.g. AudioClip[] to produce Array<string> (because audio clips will be copied to the output directory and be serialized as strings which did previously not work in arrays or lists)
- Fix: component link opens workspace again
- Fix: scene save on scene change does not trigger a new export/build anymore

### Engine
- Add: Addressable download progress is now observeable
- Add: Addressable preload support, allows to load raw bytes without actually building any components
- Add: PlayableDirector exposes tracks / clips
- Change: modify default engine loading progress bar to be used from user code
- Change: add option to Instantiate call to keep world position (set ``keepWorldPosition`` in ``InstantiateOptions`` object that you can pass into instantiate)
- Change: light uses shadow bias from Unity
- Fix: instancing requiring worldmatrix update being not properly processed
- Fix: Duplicatable world position being off (using ``keepWorldPosition``)
- Fix: ``Animation`` component, it does allow to use one main clip only now, for more complex setups please use AnimationController or Timeline
- Fix: ``SyncedRoom`` room connection on enter WebXR
- Fix: WebXR avatar loading

## [1.25.0-pre] - 2022-07-27
### Exporter
- Add: Send upload size in header to Glitch to detect if the instance has enough free space
- Add: menu item to export selected object in hierarchy as gltf or glb
- Add: Timeline animation track infinite track export (when a animation track does not use TimelineClips)
- Add: ``AnimatorData`` component to expose and support random animator speed properties and random start clip offsets to easily randomize scenes using animators with the same AnimatorController on multiple GameObjects
- Fix: npmdef import, sometimes npmdefs in a project were not registered/detected properly which led to problems with installing dependencies
- Fix: script import file-gen does not produce invalid javascript if a type is present in multiple packages
- Change: improved error log message when animation export requires ``KHR_animation_pointer``
- Change: server starts using ``localhost`` url by default and can be opened by ip directly by holding ALT (this removes the security warning shown by browsers when opening by ip that does not have a security certificate which is only necessary if you want to open on another device like quest or phone. It can still be opened by ip and is logged in he console if desired)

### Engine
- Change: bump component compiler dependency to ``1.7.1``
- Change: ``context.mainCameraComponent`` is now of type ``Camera``
- Fix: timeline control track
- Fix: timeline animation track post extrapolation
- Fix: custom shader does not fail when scene uses object with transmission (additional render pass)

## [1.24.2-pre] - 2022-07-22
### Exporter
- Add: Deployment component now also shows info in inspector when upload is in process 
- Fix: cancel deploy when build fails
- Fix: better process handling on OSX

## [1.24.1-pre] - 2022-07-22
### Exporter
- Change: ``Remix on Glitch`` button does not immediately remix the glitch template and open the remixed site

## [1.24.0-pre] - 2022-07-21
### Exporter
- Add: glitch deploy auto key request and assignment. You now only need to paste the glitch project name when remixed and the deployment key will be requested and stored automatically (once after remix)
- Fix: process output log on OSX
- Fix: process watcher should now use far less CPU
- Change: move internal publish code into separate package
### Engine
- add loading bar and show loading state text

## [1.23.1-pre] - 2022-07-20
- Fix check if toktx is installed
- Fix: disable build buttons in Build Settings Window and Deployment component when build is currently running
- Fix: dont allow running multiple upload processes at once
- engine: add using ambient light settings (Intensity Multiplier) exported from Unity

## [1.23.0-pre] - 2022-07-18
- Update UnityGLTF dependency version
- Fix packing texture references on empty gameobjects
- Fix npmdef problem factory for needle.engine and three packages
- Add help urls to our components
- engine: fix nullref in registering texture

## [1.22.0-pre.2] - 2022-07-18
- Refactor problem validation and fixing providing better feedback messages
- Add: log of component that is not installed to runtime project but used in scene
- Change: Glitch deploy buttons
- Change: Build Settings window with new icons

## [1.21.0-pre] - 2022-07-15
- Add: moving npmdef in project should now automatically resolve path in package.json (if npmdef name didnt change too)
- Add: ``Show in explorer`` to scene asset context menu
- Add: warn when component is used in scene/gltf that is not installed to current runtime project
- engine: remove legacy file
- engine: add basic implementation of ``Context.destroy``
- engine: fix ``<needle-tiny>`` src attribute
- engine: add implictly creating camera with orbit controls when loaded glb doesnt contain any (e.g. via src) 

## [1.20.3-pre.1] - 2022-07-13
- Fix exception in ComponentCompiler editor
- Fix type list for codegen including display and unavailable types

## [1.20.2-pre.2] - 2022-07-12
- Add warning to Typescript component link (in inspector) when component on GameObject is not the codegen one (e.g. multiple components with the same name exist in the project)
- Change component compiler to not show ``install`` button when package is not installed to project
- Change recreate codewatchers on editor focus
- engine: fix dont apply lightmaps to unlit materials
- engine: remove log in PlayableDirector
- engine: add support to override (not automatically create) WebXR buttons 

## [1.20.1-pre] - 2022-07-11
- Fix TypesGenerator log
- Fix ExportInfo editor when installing
- Fix: ComponentCompiler serialize path relative to project
- Fix Inspector typescript link
- Fix AnimatorController serialization in persistent asset extension
- Fix AnimatorController serialization of transition conditions
- Add more verbose output for reason why project is not being installed, visible when pressing ALT
- Fix process output logs to show more logs
- Update component compiler default version to 1.6.2
- engine: Fix AnimatorController finding clip when cloned via ``AssetReference.instantiate``
- engine: Fix deep clone array type
- engine: Fix PlayableDirectory binding when cloned via ``AssetReference.instantiate``

## [1.20.0-pre] - 2022-07-10
- Add info to ExportInfo component when project is temporary (in Library folder)
- Add ``Open in commandline`` context menu to ExportInfo component
- Add generating types.json for component generator to remove need to specify C# types explicitly via annotations
- Add context menu to ComponentGenerator component version text to open changelog
- Change: hold ALT to perform clean install when clicking install button
- Fix: KHR_animation_pointer now works in production builds
- engine: add VideoPlayer using ``AudioOutputMode.None``
- engine: fix VideoPlayer waiting for input before playing video with audio (unmuted) and being loaded lazily

## [1.19.0-pre] - 2022-07-07
- Add: automatically import npmdef package if npmdef package.json contains (existing) ``main`` file
- Add: Timeline serializer does not automatically create asset model from custom track assets for fields marked with ``[SerializeField]`` attribute
- Change: PlayableDirector allow custom tracks without output binding
- engine: Add ``getComponent`` etc methods to THREE.Object3D prototype so we can use it like in Unity: ``this.gameObject.getComponent...``
- engine: Change ``Duplictable`` serialization

## [1.18.0-pre] - 2022-07-06
- Add temp projects support: projects are temp projects when in Unity Library
- Change prevent creating project in Temp/ directory because Unity deletes content of symdir directories
- Change ExportInfo update button to open Package Manager by default (hold ALT to install without packman)
- Change starting processes with ``timeout`` instead of ``pause``
- Change: try install npmdef dependency when package.json is not found in node_modules
- Fix ComponentGenerator path selection
- Fix warning from UnityGLTF api change
- Fix codegen import of register_types on very first build
- engine: Fix networking localhost detection
- engine: update component generator package version (supporting now CODEGEN_START and END sections as well as //@ifdef for fields)

## [1.17.0-pre] - 2022-07-06
- Add mathematics #ifdef
- Change NpmDef importer to enable GUI to be usable in immutable package
- Change Move modules out of this package
- Fix ``Start Server`` killing own server again
- Fix error when searching typescript workspace in wrong directory
- Change lightmap extension to be object
- engine: change lightmap extension to be object

## [1.16.0-pre] - 2022-07-06
- Add DeviceFlag component
- Add build stats log to successfully built log printing info about file sizes
- Add warning for when Unity returns missing/null lightmap
- Add VideoPlayer ``isPlaying`` that actually checks if video is currently playing
- Add ObjectField for npmdef files to SceneEditor
- Fix BuildTarget for 2022
- Fix serializing UnityEvent without any listeners
- Fix seriailizing component ``enable`` state
- Fix skybox in production builds
- Improve VideoTrack editor preview
- Improve glitch deploy error message when project name is wrong
- Update gltf-transform versions in project templates
- Update UnityGLTF method names for compatibility with 1.10.0-pre
- engine: Add DeviceFlag component
- engine: Fix VideoPlayer loop and playback speed
- engine: Improve VideoTrack sync

## [1.15.0-pre] - 2022-07-04
- add VideoTrack export
- add Spline export
- fix ComponentCompiler finding path automatically
- fix Unity.Mathematics float2, float3, float4 serialization
- change: ExportInfo shows version during installation 
- engine: fix ``enabled`` not being always assigned
- engine: fix react-three-fiber component setting camera
- engine: add support for custom timeline track
- engine: add VideoTrack npmdef

## [1.14.3-pre] - 2022-07-01
- Add: installation progress now tracks and warns on installations taking longer than 5 minutes
- engine: Change; PlayableDirector Wrap.None now stops/resets timeline on end
- engine: Change; PlayableDirector now stops on disabling  

## [1.14.2-pre] - 2022-07-01
- Update UnityGltf dependency
- engine: fix timeline clip offsets and hold
- engine: fix timeline animationtrack support for post-extrapolation (hold, loop, pingpong)

## [1.14.1-pre] - 2022-06-30
- Fix: exception in code watcher when creating new npmdef
- Fix: issue when deleting npmdef
- engine: improve timeline clip offsets

## [1.14.0-pre] - 2022-06-30
- Add: export timeline AnimationTrack TrackOffset
- engine: Improved timeline clip- and track offset (ongoing)
- engine: Change assigning all serialized properties by default again (instead of require ``@allProperties`` decorator)
- engine: Change; deprecate ``@allProperties`` and ``@strict`` decorators

## [1.13.2-pre] - 2022-06-29
- Fix Playmode override
- Fix: Dispose code watcher on npmdef rebuild
- Add button to open npmdef directory in commandline
- Change: keep commandline open on error
- engine: add methods for unsubscribing to EventList and make constructor args optional
- engine: change camera to not change transform anymore

## [1.13.1-pre] - 2022-06-28
- Fix support for Unity 2022.1
- engine: add support for transparent rendering using camera background alpha

## [1.13.0-pre] - 2022-06-27
- Add: transform gizmo component
- Change: component generator for npmdef is not required anymore
- Change: component gen runs in background now
- Fix: typescript drag drop adding component twice in some cases
- engine: update component gen package dependency
- engine: fix redundant camera creation when exported in GLTF
- engine: fix orbit controls focus lerp, stops now on input

## [1.12.1-pre] - 2022-06-25
- Override PlayMode in sub-scene
- engine: lightmaps encoding fix
- engine: directional light direction fix 

## [1.12.0-pre] - 2022-06-25
- SceneAsset: add buttons to open vscode workspace and start server

## [1.11.1-pre] - 2022-06-25
- AnimatorController: can now be re-used on multiple objects
- Add support for exporting current scene to glb, export scene on save when used in running server
- Fix: issue that caused multi-select in hierarchy being changed
- Add glb and gltf hot-reload option to vite.config in template
- Add context menu to ``ExportInfo`` to update vite.config from template
- engine: animator controler can handle multiple target animators
- engine: fix WebXR being a child of WebARSessionRoot
- engine: improve Camera, OrbitControls, Lights OnEnable/Disable behaviour
- engine: add ``Input.getKeyPressed()``

## [1.10.0-pre] - 2022-06-23
- Support exporting multiple lightmaps
- Fix custom reflection being saved to ``Assets/Reflection.exr``
- engine: fix light error "can't add object to self" when re-enabled
- engine: remove extension log
- engine: log missing info when UnityEvent has not target (or method not found)
- engine: use lightmap index for supporting multiple lightmaps

## [1.10.0-pre] - 2022-06-23
- Support exporting multiple lightmaps
- Fix custom reflection being saved to ``Assets/Reflection.exr``
- engine: fix light error "can't add object to self" when re-enabled
- engine: remove extension log
- engine: log missing info when UnityEvent has not target (or method not found)
- engine: use lightmap index for supporting multiple lightmaps

## [1.9.0-pre] - 2022-06-23
- Initial support for exporting SceneAssets 
- GridHelper improved gizmo
- engine: Camera dont set skybox when in XR
- engine: dont add lights to scene if baked

## [1.8.1-pre] - 2022-06-22
- Automatically install referenced npmdef packages
- Refactor IBuildCallbackReceiver to be async
- Remove producing resouces.glb
- engine: fix threejs dependency pointer

## [1.8.0-pre.1] - 2022-06-22
- Add project info inspector to scene asset
- Add custom context menu to scene asset containing three export projects
- Export lightmaps and skybox as part of extension
- Known issue: production build skybox is not working correctly yet
- Fix dragdrop typescript attempting to add non-component-types to objects
- Allow overriding threejs version in project
- Bump UnityGLTF dependency
- engine: ``<needle-tiny>`` added awaitable ``getContext()`` (waits for scene being loaded to be used in external js)
- engine: fix finding main camera warning
- engine: add ``SourceIdentifier`` to components to be used to get gltf specific data (e.g. lightmaps shipped per gltf)
- engine: persistent asset resolve fix
- engine: update three dependency to support khr_pointer
- engine: remove custom khr_pointer extension
- engine: fix WebARSessionRoot exported in gltf
- engine: smaller AR reticle

## [1.7.0-pre] - 2022-06-17
- Component generator inspector: add foldout and currently installed version
- Npmdef: fix register_type when no types are in npmdef (previously it would only update the file if any type was found)
- Npmdef: importer now deletes codegen directory when completely empty
- Export: referenced prefabs dont require GltfObject component anymore
- engine: create new GltfLoader per loading request
- engine: fix bug in core which could lead to scripts being registered multiple times
- engine: Added SyncedRoom auto rejoin option (to handle disconnection by server due to window inactivity)
- engine: guid resolving first in loaded gltf and retry in whole scene on fail
- engine: fix nullref in DropListener
- engine: register main camera before first awake

## [1.6.0-pre.1] - 2022-06-15
- fix serializing components implementing IEnumerable (e.g. Animation component)
- update UnityGLTF dependency
- engine: add ``GameObject.getOrAddComponent``
- engine: ``OrbitControl`` exposing controlled object
- engine: ``getWorldPosition`` now uses buffer of cached vector3's instead of only one
- engine: add ``AvatarMarker`` to synced camera (also allows to easily attach ``PlayerColor``)
- engine: fix ``Animation`` component when using khr_pointer extension
- engine: ``VideoPlayer`` expose current time
- engine: fix ``Animator.runtimeController`` serialization
- engine: make ``SyncedRoom.tryJoinRoom`` public

## [1.5.1-pre] - 2022-06-13
- Generate components from js files
- Fix compiler error in 2022
- Improve component generator editor watchlist
- Serialize dictionary as object with key[] value[] lists
- Prevent running exporter while editor is building
- Remove empty folder triggering warning
- Fix component generator running multiple times per file when file was saved multiple times.

## [1.5.0-pre] - 2022-06-12
- Add ``Create/Typescript`` context menu
- Improved npmdef and typescript UX
- Improved component codegen: does now also delete generated components when typescript file or class will be deleted
- Component gen produces stable guid (generated from type name)

## [1.4.0-pre] - 2022-06-11
- Bumb UnityGLTF dependency to 1.8.0-pre
- Add typescript editor integration to NpmDef importer: typescript files are now being displayed in project browser with look and feel of native Unity C# components. They also show a link to the matching Unity C# component.
- Fix PathUtil error
- Fix register-types generator deleting imports for modules that are not installed in current project

## [1.3.4-pre] - 2022-06-10
- Custom shader: start supporting export for Unity 2022.1
- Custom shader: basic default texture support
- engine: allow ``@serializeable`` taking abstract types
- engine: add ``Renderer.sharedMaterials`` support

## [1.3.3-pre] - 2022-06-09
- engine: move log behind debug flag
- engine: improved serialization property assignment respecting getter only properties
- engine: add optional serialization callbacks to ``ISerializable``
- engine: default to only assign declared properties

## [1.3.2-pre] - 2022-06-09
- update UnityGLTF dependency to 1.7.0-pre
- add google drive module (wip)
- project gen: fix path with spaces
- ExportInfo: fix dependency list for npmdef (for Unity 2022)
- set types dirty before building
- engine: downloading dropped file shows minimal preview box
- engine: ``DropListener`` can use localhost
- engine: ``SyncedRoom`` avoid reload due to room parameter
- engine: ``LODGroup`` instantiate workaround
- engine: improve deserialization supporting multiple type levels

## [1.3.1-pre.2] - 2022-05-30
- minor url parsing fix
- engine: change md5 hashing package
- engine: file upload logs proper server error

## [1.3.1-pre.1] - 2022-05-30
- Check if toktx is installed for production build
- Lightmap export: treat wrong quality setting as error
- engine: disable light in gltf if mode is baked
- engine: use tiny starter as default networking backend
- engine: synced file init fix for resolving references
- engine: allow removing of gen.js completely
- engine: expose ``Camera.buildCamera`` for core allowing to use blender camera
- engine: on filedrop only add drag control if none is found

## [1.3.1-pre] - 2022-05-27
- Improved ``ComponentGenerator`` inspector UX
- Add inspector extension for ``AdditionalComponentData<>`` implementations
- Update vite template index.html and index.scriban
- engine: fix networked flatbuffer state not being stored
- engine: make ``src`` on ``<needle-tiny>`` web component optional
- engine: ``src`` can now point to glb or gltf directly
- engine: fix ``Raycaster`` registration
- engine: add ``GameObject.destroySynced``
- engine: add ``context.setCurrentCamera``
- engine: make ``DropListener`` to EventTarget
- engine: make ``DropListener`` accept explicit backend url

## [1.3.0-pre.1] - 2022-05-26
- NPM Definition importer show package name
- PackageUtils: add indent
- MenuItem "Setup Scene" adds ``ComponentGenerator``
- Added minor warnings and disabled menu items for non existing project
- Fix gltf transform textures output when used in custom shaders only
- Fix ``ExportShader`` asset label for gltf extension

## [1.3.0-pre] - 2022-05-25
- Add ``ExportShader`` asset label to mark shader or material for export
- Add output folder path to ``IBuildDistCallbackReceiver`` interface
- Add button to NpmDef importer to re-generate all typescript components
- Add ``IAdditionalComponentData`` and ``AdditionalComponentData`` to easily emit additional data for another component
- engine: fix ``VideoPlayer`` being hidden, play automatically muted until interaction
- engine: added helpers to update window history
- engine: fix setting custom shader ``Vector4`` property

## [1.2.0-pre.4] - 2022-05-25
- Fix project validator local path check
- remove ``@no-check```(instead should add node_modules as baseUrl in tsconfig)
- fix ``Animation`` component serialization
- engine: fix tsc error in ``Animation`` component
- engine: fix ``Animation`` component assigning animations for GameObject again
- engine: fix ``Animation`` calling play before awake
- engine: ``AnimatorController`` handle missing motion (not assigned in Unity)
- engine: ``AnimatorController.IsInTransition()`` fix

## [1.2.0-pre.1] - 2022-05-20
- Disable separate installation of ``npmdef`` on build again as it would cause problems with react being bundled twice
- Add resolve for react and react-fiber to template vite.config
- Adding ``@no-check`` to react component as a temporary build fix solution
- Make template ``floor.fbx`` readable
- engine: minor tsc issues fixed

## [1.2.0-pre] - 2022-05-20
- Add initial react-three-fiber template
- Vite template: cleanup dependencies and add http2 memory workaround
- Dont show dependencies list in ``ExportInfo`` component when project does not exist yet
- Creating new npmdef with default ``.gitignore`` and catch IOException
- Building with referenced but uninstalled npmdef will now attempt to install those automatically
- engine: add ``isManagedExternally`` if renderer is not owned (e.g. when using react-fiber)

## [1.1.0-pre.6] - 2022-05-19
- Add resolve module for peer dependencies (for ``npmdef``) to vite project template
- Various NullReferenceException fixes
- Easily display and edit ``npmdef`` dependencies in ``ExportInfo`` component
- Add problem detection and run auto resolve for some of those problems (e.g. uninstalled dependency)
- engine: add basic support for ``stopEventPropagation`` (to make e.g. ``DragControls`` camera control agnostic in preparation of react support)

## [1.1.0-pre.5] - 2022-05-19
- ``Clean install`` does now delete node_modules and package-lock
- Mark types dirty after installation to fix missing types on first time install
- Fix ``npmdef`` registration on first project load

## [1.1.0-pre.2] - 2022-05-18
- improved ``NpmDef`` support

## [1.1.0-pre] - 2022-05-17
- fix ``EventList`` outside of gltf
- fix ``EventList`` without any function assigned (``No Function`` in Unity)
- fix minimal template gizmo icon copy
- start implementing ``NpmDef`` support allowing for modular project setup.
- engine: support changing ``WebARSessionRoot.arScale`` changing at runtime

## [1.0.0-pre.31] - 2022-05-12
- engine: fix webx avatar instantiate
- engine: stop input preventing key event defaults

## [1.0.0-pre.30] - 2022-05-12
- replace glb in collab sandbox template with fbx
- minor change in ``ComponentGenerator`` log
- add update info and button to ``ExportInfo`` component
- engine: log error if ``instantiate`` is called with false parent
- engine: fix instantiate with correct ``AnimatorController`` cloning

## [1.0.0-pre.29] - 2022-05-11
- engine: fix ``@syncField()``
- engine: fix ``AssetReference.instantiate`` and ``AssetReference.instantiateSynced`` parenting
- engine: improve ``PlayerSync`` and ``PlayerState``

## [1.0.0-pre.28] - 2022-05-11
- Move ``PlayerState`` and ``PlayerSync`` to experimental components
- Add TypeUtils to get all known typescript components
- Add docs to ``SyncTransform`` component
- Add support for ``UnityEvent.off`` state
- engine: prepend three canvas to the web element instead of appending
- engine: ``SyncedRoom`` logs warning when disconnected
- engine: internal networking does not attempt to reconnect on connection closed
- engine: internal networking now empties user list when disconnected from room
- engine: ``GameObject.instantiate`` does not always generate new guid to support cases where e.g. ``SyncTransform`` is on cloned object and requires unique id
- engine: ``syncedInstantiate`` add fallback to ``Context.Current`` when missing
- engine: ``EventList`` refactored to use list of ``CallInfo`` objects internally instead of plain function array to more easily attach meta info like ``UnityEvent.off`` 
- engine: add ``GameObject.instantiateSynced``

## [1.0.0-pre.27] - 2022-05-10
- add directory check to ``ComponentGenerator``
- parse glitch url in ``Networking.localhost``
- engine: fix font path
- engine: add ``debugnewscripts`` url parameter
- engine: start adding simplifcation to automatic instance creation + sync per player
- engine: allow InstantiateOptions in ``GameObject.instantiate`` to be inlined as e.g. ``{ position: ... }``
- engine: add ``AvatarMarker`` creation and destroy events
- engine: fix networking message buffering

## [1.0.0-pre.26] - 2022-05-10
- Fix js emitter writing guid for glTF root which caused guid to be present on two objects and thus resolved references for gltf root were wrong
- Improved ``SyncedRoom`` and ``Networking`` components tooltips and info
- Improved ``SyncedCam`` reference assignment - warn if asset is assigned instead of scene reference
- Build error fix
- Added versions to ``ExportInfo`` editor and context menu to quickly access package.jsons and changelogs

## [1.0.0-pre.25] - 2022-05-08
- Unity 2022 enter PlayMode fix for broken skybox when invoked from play button or ``[InitializeOnLoad]``
- Unity 2022 minor warning / obsolete fixes
- remove GltFast toggle in ``GltfObject`` as currently not supported/used
- fix build error
- rename and update scene templates
- engine: ``SpatialTrigger`` is serializable
- engine: fix ``DragControls`` offset when using without ground
- engine: fix ``WebXRController`` interaction with UI ``Button.onClick``

## [1.0.0-pre.24] - 2022-05-04
- fix ifdef in template component
- allow disabling component gen component
- fix exporting asset: check if it is root
- fix ``InputAction`` locking export
- engine: fix gltf extension not awaiting dependencies
- engine: fix persistent asset @serializable check for arrays
- engine: add ``setWorldScale``
- engine: fix ``instantiate`` setting position
- engine: ``AssetReference`` does now create new instance in ``instantiate`` call
- engine: add awaitable ``delay`` util method
- engine: fix scripts being active when loaded in gltf but never added to scene
- engine: minimal support for mocking pointer input
- engine: emit minimal down and up input events when in AR

## [1.0.0-pre.23] - 2022-05-03
- show warning in ``^2021`` that templating is currently not supported
- clean install now asks to stop running servers before running
- engine: improved default loading element
- play button does now ask to create a project if none exits

## [1.0.0-pre.22] - 2022-05-02
- lightmaps fixed
- glitch upload shows time estimate
- deployment build error fix
- json pointer resolve
- improved auto install
- started basic ``SpriteRenderer`` support
- basic ``AnimationCurve`` support
- fixed ``PlayerColor``
- fixed ``persistent_assets`` and ``serializeable`` conflict
- basic export of references to components in root prefab

## [1.0.0-pre.21] - 2022-04-30
- cleanup ``WebXR`` and ``WebXRSync``
- Play button does not also trigger installation and setup when necessary
- Fixed addressables export
- Added doc links to ``ComponentGenerator`` and updated urls in welcome window.
- ``Deployment.GlitchModel`` does now support Undo

## [1.0.0-pre.20] - 2022-04-29
- add internal publish button
- dont emit ``khr_techniques_webgl`` extension when not exporting custom shaders
- fix environment light export
- use newtonsoft converters when serializing additional data
- add ``Open Server`` button to ``ExportInfo`` component
- ``component-compiler`` logs command and log output file

## [1.0.0-pre.18] - 2022-04-27
- refactor extension serialization to use Newtonsoft

## [1.0.0-pre.11] - 2022-04-22
- initial release