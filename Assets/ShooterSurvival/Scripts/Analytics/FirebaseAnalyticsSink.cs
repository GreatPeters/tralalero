using System;
using System.Collections.Generic;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using Firebase.Analytics;
#endif
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival.Analytics
{
    public sealed class FirebaseAnalyticsSink : IAnalyticsSink
    {
        private const string PendingEventsPlayerPrefsKey =
            "analytics_pending_events_v1";
        private const int MaxPendingEvents = 128;

        private readonly string pendingEventsPlayerPrefsKey;
        private readonly List<PendingAnalyticsEvent> pendingEvents;
        private readonly Func<AnalyticsEventData, bool> eventSender;
        private bool firebaseReady;
        private bool collectionEnabled;

        public FirebaseAnalyticsSink() :
            this(PendingEventsPlayerPrefsKey, true, TrySendToFirebase)
        {
        }

        public FirebaseAnalyticsSink(bool isCollectionEnabled) :
            this(
                PendingEventsPlayerPrefsKey,
                isCollectionEnabled,
                TrySendToFirebase)
        {
        }

        public FirebaseAnalyticsSink(string persistenceKey) :
            this(persistenceKey, true, TrySendToFirebase)
        {
        }

        public FirebaseAnalyticsSink(
            string persistenceKey,
            bool isCollectionEnabled,
            Func<AnalyticsEventData, bool> sender)
        {
            if (string.IsNullOrWhiteSpace(persistenceKey))
                throw new ArgumentException(
                    "Analytics persistence key is required.",
                    nameof(persistenceKey));

            pendingEventsPlayerPrefsKey = persistenceKey;
            pendingEvents = LoadPendingEvents(persistenceKey);
            eventSender = sender ?? throw new ArgumentNullException(nameof(sender));
            collectionEnabled = isCollectionEnabled;

            if (!collectionEnabled)
                ClearPendingEvents();
        }

        // Ready means the sink can durably accept an event. Native Firebase
        // readiness is tracked separately while events wait in PlayerPrefs.
        public bool IsReady => collectionEnabled;

        public int PendingEventCount => pendingEvents.Count;

        public void MarkFirebaseReady()
        {
            firebaseReady = true;
            if (collectionEnabled)
                TryDrainPendingEvents();
        }

        public void SetCollectionEnabled(bool enabled)
        {
            collectionEnabled = enabled;
            if (!enabled)
            {
                ClearPendingEvents();
                return;
            }

            if (firebaseReady)
                TryDrainPendingEvents();
        }

        public void LogEvent(AnalyticsEventData eventData)
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));

            if (!collectionEnabled)
                return;

            if (!firebaseReady)
            {
                Enqueue(eventData);
                return;
            }

            TryDrainPendingEvents();
            if (pendingEvents.Count > 0 || !eventSender(eventData))
                Enqueue(eventData);
        }

        public void Flush()
        {
            if (collectionEnabled && firebaseReady)
                TryDrainPendingEvents();
        }

        private void TryDrainPendingEvents()
        {
            bool changed = false;
            while (pendingEvents.Count > 0)
            {
                AnalyticsEventData eventData;
                try
                {
                    eventData = pendingEvents[0].ToEventData();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"[Analytics] Dropped an invalid queued event: {exception.Message}");
                    pendingEvents.RemoveAt(0);
                    changed = true;
                    continue;
                }

                if (!eventSender(eventData))
                    break;

                pendingEvents.RemoveAt(0);
                changed = true;
            }

            if (changed)
                SavePendingEvents();
        }

        private static bool TrySendToFirebase(AnalyticsEventData eventData)
        {
#if !((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            Debug.LogWarning(
                $"[Analytics] Firebase Analytics is unavailable on this build target; " +
                $"event '{eventData.Name}' remains queued.");
            return false;
#else
            try
            {
                var parameters = new Parameter[eventData.Parameters.Count];
                for (int i = 0; i < eventData.Parameters.Count; i++)
                {
                    AnalyticsParameterValue parameter = eventData.Parameters[i];
                    parameters[i] = parameter.Kind switch
                    {
                        AnalyticsParameterKind.String =>
                            new Parameter(parameter.Name, parameter.StringValue),
                        AnalyticsParameterKind.Long =>
                            new Parameter(parameter.Name, parameter.LongValue),
                        AnalyticsParameterKind.Double =>
                            new Parameter(parameter.Name, parameter.DoubleValue),
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(parameter.Kind),
                            parameter.Kind,
                            "Unsupported analytics parameter kind.")
                    };
                }

                FirebaseAnalytics.LogEvent(eventData.Name, parameters);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[Analytics] Firebase event '{eventData.Name}' will be retried: " +
                    exception.Message);
                return false;
            }
#endif
        }

        private void Enqueue(AnalyticsEventData eventData)
        {
            if (pendingEvents.Count >= MaxPendingEvents)
            {
                pendingEvents.RemoveAt(0);
                Debug.LogWarning(
                    "[Analytics] Pending event limit reached; the oldest event was dropped.");
            }

            pendingEvents.Add(PendingAnalyticsEvent.From(eventData));
            SavePendingEvents();
        }

        private static List<PendingAnalyticsEvent> LoadPendingEvents(
            string persistenceKey)
        {
            if (!PlayerPrefs.HasKey(persistenceKey))
                return new List<PendingAnalyticsEvent>();

            try
            {
                string json = PlayerPrefs.GetString(persistenceKey);
                PendingAnalyticsEventQueue queue =
                    JsonUtility.FromJson<PendingAnalyticsEventQueue>(json);
                List<PendingAnalyticsEvent> loaded =
                    queue?.events ?? new List<PendingAnalyticsEvent>();
                if (loaded.Count <= MaxPendingEvents)
                    return loaded;

                loaded.RemoveRange(0, loaded.Count - MaxPendingEvents);
                return loaded;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[Analytics] Invalid pending event queue was cleared: {exception.Message}");
                PlayerPrefs.DeleteKey(persistenceKey);
                PlayerPrefs.Save();
                return new List<PendingAnalyticsEvent>();
            }
        }

        private void SavePendingEvents()
        {
            if (pendingEvents.Count == 0)
            {
                PlayerPrefs.DeleteKey(pendingEventsPlayerPrefsKey);
            }
            else
            {
                var queue = new PendingAnalyticsEventQueue
                {
                    events = pendingEvents
                };
                PlayerPrefs.SetString(
                    pendingEventsPlayerPrefsKey,
                    JsonUtility.ToJson(queue));
            }

            PlayerPrefs.Save();
        }

        private void ClearPendingEvents()
        {
            pendingEvents.Clear();
            PlayerPrefs.DeleteKey(pendingEventsPlayerPrefsKey);
            PlayerPrefs.Save();
        }

        [Serializable]
        private sealed class PendingAnalyticsEventQueue
        {
            public List<PendingAnalyticsEvent> events =
                new List<PendingAnalyticsEvent>();
        }

        [Serializable]
        private sealed class PendingAnalyticsEvent
        {
            public string eventName = string.Empty;
            public List<PendingAnalyticsParameter> parameters =
                new List<PendingAnalyticsParameter>();

            public static PendingAnalyticsEvent From(AnalyticsEventData eventData)
            {
                var pending = new PendingAnalyticsEvent
                {
                    eventName = eventData.Name
                };

                for (int i = 0; i < eventData.Parameters.Count; i++)
                {
                    pending.parameters.Add(
                        PendingAnalyticsParameter.From(eventData.Parameters[i]));
                }

                return pending;
            }

            public AnalyticsEventData ToEventData()
            {
                var values = new AnalyticsParameterValue[parameters.Count];
                for (int i = 0; i < parameters.Count; i++)
                    values[i] = parameters[i].ToValue();

                return new AnalyticsEventData(eventName, values);
            }
        }

        [Serializable]
        private sealed class PendingAnalyticsParameter
        {
            public string parameterName = string.Empty;
            public AnalyticsParameterKind kind;
            public string stringValue = string.Empty;
            public long longValue;
            public double doubleValue;

            public static PendingAnalyticsParameter From(
                AnalyticsParameterValue parameter)
            {
                return new PendingAnalyticsParameter
                {
                    parameterName = parameter.Name,
                    kind = parameter.Kind,
                    stringValue = parameter.StringValue,
                    longValue = parameter.LongValue,
                    doubleValue = parameter.DoubleValue
                };
            }

            public AnalyticsParameterValue ToValue()
            {
                return kind switch
                {
                    AnalyticsParameterKind.String =>
                        new AnalyticsParameterValue(parameterName, stringValue),
                    AnalyticsParameterKind.Long =>
                        new AnalyticsParameterValue(parameterName, longValue),
                    AnalyticsParameterKind.Double =>
                        new AnalyticsParameterValue(parameterName, doubleValue),
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Unsupported queued analytics parameter kind.")
                };
            }
        }
    }
}
