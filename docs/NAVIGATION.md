# Shared navigation foundation

Status: isolated, opt-in foundation; no consumer migration implied. Existing APIs remain unchanged.

## Evidence and boundary

Examined SRankSentinel at 6ee1a472df343bfe5c7158eb77801135e439145a (accepted runtime 0.7.54.0) and PvPSentinel at 0971ecdc53f5b4b5582295aab1748742164ae522.
Both use asynchronous path calculation, a separately submitted vnavmesh follower, mounting, readiness and bounded recovery. SRank owns hunt projections, parking/facing, tagging, positive kill/reset evidence and S/SS staging. PvP owns strategic destinations, map safety, formation and combat decisions. None of those domain policies move into Core.

Public XeldarAlz AutoHuntTrain README at 795b410756e80eb3895fec9d20e2442b474e0112 and AutoPVPSeriesGrind README at ada94edae9c6547321832a7cb47b4a7855dabc03 were reviewed for architectural capabilities (separate travel/movement/domain stages, cancellation, bounded recovery). Their licenses are AGPL-3.0-or-later. No implementation source was used or copied. Sentinel's implementation is independently authored from Sentinel's contracts and tests.

Verified upstream awgil/ffxiv_navmesh IPCProvider.cs and NavmeshManager.cs at 6fc80725eb8290472eee433fc4be7ee06ec79357: Nav.IsReady alone indicates a mesh object; Nav.BuildProgress is negative when no build/load is running. Pathfind returns data; Path.MoveTo starts following. PointOnFloor supports rejecting unlandable points. There is no public IPC giving the mesh's territory/instance identity.

## Contract

- One NavigationCoordinator per movement backend in a consumer. Construct lazily on the framework thread; Begin, Tick, Cancel, Dispose and projection must run there.
- Begin synchronously cancels the previous operation. Cancellation clears the pending task before stopping only the follower owned by that operation. A retired handle cannot affect its successor. Failed Stop poisons the coordinator and prevents ownership transfer.
- Async work has no movement continuations. Late results are abandoned; faults are observed without logging their potentially private message. Only the active task is consumed by Tick.
- Adapter is the sole writer during an operation. This is in-process ownership, not an ecosystem-wide lock: independent plugins or manual vnavmesh commands can still interfere. Do not run another movement owner concurrently during proving.
- Read supplies a coherent zone stamp (territory plus monotonically increasing epoch on zoning, including same-territory instance/world transitions), current mesh evidence, build progress, physical state, and the remaining ordered waypoints.
- Unknown/missing readiness IPC fails closed. MeshZone must remain null during zoning/uncertainty. The first adapter must invalidate on loading transitions and require stable post-load IsReady plus negative BuildProgress. Because upstream exposes no mesh identity, this is an explicit observed-readiness limitation requiring supervised validation; never label elapsed time alone proof of mesh provenance.
- Query and following are different states. Results are checked for current operation/zone, physical start drift, finite points and destination coverage before submission.
- Stall evidence is improvement toward the current waypoint or a decreased remaining waypoint count. Sideways displacement and oscillation do not reset progress.
- Every query/startup/readiness wait has a deadline, every operation an overall deadline, and recovery shares a finite retry budget. Recovery stops its owned path before another request.
- Mount/takeoff requests are rate-limited and require observed physical confirmation. Failed takeoff never silently downgrades to ground. PreferFlight uses ground only when flight is explicitly unavailable; changing availability or lost flight invalidates the existing route.
- Landing projection is a bounded, finite, readiness-gated primitive. Choosing a crowd-safe parking point and deciding when to land remain consumer policy.
- Cancellation tokens are passed to path adapters where supported. Older non-cancellable IPC can complete later; logical cancellation still rejects its result. Adapters must not stop followers in token callbacks.
- Diagnostics carry UTC timestamp, plugin/version, operation ID, state transition, enum reason, dependency state, limited physical context and result. Export schema sentinel.navigation.v1 is allowlisted and bounded; it has no arbitrary text/exception/config/chat fields, account/character/world names, actor IDs, credentials or coordinates. Existing DiagnosticBuffer stays compatible and is deliberately not exported.

## Proving sequence and rollback

Automated tests exercise ownership, late results, cancellation/disposal, zone/build readiness, origin/endpoint validation, timeout/retry budgets, waypoint stalls, mount/takeoff/flight changes, projection bounds, sanitized export and thread/stop failures.
First SRank adoption must be session-only opt-in, default legacy, restricted to ordinary long approach; landing/tag/kill/return stay existing policy. Stop/reload must disable the proving path and dispose its lease before legacy resumes. No PvP adoption until supervised SRank acceptance. Facing and responsive UI work are separate.

A successful build does not establish in-game correctness.

## Opt-in bounded landing (0.4.1)

The existing NavigationRequest constructor and INavigationAdapter remain unchanged. RequireLanding
defaults off; old destination-radius completion and enum numeric values are preserved. Opt-in requests
require ILandingNavigationAdapter and physical Grounded evidence on NavigationSnapshot.

At destination radius, Core invalidates outstanding query work, stops the owned follower, and keeps
the operation active in Landing. It never re-enters mounting/takeoff from Landing. Normal landing
requests are synchronous, airborne-only and at most once per second. Their acceptance is not success.
The consumer must verify a locally usable floor and domain safety before requesting a native action.

Success requires InFlight=false, Grounded=true, destination vertical distance <=1.5y and stable
confirmation for 0.75s. Mount retention on actual ground is permitted. Unknown evidence fails closed.
A fixed 20-second landing deadline is independent of repeated transition timestamps; dependency loss,
zone changes, external movement and drift terminate safely. Cancellation/disposal/replacement revoke
the old operation; no task or delayed cleanup may issue landing actions or stop the successor.

No automatic landing relocation or hunt parking strategy lives in Core. SRank's explicit probe is the
first consumer; its existing crowd-aware hunt landing remains legacy. Live landing proof is pending.
