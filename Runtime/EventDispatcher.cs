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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

namespace CentauriCore.EventDispatcher
{
    /// <summary>
    /// Manually dispatches out playerloop events, allowing us to improve performance by organizing the playerloop better than Unity does
    /// </summary>
    public class EventDispatcher : MonoBehaviour
    {
        public bool ShowDebugLogs = false;

        #region Singleton
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (!_instance)
            {
                var gameObj = new GameObject("EventDispatcher", new Type[] { typeof(EventDispatcher) });
                _instance = gameObj.GetComponent<EventDispatcher>();
            }
        }

        private static EventDispatcher _instance;

        public static EventDispatcher Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindFirstObjectByType<EventDispatcher>();
                }

                return _instance;
            }
        }

        #endregion
        
        public enum EventType
        {
            Update,
            LateUpdate,
            FixedUpdate,
            PostLateUpdate
        }
        
        private static IDispatcher[] UpdateDispatching = new IDispatcher[0];
        private static MonoBehaviour[] UpdateInternalScriptRefs = new MonoBehaviour[0]; // We store this ref for null checking
        
        private static IDispatcher[] LateUpdateDispatching = new IDispatcher[0];
        private static MonoBehaviour[] LateUpdateInternalScriptRefs = new MonoBehaviour[0]; // We store this ref for null checking
        
        private static IDispatcher[] FixedUpdateDispatching = new IDispatcher[0];
        private static MonoBehaviour[] FixedUpdateInternalScriptRefs = new MonoBehaviour[0]; // We store this ref for null checking
        
        private static IDispatcher[] PostLateUpdateDispatching = new IDispatcher[0];
        private static MonoBehaviour[] PostLateUpdateInternalScriptRefs = new MonoBehaviour[0]; // We store this ref for null checking


        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            
            SetupPostLateUpdate();
        }

        #region Custom PlayerLoops

        public void SetupPostLateUpdate()
        {
            // Retrieve the current player loop configuration from Unity
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            // Iterate over all subsystems in the player loop to find the PostLateUpdate phase
            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                if (playerLoop.subSystemList[i].type == typeof(UnityEngine.PlayerLoop.PostLateUpdate))
                {
                    // Get the current list of subsystems in the PostLateUpdate phase
                    var subSystems = playerLoop.subSystemList[i].subSystemList;
                    // Create a new array to hold the existing plus one new subsystem
                    var newSubSystems = new PlayerLoopSystem[subSystems.Length + 1];
                    // Copy the existing subsystems into the new array
                    Array.Copy(subSystems, 0, newSubSystems, 0, subSystems.Length);

                    // Define the new custom subsystem to run after LateUpdate
                    var postLateUpdateLoop = new PlayerLoopSystem
                    {
                        type = typeof(EventDispatcher),
                        updateDelegate = PostLateUpdate
                    };

                    // Add new subsystem to the end of the array
                    newSubSystems[subSystems.Length] = postLateUpdateLoop;
                    // Replace the original array with the new one
                    playerLoop.subSystemList[i].subSystemList = newSubSystems;
                    break;
                }
            }

            // Set player loop to new modified loop
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        #endregion

        #region Event Management

        /// <summary>
        /// Add target script to event dispatcher, which will make it so that the IDispatcher events for the target event type will now be invoked.
        /// </summary>
        /// <param name="eventType">PlayerLoop event type</param>
        /// <param name="targetScript">Target script</param>
        public static void AddHandler(EventType eventType, IDispatcher targetScript)
        {
            if (targetScript is MonoBehaviour behaviour)
            {
                if(Instance.ShowDebugLogs) Debug.Log($"{DebugLogColors.eventDebugMarker} Added handler for {behaviour.gameObject.name}!");
                
                switch (eventType) // Adds both the interface reference and the monobehaviour reference to the relevant arrays. We'll add monobehaviours so that we can keep track of when to automatically clean up scripts.
                {
                    case EventType.Update:
                        if (!UpdateDispatching.Contains(targetScript))
                        {
                            UpdateDispatching = UpdateDispatching.AddUnique(targetScript);
                            UpdateInternalScriptRefs = UpdateInternalScriptRefs.AddUnique(behaviour);
                            
                            targetScript.OnAddHandler(eventType);
                            targetScript.UpdateHandler(Time.deltaTime);
                        }
                        break;
                    case EventType.LateUpdate:
                        if (!LateUpdateDispatching.Contains(targetScript))
                        {
                            LateUpdateDispatching = LateUpdateDispatching.AddUnique(targetScript);
                            LateUpdateInternalScriptRefs = LateUpdateInternalScriptRefs.AddUnique(behaviour);
                            
                            targetScript.OnAddHandler(eventType);
                            targetScript.LateUpdateHandler(Time.deltaTime);
                        }
                        break;
                    case EventType.FixedUpdate:
                        if (!FixedUpdateDispatching.Contains(targetScript))
                        {
                            FixedUpdateDispatching = FixedUpdateDispatching.AddUnique(targetScript);
                            FixedUpdateInternalScriptRefs = FixedUpdateInternalScriptRefs.AddUnique(behaviour);
                            
                            targetScript.OnAddHandler(eventType);
                            targetScript.FixedUpdateHandler(Time.fixedDeltaTime);
                        }
                        break;
                    case EventType.PostLateUpdate:
                        if (!PostLateUpdateDispatching.Contains(targetScript))
                        {
                            PostLateUpdateDispatching = PostLateUpdateDispatching.AddUnique(targetScript);
                            PostLateUpdateInternalScriptRefs = PostLateUpdateInternalScriptRefs.AddUnique(behaviour);
                            
                            targetScript.OnAddHandler(eventType);
                            targetScript.PostLateUpdateHandler(Time.deltaTime);
                        }
                        break;
                    
                }
            }
            else
            {
                if(Instance.ShowDebugLogs) Debug.LogError($"{DebugLogColors.eventDebugMarker} Target script is not a MonoBehaviour! The event dispatcher currently only supports MonoBehaviours");
            }
        }
        
        /// <summary>
        /// Removes target from event dispatcher, which will make relevant IDispatcher events will no longer be invoked.
        /// </summary>
        /// <param name="eventType">PlayerLoop event type</param>
        /// <param name="targetScript">Target script</param>
        public static void RemoveHandler(EventType eventType, IDispatcher targetScript)
        {
            if (targetScript is MonoBehaviour behaviour)
            {
                if(Instance.ShowDebugLogs) Debug.Log($"{DebugLogColors.eventDebugMarker} Removed handler from {behaviour.gameObject.name}!");
                
                switch (eventType)
                {
                    case EventType.Update:
                        UpdateDispatching = UpdateDispatching.Remove(targetScript);
                        UpdateInternalScriptRefs = UpdateInternalScriptRefs.Remove(behaviour);
                        
                        targetScript.OnRemoveHandler(eventType);
                        break;
                    case EventType.LateUpdate:
                        LateUpdateDispatching = LateUpdateDispatching.Remove(targetScript);
                        LateUpdateInternalScriptRefs = LateUpdateInternalScriptRefs.Remove(behaviour);
                        
                        targetScript.OnRemoveHandler(eventType);
                        break;
                    case EventType.FixedUpdate:
                        FixedUpdateDispatching = FixedUpdateDispatching.Remove(targetScript);
                        FixedUpdateInternalScriptRefs = FixedUpdateInternalScriptRefs.Remove(behaviour);
                        
                        targetScript.OnRemoveHandler(eventType);
                        break;
                    case EventType.PostLateUpdate:
                        PostLateUpdateDispatching = PostLateUpdateDispatching.Remove(targetScript);
                        PostLateUpdateInternalScriptRefs = PostLateUpdateInternalScriptRefs.Remove(behaviour);
                        
                        targetScript.OnRemoveHandler(eventType);
                        break;
                }
            }
            else
            {
                if(Instance.ShowDebugLogs) Debug.LogError($"{DebugLogColors.eventDebugMarker} Target script is not a MonoBehaviour! The event dispatcher currently only supports MonoBehaviours");
                
                switch (eventType)
                {
                    case EventType.Update:
                        if (UpdateDispatching.Contains(targetScript))
                        {
                            if(Instance.ShowDebugLogs) Debug.LogError($"{DebugLogColors.eventDebugMarker} Target script is not a MonoBehaviour, but has somehow made its way into the <color=yellow>Update</color> dispatcher array!");
                        }
                        break;
                    case EventType.LateUpdate:
                        if (LateUpdateDispatching.Contains(targetScript))
                        {
                            if(Instance.ShowDebugLogs) Debug.LogError($"{DebugLogColors.eventDebugMarker} Target script is not a MonoBehaviour, but has somehow made its way into the <color=yellow>LateUpdate</color> dispatcher array!");
                        }
                        break;
                    case EventType.FixedUpdate:
                        if (FixedUpdateDispatching.Contains(targetScript))
                        {
                            if(Instance.ShowDebugLogs) Debug.LogError($"{DebugLogColors.eventDebugMarker} Target script is not a MonoBehaviour, but has somehow made its way into the <color=yellow>FixedUpdate</color> dispatcher array!");
                        }
                        break;
                    case EventType.PostLateUpdate:
                        if (PostLateUpdateDispatching.Contains(targetScript))
                        {
                            if(Instance.ShowDebugLogs) Debug.LogError($"{DebugLogColors.eventDebugMarker} Target script is not a MonoBehaviour, but has somehow made its way into the <color=yellow>PostLateUpdate</color> dispatcher array!");
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Checks if target script has an active handler of the input event type.
        /// </summary>
        /// <param name="eventType">PlayerLoop event type</param>
        /// <param name="targetScript">Target script</param>
        /// <returns>True if handler found of input event type</returns>
        public static bool HasHandler(EventType eventType, IDispatcher targetScript)
        {
            switch (eventType)
            {
                case EventType.Update:
                    if (UpdateDispatching.Contains(targetScript)) return true;
                    break;
                case EventType.LateUpdate:
                    if (LateUpdateDispatching.Contains(targetScript)) return true;
                    break;
                case EventType.FixedUpdate:
                    if (FixedUpdateDispatching.Contains(targetScript)) return true;
                    break;
                case EventType.PostLateUpdate:
                    if (PostLateUpdateDispatching.Contains(targetScript)) return true;
                    break;
            }

            return false;
        }

        #endregion

        #region Loops

        private void Update()
        {
            if (UpdateDispatching.Length < 1) return;

            for (var i = 0; i < UpdateDispatching.Length; i++)
            {
                if (!UpdateInternalScriptRefs[i]) // If script is now null (Usually due to scene switching), remove it from the array
                {
                    RemoveHandler(EventType.Update, UpdateDispatching[i]);
                    if(ShowDebugLogs) Debug.Log($"{DebugLogColors.eventDebugMarker} Script has been removed from <color=yellow>Update</color> loop due to null ref!");
                }
                else
                {
                    UpdateDispatching[i].UpdateHandler(Time.deltaTime);
                }
            }
        }
        
        private void LateUpdate()
        {
            if (LateUpdateDispatching.Length < 1) return;

            for (var i = 0; i < LateUpdateDispatching.Length; i++)
            {
                if (!LateUpdateInternalScriptRefs[i]) // If script is now null (Usually due to scene switching), remove it from the array
                {
                    RemoveHandler(EventType.LateUpdate, LateUpdateDispatching[i]);
                    if(Instance.ShowDebugLogs) Debug.Log($"{DebugLogColors.eventDebugMarker} Script has been removed from <color=yellow>LateUpdate</color> loop due to null ref!");
                }
                else
                {
                    LateUpdateDispatching[i].LateUpdateHandler(Time.deltaTime);
                }
            }
        }
        
        private void FixedUpdate()
        {
            if (FixedUpdateDispatching.Length < 1) return;

            for (var i = 0; i < FixedUpdateDispatching.Length; i++)
            {
                if (!FixedUpdateInternalScriptRefs[i]) // If script is now null (Usually due to scene switching), remove it from the array
                {
                    RemoveHandler(EventType.FixedUpdate, FixedUpdateDispatching[i]);
                    if(Instance.ShowDebugLogs) Debug.Log($"{DebugLogColors.eventDebugMarker} Script has been removed from <color=yellow>FixedUpdate</color> loop due to null ref!");
                }
                else
                {
                    FixedUpdateDispatching[i].FixedUpdateHandler(Time.fixedDeltaTime);
                }
            }
        }

        private void PostLateUpdate()
        {
            if (PostLateUpdateDispatching.Length < 1) return;

            for (var i = 0; i < PostLateUpdateDispatching.Length; i++)
            {
                if (!PostLateUpdateInternalScriptRefs[i]) // If script is now null (Usually due to scene switching), remove it from the array
                {
                    RemoveHandler(EventType.PostLateUpdate, PostLateUpdateDispatching[i]);
                    if(Instance.ShowDebugLogs) Debug.Log($"{DebugLogColors.eventDebugMarker} Script has been removed from <color=yellow>FixedUpdate</color> loop due to null ref!");
                }
                else
                {
                    PostLateUpdateDispatching[i].PostLateUpdateHandler(Time.deltaTime);
                }
            }
        }

        #endregion
    }

    #region Helper Classes

    public struct DebugLogColors
    {
        #region Color Values

        private static readonly Color _eventDispatcherColor = new Color(0.89f, 0.486f, 0.1f);

        #endregion

        #region Properties

        #region Colors
        public static string eventDispatcherColor
        {
            get => ColorToHex(_eventDispatcherColor);
        }

        #endregion

        #region Markers
        public static string eventDebugMarker
        {
            get => $"[<color={eventDispatcherColor}>EVENT DISPATCHER</color>]";
        }


        #endregion

        #endregion
        
        #region Helpers

        /// <summary>
        /// Returns the corresponding HEX value to an input color.
        /// </summary>
        /// <param name="inputColor"></param>
        /// <returns></returns>
        public static string ColorToHex(Color inputColor)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(inputColor)}";
        }

        #endregion
    }

    public static class ArrayExtensions
    {
        /// <summary>
        /// Adds an object to the end of the array
        /// <para>
        /// Based on: <see href="https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.add?view=net-6.0">List&lt;T&gt;.Add(T)</see>
        /// </para>
        /// </summary>
        /// <returns>Modified T[]</returns>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">Source T[] to modify.</param>
        /// <param name="item">The object to be added to the end of the T[].</param>
        private static T[] Add<T>(this T[] array, T item)
        {
            int length = array.Length;

            T[] newArray = new T[length + 1];

            array.CopyTo(newArray, 0);

            newArray.SetValue(item, length);

            return newArray;
        }
        
        /// <summary>
        /// Adds an object to the end of the array while ensuring that duplicates are not added
        /// </summary>
        /// <returns>Modified T[]</returns>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">Source T[] to modify.</param>
        /// <param name="item">The object to be added to the end of the T[].</param>
        public static T[] AddUnique<T>(this T[] array, T item)
        {
            if (Array.IndexOf(array, item) >= 0)
            {
                return array;
            }

            return array.Add(item);
        }
        
        /// <summary>
        /// Determines whether an element is in the array
        /// <para>
        /// Based on: <see href="https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.contains?view=net-6.0">List&lt;T&gt;.Contains(T)</see>
        /// </para>
        /// </summary>
        /// <returns><b><i>true</i></b> if <b><i>item</i></b> is found in the T[]; otherwise, <b><i>false</i></b>.</returns>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">Source T[] to modify.</param>
        /// <param name="item">The object to locate in the T[].</param>
        public static bool Contains<T>(this T[] array, T item)
        {
            return Array.IndexOf(array, item) >= 0;
        }
        
        /// <summary>
        /// Removes the first occurrence of a specific object from the array
        /// <para>
        /// Based on: <see href="https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.remove?view=net-6.0">List&lt;T&gt;.Remove(T)</see>
        /// </para>
        /// </summary>
        /// <returns>Modified T[]</returns>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">Source T[] to modify.</param>
        /// <param name="item">The object to remove from the T[].</param>
        public static T[] Remove<T>(this T[] array, T item)
        {
            int index = Array.IndexOf(array, item);

            if (index == -1)
            {
                return array;
            }

            return array.RemoveAt(index);
        }
        
        /// <summary>
        /// Removes the element at the specified index of the array
        /// <para>
        /// Based on: <see href="https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.removeat?view=net-6.0">List&lt;T&gt;.RemoveAt(Int32)</see>
        /// </para>
        /// </summary>
        /// <returns>Modified T[]</returns>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">Source T[] to modify.</param>
        /// <param name="index">The zero-based index of the element to remove.</param>
        private static T[] RemoveAt<T>(this T[] array, int index)
        {
            int length = array.Length;

            if (index >= length || index < 0)
            {
                return array;
            }

            int maxIndex = length - 1;

            T[] newArray = new T[maxIndex];

            if (index == 0)
            {
                Array.Copy(array, 1, newArray, 0, maxIndex);
            }
            else if (index == maxIndex)
            {
                Array.Copy(array, 0, newArray, 0, maxIndex);
            }
            else
            {
                Array.Copy(array, 0, newArray, 0, index);
                Array.Copy(array, index + 1, newArray, index, maxIndex - index);
            }

            return newArray;
        }
    }

    #endregion
}
