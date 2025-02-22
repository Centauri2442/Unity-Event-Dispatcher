/*
Copyright 2025 CentauriCore LLC

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CentauriCore.EventDispatcher
{
    /// <summary>
    /// Interface that allows usage of the event dispatcher, a more performant alternative to Unity's built-in playerloop events.
    /// </summary>
    public interface IDispatcher
    {
        public virtual void FixedUpdateHandler(float fixedDeltaTime) { }
        
        public virtual void UpdateHandler(float deltaTime) { }
        
        public virtual void LateUpdateHandler(float deltaTime) { }
        
        public virtual void PostLateUpdateHandler(float deltaTime) { }
        
        /// <summary>
        /// Fires when handler targeting this script is added : Fires BEFORE but IN SAME FRAME as first update!
        /// </summary>
        /// <param name="eventType">PlayerLoop event type added</param>
        public virtual void OnAddHandler(EventDispatcher.EventType eventType) { }
        
        /// <summary>
        /// Fires when handler targeting this script is removed : Fires AFTER but IN SAME FRAME as its removal!
        /// </summary>
        /// <param name="eventType">PlayerLoop event type removed</param>
        public virtual void OnRemoveHandler(EventDispatcher.EventType eventType) { }
    }
}
