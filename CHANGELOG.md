## [1.0.1] 2025-02-22
## Bugfixes & Improvements
- Modified AddHandler, RemoveHandler, and HasHandler methods to explicitly ensure any code calling them contains the required MonoBehaviour during compile.
- Swapped when OnAddHandler and OnRemoveHandler is called in the execution order. OnAddHandler now fires after being added, and OnRemoveHandler now fires before being removed.

## [1.0.0] 2025-02-21
## First Release
- Event Manager that allows for scripts to register themselves for ordered PlayerLoop execution at a cheaper runtime cost
- Adds PostLateUpdate to PlayerLoop, which runs after LateUpdate