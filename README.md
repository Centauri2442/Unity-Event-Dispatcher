# UNITY EVENT DISPATCHER
#### The Event Dispatcher allows users to register classes to a central PlayerLoop manager, with an easy-to-use interface setup.
---
## Features
- Improved runtime performance at high PlayerLoop load by organizing calls through a single manager class.
- Implements PostLateUpdate, which runs after LateUpdate in the PlayerLoop.
---
## How To Use
The event dispatcher automatically adds itself to the scene, so the only setup required is on scripts wishing to use the system.

How to Add Handler
- Implement _IDispatcher_ interface.
- Add whichever handler methods you wish to use from _IDispatcher_.
- Call _EventDispatcher.AddHandler(EventType eventType, IDispatcher targetScript)_ on the target behaviour, specifying PlayerLoop eventType and the target script (Usually just self referencing as _this__).

How to Remove Handler
- Call _EventDispatcher.RemoveHandler(EventType eventType, IDispatcher targetScript)_, specifying PlayerLoop eventType and the target script (Usually just self referencing as _this__).
- Remove whichever handler methods you had added from IDispatcher.
- Un-implement _IDispatcher_ interface.
---
## Limitations
Currently, the Event Dispatcher only works for scripts derived from MonoBehaviour, due to how script cleanup is handled to prevent null exceptions.
